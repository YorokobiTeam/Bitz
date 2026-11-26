#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BitzReviveFlowSmokeTest
{
    private const string RunningKey = "Bitz.ReviveFlowSmoke.Running";
    static BitzReviveFlowSmokeTest() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { SessionState.SetBool(RunningKey, true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); EditorApplication.isPlaying = true; }
    private static async void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
        try
        {
            var provider = new GameObject("Revive flow smoke").AddComponent<BitzRewardedReviveProvider>();
            var gateway = new FakeGateway { Lives = 1 }; var ads = new FakeAds(); provider.Configure(gateway, ads);
            Check(await Option(provider) == BitzReviveOption.UseLife, "existing life option");
            Check(await Request(provider) == BitzReviveResult.Confirmed && gateway.Lives == 0 && ads.Calls == 0, "existing life consumed without ad");
            Check(await Option(provider) == BitzReviveOption.WatchAd, "zero lives offers ad");
            gateway.ClaimCode = "unavailable";
            Check(await Request(provider) == BitzReviveResult.Pending && gateway.Lives == 0 && ads.Calls == 1, "unverified receipt cannot revive");
            gateway.ClaimCode = "Granted";
            Check(await Option(provider) == BitzReviveOption.CheckPending, "pending receipt recovered");
            Check(await Request(provider) == BitzReviveResult.Confirmed && gateway.Lives == 0 && ads.Calls == 1, "confirmed reward spent once");
            var retryGateway = new FakeGateway { Lives = 1, ThrowAfterSpendOnce = true };
            provider.Configure(retryGateway, ads);
            Check(await Request(provider) == BitzReviveResult.Unavailable && retryGateway.Lives == 0, "lost spend response");
            Check(await Option(provider) == BitzReviveOption.CheckPending, "lost spend is recoverable");
            Check(await Request(provider) == BitzReviveResult.Confirmed && retryGateway.Lives == 0 && retryGateway.UniqueSpends == 1, "spend retry is idempotent");
            var expiredGateway = new FakeGateway { Lives = 0, StatusExpired = true, ClaimCode = "Granted" };
            var expiredAds = new FakeAds { Receipt = null }; provider.Configure(expiredGateway, expiredAds);
            Check(await Request(provider) == BitzReviveResult.Expired && expiredAds.Calls == 1, "expired attempt reported");
            Check(await Option(provider) == BitzReviveOption.StartNew, "new attempt requires player action");
            expiredAds.Receipt = "test-receipt";
            Check(await Request(provider) == BitzReviveResult.Confirmed && expiredAds.Calls == 2, "new attempt after expiry");
            var switchedGateway = new FakeGateway { Lives = 0, ClaimCode = "Granted" };
            var delayedAd = new FakeAds { Delayed = true }; provider.Configure(switchedGateway, delayedAd);
            var pending = Request(provider);
            switchedGateway.CurrentPlayerId = Guid.NewGuid().ToString();
            delayedAd.Complete();
            Check(await pending == BitzReviveResult.Unavailable && switchedGateway.Lives == 0, "account switch cannot claim another player's reward");
            Debug.Log("Revive flow passed: existing lives, signed receipt recovery, and confirmed spend.");
            Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static Task<BitzReviveResult> Request(BitzRewardedReviveProvider provider) { var t=new TaskCompletionSource<BitzReviveResult>(); provider.RequestRevive(x=>t.TrySetResult(x)); return t.Task; }
    private static Task<BitzReviveOption> Option(BitzRewardedReviveProvider provider) { var t=new TaskCompletionSource<BitzReviveOption>(); provider.GetOption(x=>t.TrySetResult(x)); return t.Task; }
    private static void Check(bool ok,string message){if(!ok)throw new Exception("Revive flow test: "+message);}
    private static void Finish(int code){SessionState.SetBool(RunningKey,false);EditorApplication.Exit(code);}
    private sealed class FakeAds : IBitzRewardedAdPresenter
    {
        public int Calls;
        public string Receipt = "test-receipt";
        public bool Delayed;
        private TaskCompletionSource<(BitzAds.RewardedDisplayResult, string)> pending;
        public Task<(BitzAds.RewardedDisplayResult result,string receipt)> ShowAsync(string appId,string unit,string data,Action<string> receiptAvailable)
        {
            Calls++;
            if(Delayed){pending=new TaskCompletionSource<(BitzAds.RewardedDisplayResult,string)>();return pending.Task;}
            if(!string.IsNullOrEmpty(Receipt))receiptAvailable(Receipt);
            return Task.FromResult((BitzAds.RewardedDisplayResult.VerificationPending,Receipt));
        }
        public void Complete()=>pending.SetResult((BitzAds.RewardedDisplayResult.VerificationPending,Receipt));
    }
    private sealed class FakeGateway : IBitzRewardGateway
    {
        public string CurrentPlayerId { get; set; } = Guid.NewGuid().ToString();
        public int Lives;
        public string ClaimCode;
        public bool ThrowAfterSpendOnce;
        public bool StatusExpired;
        public int UniqueSpends => spent.Count;
        private readonly HashSet<string> spent = new HashSet<string>();
        public Task<BitzRewardReply> GetLivesAsync()=>Task.FromResult(new BitzRewardReply { Code="Ok",Lives=Lives });
        public Task<BitzRewardReply> SpendLifeAsync(string requestId,string mapId)
        {
            if(spent.Contains(requestId))return Task.FromResult(new BitzRewardReply {Code="Ok",Lives=Lives});
            if(Lives==0)return Task.FromResult(new BitzRewardReply {Code="NoLives",Lives=0});
            spent.Add(requestId);Lives--;
            if(ThrowAfterSpendOnce){ThrowAfterSpendOnce=false;throw new InvalidOperationException("Simulated lost response");}
            return Task.FromResult(new BitzRewardReply {Code="Ok",Lives=Lives});
        }
        public Task<BitzRewardReply> PrepareAsync(string id)=>Task.FromResult(new BitzRewardReply {Code="Prepared",AttemptId=Guid.NewGuid().ToString(),AppId=Guid.NewGuid().ToString(),AdUnitId=Guid.NewGuid().ToString(),CustomData="attempt-token"});
        public Task<BitzRewardReply> GetStatusAsync(string attempt)=>Task.FromResult(new BitzRewardReply {Code="Pending",AttemptExpired=StatusExpired});
        public Task<BitzRewardReply> SubmitReceiptAsync(string receipt)
        {
            if(ClaimCode=="Granted")Lives++;
            return Task.FromResult(new BitzRewardReply {Code=ClaimCode});
        }
    }
}
#endif
