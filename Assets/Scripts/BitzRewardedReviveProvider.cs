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

internal interface IBitzRewardedAdAvailability
{
    string CheckBeforePrepare();
    string GetLastFailureMessage();
}

/// <summary>Shows a bound rewarded ad and revives only after the game server confirms the grant.</summary>
public sealed class BitzRewardedReviveProvider : MonoBehaviour, IBitzReviveProvider, IBitzReviveOptions, IBitzReviveStatusSource
{
    private IBitzRewardGateway gateway;
    private IBitzRewardedAdPresenter presenter;
    private bool busy;
    private const string Prefix = "bitz.rewardAttempt.v1.";

    public string LastStatusMessage { get; private set; }
    public event Action<string> StatusChanged;

    public void Configure(IBitzRewardGateway rewardGateway, IBitzRewardedAdPresenter adPresenter = null)
    { gateway = rewardGateway; presenter = adPresenter ?? new SdkPresenter(); }

    private void ReportStatus(string message)
    {
        LastStatusMessage = message;
        if (!string.IsNullOrEmpty(message)) StatusChanged?.Invoke(message);
    }

    public async void GetOption(Action<BitzReviveOption> completed)
    {
        LastStatusMessage = null;
        if (gateway == null)
        {
            LastStatusMessage = "Revive service is not connected.";
            completed?.Invoke(BitzReviveOption.Unavailable);
            return;
        }
        try
        {
            if (gateway is IBitzAutoSignInGateway autoSignIn)
            {
                var signedIn = await autoSignIn.EnsureSignedInAsync();
                if (this == null) return;
                if (!signedIn)
                {
                    LastStatusMessage = "Game server is offline. Check that Bitz-Server is running.";
                    completed?.Invoke(BitzReviveOption.Unavailable);
                    return;
                }
            }

            var owner = gateway.CurrentPlayerId;
            if (string.IsNullOrWhiteSpace(owner))
            {
                LastStatusMessage = "Not signed in to the game server.";
                completed?.Invoke(BitzReviveOption.Unavailable);
                return;
            }
            var key = Prefix + owner + ".";
            if (PlayerPrefs.GetInt(key + "expired", 0) == 1) { completed?.Invoke(BitzReviveOption.StartNew); return; }
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString(key + "spend", "")) || !string.IsNullOrEmpty(PlayerPrefs.GetString(key + "receipt", "")) || PlayerPrefs.GetInt(key + "awaiting", 0) == 1)
            { completed?.Invoke(BitzReviveOption.CheckPending); return; }
            var result = await gateway.GetLivesAsync();
            if (this == null) return;
            if (gateway.CurrentPlayerId == owner && result?.Code == "Ok" && result.Lives.HasValue)
            {
                if (result.Lives.Value > 0)
                {
                    var count = result.Lives.Value;
                    LastStatusMessage = $"Use one life to continue ({count} {(count == 1 ? "life" : "lives")} left).";
                    completed?.Invoke(BitzReviveOption.UseLife);
                    return;
                }
                if (presenter is IBitzRewardedAdAvailability availability)
                {
                    var blockReason = availability.CheckBeforePrepare();
                    if (!string.IsNullOrEmpty(blockReason)) LastStatusMessage = blockReason;
                }
                completed?.Invoke(BitzReviveOption.WatchAd);
                return;
            }
            LastStatusMessage = "Game server is offline or unreachable. Please try again.";
            completed?.Invoke(BitzReviveOption.Unavailable);
        }
        catch (Exception)
        {
            LastStatusMessage = "Game server is offline or unreachable. Please try again.";
            if (this != null) completed?.Invoke(BitzReviveOption.Unavailable);
        }
    }

    public async void RequestRevive(Action<BitzReviveResult> completed)
    {
        if (busy || gateway == null)
        {
            if (gateway == null) LastStatusMessage = "Revive service is not connected.";
            completed?.Invoke(BitzReviveResult.Unavailable);
            return;
        }
        busy = true;
        LastStatusMessage = null;
        try
        {
            if (gateway is IBitzAutoSignInGateway autoSignIn)
            {
                ReportStatus("Connecting to game server...");
                var signedIn = await autoSignIn.EnsureSignedInAsync();
                if (this == null) return;
                if (!signedIn)
                {
                    ReportStatus("Game server is offline. Gameplay continues offline.");
                    completed?.Invoke(BitzReviveResult.Unavailable);
                    return;
                }
            }

            var owner = gateway.CurrentPlayerId;
            if (string.IsNullOrWhiteSpace(owner))
            {
                ReportStatus("Not signed in to the game server.");
                completed?.Invoke(BitzReviveResult.Unavailable);
                return;
            }
            var result = await RunAsync(owner);
            if (this != null) completed?.Invoke(gateway.CurrentPlayerId == owner ? result : BitzReviveResult.Unavailable);
        }
        catch (Exception)
        {
            ReportStatus("Game server request failed. Please try again.");
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
                ReportStatus("Checking previous revive attempt...");
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
            ReportStatus("Completing revive...");
            var spendResult = await SpendLife(owner, key);
            if (spendResult != BitzReviveResult.Unavailable) return spendResult;
        }
        var receipt = PlayerPrefs.GetString(key + "receipt", "");
        var attempt = PlayerPrefs.GetString(key + "attempt", "");
        if (!string.IsNullOrEmpty(receipt))
        {
            ReportStatus("Verifying reward receipt...");
            var claim = await gateway.SubmitReceiptAsync(receipt);
            if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
            if (claim?.Code == "Granted") return await SpendLife(owner, key);
            if (claim?.Code == "Rejected") { Clear(key); ReportStatus("Reward receipt was rejected by the server."); return BitzReviveResult.Unavailable; }
            return BitzReviveResult.Pending;
        }
        if (!string.IsNullOrEmpty(attempt) && PlayerPrefs.GetInt(key + "awaiting", 0) == 1)
            return await CheckStatus(owner, key, attempt);

        ReportStatus("Checking lives...");
        var lives = await gateway.GetLivesAsync();
        if (gateway.CurrentPlayerId != owner || lives?.Code != "Ok" || !lives.Lives.HasValue)
        {
            ReportStatus("Game server is offline or unreachable. Please try again.");
            return BitzReviveResult.Unavailable;
        }
        if (lives.Lives.Value > 0)
        {
            ReportStatus("Using one life...");
            return await SpendLife(owner, key);
        }

        if (presenter is IBitzRewardedAdAvailability availability)
        {
            var adBlockReason = availability.CheckBeforePrepare();
            if (!string.IsNullOrEmpty(adBlockReason))
            {
                ReportStatus(adBlockReason);
                return BitzReviveResult.Unavailable;
            }
        }

        ReportStatus("Preparing revive on server...");
        var request = PlayerPrefs.GetString(key + "request", "");
        if (!Guid.TryParse(request, out _)) { request = Guid.NewGuid().ToString(); PlayerPrefs.SetString(key + "request", request); PlayerPrefs.Save(); }
        var prepared = await gateway.PrepareAsync(request);
        if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
        if (prepared?.Code == "Completed" && !string.IsNullOrEmpty(prepared.AttemptId))
            return await CheckStatus(owner, key, prepared.AttemptId);
        if (prepared?.Code == "Expired" || prepared?.Code == "Superseded" || prepared?.Code == "Conflict")
        { Clear(key); ReportStatus("Revive attempt expired. Please try again."); return BitzReviveResult.Unavailable; }
        if (prepared?.Code != "Prepared" || string.IsNullOrWhiteSpace(prepared.AttemptId) ||
            string.IsNullOrWhiteSpace(prepared.CustomData) || string.IsNullOrWhiteSpace(prepared.AppId) || string.IsNullOrWhiteSpace(prepared.AdUnitId))
        {
            if (prepared?.Code == "Pending") return BitzReviveResult.Pending;
            ReportStatus(string.IsNullOrWhiteSpace(prepared?.AdUnitId) || string.IsNullOrWhiteSpace(prepared?.AppId)
                ? "Rewarded ad is not configured or SDK is not ready."
                : "Unable to prepare revive on the game server.");
            return BitzReviveResult.Unavailable;
        }

        attempt = prepared.AttemptId;
        PlayerPrefs.SetString(key + "attempt", attempt);
        PlayerPrefs.SetInt(key + "awaiting", 1);
        PlayerPrefs.Save();

        ReportStatus("Loading rewarded ad...");
        var playback = await presenter.ShowAsync(prepared.AppId, prepared.AdUnitId, prepared.CustomData, signedReceipt =>
        {
            if (this != null && gateway.CurrentPlayerId == owner && !string.IsNullOrEmpty(signedReceipt))
            { PlayerPrefs.SetString(key + "receipt", signedReceipt); PlayerPrefs.Save(); }
        });
        if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;

        if (playback.result == BitzAds.RewardedDisplayResult.VerificationPending &&
            gateway is IBitzSimulatedRewardConfirmation simulated &&
            simulated.AllowSimulatedConfirmation)
        {
            ReportStatus("Confirming revive with server...");
            var confirmed = await simulated.ConfirmAfterCompletedAdAsync(attempt, playback.receipt);
            if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
            if (confirmed?.Code == "Granted") return await SpendLife(owner, key);
            ReportStatus("Could not confirm revive with the game server. Check status shortly.");
            return BitzReviveResult.Pending;
        }

        if (!string.IsNullOrEmpty(playback.receipt))
        {
            // Kept locally for retry after a disconnect; it is not proof until the server verifies it.
            PlayerPrefs.SetString(key + "receipt", playback.receipt); PlayerPrefs.Save();
            ReportStatus("Verifying reward receipt...");
            var claim = await gateway.SubmitReceiptAsync(playback.receipt);
            if (gateway.CurrentPlayerId != owner) return BitzReviveResult.Unavailable;
            if (claim?.Code == "Granted") return await SpendLife(owner, key);
            if (claim?.Code == "Rejected") { Clear(key); ReportStatus("Reward receipt was rejected by the server."); return BitzReviveResult.Unavailable; }
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
            if (playback.result == BitzAds.RewardedDisplayResult.Closed)
            {
                ReportStatus("The ad was closed early. Watch the full ad to revive.");
                return BitzReviveResult.Cancelled;
            }
            if (presenter is IBitzRewardedAdAvailability failureSource)
                ReportStatus(failureSource.GetLastFailureMessage());
            return BitzReviveResult.Unavailable;
        }
        ReportStatus("Waiting for server reward verification...");
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
            ReportStatus("No lives available on the server.");
            return BitzReviveResult.Unavailable;
        }
        return BitzReviveResult.Pending;
    }

    private sealed class SdkPresenter : IBitzRewardedAdPresenter, IBitzRewardedAdAvailability
    {
        public string CheckBeforePrepare()
        {
            var reason = BitzAds.CheckRewardedAvailability();
            return reason == BitzAds.AdFailureReason.None ? null : BitzAds.DescribeFailure(reason);
        }

        public string GetLastFailureMessage() => BitzAds.DescribeFailure(BitzAds.LastRewardedFailure);

        public Task<(BitzAds.RewardedDisplayResult result, string receipt)> ShowAsync(string appId, string adUnitId, string customData, Action<string> receiptAvailable)
        {
            var completion = new TaskCompletionSource<(BitzAds.RewardedDisplayResult, string)>();
            BitzAds.ShowRewardedWithReceipt(appId, adUnitId, customData, (result, receipt) => completion.TrySetResult((result, receipt)), receiptAvailable);
            return completion.Task;
        }
    }
}
