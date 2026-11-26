using System;
using System.Threading.Tasks;
using UnityEngine;

// Implementations must call the authenticated game server; these values are never trusted as proof locally.
public interface IBitzRewardGateway
{
    string CurrentPlayerId { get; }
    Task<BitzRewardReply> GetLivesAsync();
    Task<BitzRewardReply> SpendLifeAsync(string requestId, string mapId);
    Task<BitzRewardReply> PrepareAsync(string requestId);
    Task<BitzRewardReply> GetStatusAsync(string attemptId);
    Task<BitzRewardReply> SubmitReceiptAsync(string receipt);
}

public sealed class BitzRewardReply
{
    public string Code;
    public string AttemptId;
    public string CustomData;
    public string AppId;
    public string AdUnitId;
    public bool AttemptExpired;
    public int? Lives;
}

public interface IBitzRewardedAdPresenter
{
    Task<(BitzAds.RewardedDisplayResult result, string receipt)> ShowAsync(string appId, string adUnitId, string customData, Action<string> receiptAvailable);
}

/// <summary>Shows a bound rewarded ad and revives only after the game server confirms the grant.</summary>
public sealed class BitzRewardedReviveProvider : MonoBehaviour, IBitzReviveProvider, IBitzReviveOptions
{
    private IBitzRewardGateway gateway;
    private IBitzRewardedAdPresenter presenter;
    private bool busy;
    private const string Prefix = "bitz.rewardAttempt.v1.";

    public void Configure(IBitzRewardGateway rewardGateway, IBitzRewardedAdPresenter adPresenter = null)
    { gateway = rewardGateway; presenter = adPresenter ?? new SdkPresenter(); }

