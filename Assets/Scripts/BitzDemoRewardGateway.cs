using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Optional interface for gateways that can automatically sign in before reward calls.</summary>
public interface IBitzAutoSignInGateway
{
    Task<bool> EnsureSignedInAsync();
    string LastConnectionError { get; }
}

/// <summary>
/// Dev-only interface for local demo servers that have no external SSV callback configured.
/// Never called unless a real rewarded ad finishes playing.
/// </summary>
public interface IBitzSimulatedRewardConfirmation
{
    bool AllowSimulatedConfirmation { get; }
    Task<BitzRewardReply> ConfirmAfterCompletedAdAsync(string attemptId, string receipt);
}

/// <summary>
/// Adapts the auto-connected <see cref="BitzLocalServerClient"/> to <see cref="IBitzRewardGateway"/>
/// so <see cref="BitzRewardedReviveProvider"/> runs the same prepare -> play ad -> confirm -> spend
/// flow against the local or hosted demo Bitz-Server.
/// </summary>
public sealed class BitzDemoRewardGateway : IBitzRewardGateway, IBitzAutoSignInGateway, IBitzSimulatedRewardConfirmation
{
    private readonly BitzLocalServerClient client;
    private bool loggedSimulatedNotice;

    public BitzDemoRewardGateway(BitzLocalServerClient serverClient)
    {
        client = serverClient ?? throw new ArgumentNullException(nameof(serverClient));
    }

    public string CurrentPlayerId => client != null ? client.PlayerId : null;
    public string LastConnectionError => client != null ? client.LastError : "Game server client is unavailable.";

    public bool AllowSimulatedConfirmation => client != null && client.AllowSimulatedConfirmation;

    public Task<bool> EnsureSignedInAsync()
    {
        if (client == null) return Task.FromResult(false);
        return client.EnsureSignedInAsync();
    }

    public async Task<BitzRewardReply> GetLivesAsync()
    {
        if (!await EnsureSignedInAsync()) return OfflineReply();
        var result = await client.GetStateAsync();
        if (result == null) return OfflineReply();
        if (result.success) return new BitzRewardReply { Code = "Ok", Lives = result.lives };
        return ErrorReply(result);
    }

    public async Task<BitzRewardReply> SpendLifeAsync(string requestId, string mapId)
    {
        if (!await EnsureSignedInAsync()) return OfflineReply();
        var result = await client.StartSongAsync(mapId, requestId);
        if (result == null) return OfflineReply();
        if (result.success) return new BitzRewardReply { Code = "Ok", Lives = result.lives };
        if (string.Equals(result.code, "no_lives", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(result.code, "insufficient_lives", StringComparison.OrdinalIgnoreCase))
            return new BitzRewardReply { Code = "NoLives", Lives = result.lives };
        return ErrorReply(result);
    }

    public async Task<BitzRewardReply> PrepareAsync(string requestId)
    {
        if (!await EnsureSignedInAsync()) return OfflineReply();
        var result = await client.PrepareSimulatedReviveAsync(requestId);
        if (result == null) return OfflineReply();
        if (!result.success || string.IsNullOrWhiteSpace(result.attemptId))
            return ErrorReply(result);

        var appId = !string.IsNullOrWhiteSpace(result.appId) ? result.appId : BitzAds.VerifiedAppId;
        var adUnitId = !string.IsNullOrWhiteSpace(result.adUnitId) ? result.adUnitId : BitzAds.ConfiguredRewardedAdUnitId;
        var customData = !string.IsNullOrWhiteSpace(result.customData) ? result.customData : result.attemptId;

        return new BitzRewardReply
        {
            Code = "Prepared",
            AttemptId = result.attemptId,
            CustomData = customData,
            AppId = appId,
            AdUnitId = adUnitId,
            Lives = result.lives
        };
    }

    // Read-only status probe: never grants a simulated reward on early close or failed playback.
    public async Task<BitzRewardReply> GetStatusAsync(string attemptId)
    {
        if (!await EnsureSignedInAsync()) return OfflineReply();
        var state = await client.GetStateAsync();
        if (state == null || !state.success) return ErrorReply(state);
        return new BitzRewardReply { Code = "Pending", AttemptExpired = false, Lives = state.lives };
    }

    public Task<BitzRewardReply> SubmitReceiptAsync(string receipt)
    {
        // Demo HTTP server does not verify signed receipts directly; simulated confirmation is handled
        // explicitly via ConfirmAfterCompletedAdAsync when allowed in dev builds.
        return Task.FromResult(new BitzRewardReply { Code = "Pending" });
    }

    public async Task<BitzRewardReply> ConfirmAfterCompletedAdAsync(string attemptId, string receipt)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!AllowSimulatedConfirmation)
            return new BitzRewardReply { Code = "Pending" };

        if (!loggedSimulatedNotice)
        {
            loggedSimulatedNotice = true;
            Debug.Log("BitzDemoRewardGateway: requesting dev-only simulated revive confirmation after completed rewarded ad.");
        }

        if (!await EnsureSignedInAsync()) return OfflineReply();
        var result = await client.GrantSimulatedReviveAsync(attemptId);
        if (result == null) return OfflineReply();
        if (result.success) return new BitzRewardReply { Code = "Granted", Lives = result.lives };
        return ErrorReply(result);
#else
        return await Task.FromResult(new BitzRewardReply { Code = "Pending" });
#endif
    }

    private static BitzRewardReply OfflineReply() => new BitzRewardReply { Code = "Unavailable" };

    private static BitzRewardReply ErrorReply(BitzLocalServerClient.Result result)
    {
        if (result == null) return OfflineReply();
        if (string.Equals(result.code, "unauthorized", StringComparison.OrdinalIgnoreCase))
            return new BitzRewardReply { Code = "Unauthorized" };
        if (string.Equals(result.code, "expired", StringComparison.OrdinalIgnoreCase))
            return new BitzRewardReply { Code = "Expired", AttemptExpired = true };
        if (string.Equals(result.code, "conflict", StringComparison.OrdinalIgnoreCase))
            return new BitzRewardReply { Code = "Conflict" };
        return new BitzRewardReply { Code = "Unavailable" };
    }
}