    public async void GetOption(Action<BitzReviveOption> completed)
    {
        if (gateway == null) { completed?.Invoke(BitzReviveOption.Unavailable); return; }
        try
        {
            var owner = gateway.CurrentPlayerId;
            if (string.IsNullOrWhiteSpace(owner)) { completed?.Invoke(BitzReviveOption.Unavailable); return; }
            var key = Prefix + owner + ".";
            if (PlayerPrefs.GetInt(key + "expired", 0) == 1) { completed?.Invoke(BitzReviveOption.StartNew); return; }
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString(key + "spend", "")) || !string.IsNullOrEmpty(PlayerPrefs.GetString(key + "receipt", "")) || PlayerPrefs.GetInt(key + "awaiting", 0) == 1)
            { completed?.Invoke(BitzReviveOption.CheckPending); return; }
            var result = await gateway.GetLivesAsync();
            if (this != null) completed?.Invoke(gateway.CurrentPlayerId == owner && result?.Code == "Ok" && result.Lives.HasValue
                ? result.Lives.Value > 0 ? BitzReviveOption.UseLife : BitzReviveOption.WatchAd : BitzReviveOption.Unavailable);
        }
        catch (Exception) { if (this != null) completed?.Invoke(BitzReviveOption.Unavailable); }
    }

    public async void RequestRevive(Action<BitzReviveResult> completed)
    {
        if (busy || gateway == null)
        { completed?.Invoke(BitzReviveResult.Unavailable); return; }
        busy = true;
        try
        {
            var owner = gateway.CurrentPlayerId;
            if (string.IsNullOrWhiteSpace(owner)) { completed?.Invoke(BitzReviveResult.Unavailable); return; }
            var result = await RunAsync(owner);
            if (this != null) completed?.Invoke(gateway.CurrentPlayerId == owner ? result : BitzReviveResult.Unavailable);
        }
        catch (Exception)
        {
            if (this != null) completed?.Invoke(BitzReviveResult.Unavailable);
        }
        finally { busy = false; }
    }

    private async Task<BitzReviveResult> RunAsync(string owner)
    {
        var key = Prefix + owner + ".";
        if (PlayerPrefs.GetInt(key + "expired", 0) == 1)
        {
            var oldAttempt = PlayerPrefs.GetString(key + "attempt", "");
            if (!string.IsNullOrEmpty(oldAttempt))
            {
                var oldStatus = await gateway.GetStatusAsync(oldAttempt);
                if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
                if (oldStatus?.Code == "Granted") return await SpendLife(owner, key);
                if (oldStatus == null || oldStatus.Code == "Unavailable" || oldStatus.Code == "Unauthorized") return BitzReviveResult.Pending;
            }
            Clear(key);
        }
        // The spend request is persisted before the call so a lost response cannot spend twice.
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString(key + "spend", "")))
        {
            var spendResult = await SpendLife(owner, key);
            if (spendResult != BitzReviveResult.Unavailable) return spendResult;
        }
        var receipt = PlayerPrefs.GetString(key + "receipt", "");
        var attempt = PlayerPrefs.GetString(key + "attempt", "");
        if (!string.IsNullOrEmpty(receipt))
        {
            var claim = await gateway.SubmitReceiptAsync(receipt);
            if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
            if (claim?.Code == "Granted") return await SpendLife(owner, key);
            if (claim?.Code == "Rejected") { Clear(key); return BitzReviveResult.Unavailable; }
            return BitzReviveResult.Pending;
        }
        if (!string.IsNullOrEmpty(attempt) && PlayerPrefs.GetInt(key + "awaiting", 0) == 1)
            return await CheckStatus(owner, key, attempt);

        var lives = await gateway.GetLivesAsync();
        if (gateway.CurrentPlayerId != owner || lives?.Code != "Ok" || !lives.Lives.HasValue) return BitzReviveResult.Unavailable;
        if (lives.Lives.Value > 0) return await SpendLife(owner, key);

        var request = PlayerPrefs.GetString(key + "request", "");
        if (!Guid.TryParse(request, out _)) { request = Guid.NewGuid().ToString(); PlayerPrefs.SetString(key + "request", request); PlayerPrefs.Save(); }
        var prepared = await gateway.PrepareAsync(request);
        if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
        if (prepared?.Code == "Completed" && !string.IsNullOrEmpty(prepared.AttemptId))
            return await CheckStatus(owner, key, prepared.AttemptId);
        if (prepared?.Code == "Expired" || prepared?.Code == "Superseded" || prepared?.Code == "Conflict")
        { Clear(key); return BitzReviveResult.Unavailable; }
        if (prepared?.Code != "Prepared" || string.IsNullOrWhiteSpace(prepared.AttemptId) ||
            string.IsNullOrWhiteSpace(prepared.CustomData) || string.IsNullOrWhiteSpace(prepared.AppId) || string.IsNullOrWhiteSpace(prepared.AdUnitId))
            return prepared?.Code == "Pending" ? BitzReviveResult.Pending : BitzReviveResult.Unavailable;

        attempt = prepared.AttemptId;
        PlayerPrefs.SetString(key + "attempt", attempt);
        PlayerPrefs.SetInt(key + "awaiting", 1);
        PlayerPrefs.Save();
        var playback = await presenter.ShowAsync(prepared.AppId, prepared.AdUnitId, prepared.CustomData, signedReceipt =>
        {
            if (this != null && gateway.CurrentPlayerId == owner && !string.IsNullOrEmpty(signedReceipt))
            { PlayerPrefs.SetString(key + "receipt", signedReceipt); PlayerPrefs.Save(); }
        });
        if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
        if (!string.IsNullOrEmpty(playback.receipt))
        {
            // Kept locally for retry after a disconnect; it is not proof until the server verifies it.
            PlayerPrefs.SetString(key + "receipt", playback.receipt); PlayerPrefs.Save();
            var claim = await gateway.SubmitReceiptAsync(playback.receipt);
            if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
            if (claim?.Code == "Granted") return await SpendLife(owner, key);
            if (claim?.Code == "Rejected") { Clear(key); return BitzReviveResult.Unavailable; }
            return BitzReviveResult.Pending;
        }
        if (playback.result != BitzAds.RewardedDisplayResult.VerificationPending)
        {
            // Check before allowing another playback: a callback may already have arrived.
            var status = await gateway.GetStatusAsync(attempt);
            if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
            if (status?.Code == "Granted") return await SpendLife(owner, key);
            if (status?.Code == "Pending" && !status.AttemptExpired)
            { PlayerPrefs.SetInt(key + "awaiting", 0); PlayerPrefs.Save(); }
            return playback.result == BitzAds.RewardedDisplayResult.Closed ? BitzReviveResult.Cancelled : BitzReviveResult.Unavailable;
        }
        return await CheckStatus(owner, key, attempt);
    }

    private async Task<BitzReviveResult> CheckStatus(string owner, string key, string attempt)
    {
        for (var i = 0; i < 6; i++)
        {
            var status = await gateway.GetStatusAsync(attempt);
            if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
            if (status?.Code == "Granted") return await SpendLife(owner, key);
            if (status?.Code == "Superseded" || status?.Code == "NotFound") { Clear(key); return BitzReviveResult.Unavailable; }
            if (status?.Code == "Pending" && status.AttemptExpired)
            { PlayerPrefs.SetInt(key + "expired", 1); PlayerPrefs.Save(); return BitzReviveResult.Expired; }
            if (i < 5) await Task.Delay(2000);
        }
        return BitzReviveResult.Pending;
    }

    private static void Clear(string key)
    {
        PlayerPrefs.DeleteKey(key + "request"); PlayerPrefs.DeleteKey(key + "attempt");
        PlayerPrefs.DeleteKey(key + "receipt"); PlayerPrefs.DeleteKey(key + "awaiting"); PlayerPrefs.Save();
        PlayerPrefs.DeleteKey(key + "spend"); PlayerPrefs.DeleteKey(key + "spendMap"); PlayerPrefs.DeleteKey(key + "expired"); PlayerPrefs.Save();
    }

    private async Task<BitzReviveResult> SpendLife(string owner, string key)
    {
        var request = PlayerPrefs.GetString(key + "spend", "");
        var map = PlayerPrefs.GetString(key + "spendMap", "");
        if (!Guid.TryParse(request, out _) || !Guid.TryParse(map, out _))
        {
            request = Guid.NewGuid().ToString(); map = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(key + "spend", request); PlayerPrefs.SetString(key + "spendMap", map); PlayerPrefs.Save();
        }
        var result = await gateway.SpendLifeAsync(request, map);
        if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
        if (result?.Code == "Ok") { Clear(key); return BitzReviveResult.Confirmed; }
        if (result?.Code == "NoLives")
        {
            PlayerPrefs.DeleteKey(key + "spend"); PlayerPrefs.DeleteKey(key + "spendMap"); PlayerPrefs.Save();
            return BitzReviveResult.Unavailable;
        }
        return BitzReviveResult.Pending;
    }

    private sealed class SdkPresenter : IBitzRewardedAdPresenter
    {
        public Task<(BitzAds.RewardedDisplayResult result, string receipt)> ShowAsync(string appId, string adUnitId, string customData, Action<string> receiptAvailable)
        {
            var completion = new TaskCompletionSource<(BitzAds.RewardedDisplayResult, string)>();
            BitzAds.ShowRewardedWithReceipt(appId, adUnitId, customData, (result, receipt) => completion.TrySetResult((result, receipt)), receiptAvailable);
            return completion.Task;
        }
    }
}
