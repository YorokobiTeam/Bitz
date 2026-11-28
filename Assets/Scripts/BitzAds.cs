using System;
using UnityEngine;
using Yorokobi.mGAds.Api;

// This service is invoked on Unity's main thread. A missing configuration leaves the game ad-free.
public sealed class BitzAds : MonoBehaviour
{
    public enum RewardedDisplayResult { Unavailable, Closed, VerificationPending }
    [Serializable]
    private sealed class Settings
    {
        public string sandboxSdkKey;
        public string interstitialAdUnitId;
        public string rewardedAdUnitId;
        public string baseUrl;
        public bool editorMock;
    }

    private static BitzAds instance;
    private const string ConsentKey = "bitz.sandboxAdsChoice.v1";
    private Settings settings;
    private bool ready;
    private bool initializing;
    private bool loading;
    private bool showing;
    private int generation;
    private float loadedAt;
    private InterstitialAd interstitial;
    private RewardedAd rewarded;
    private bool rewardLoading;
    private bool preloadWhenReady;
    private string diagnosticStatus = "Advertising has not been initialized.";
    private string verifiedAppId;
    private Action<RewardedDisplayResult, string> rewardFinished;
    private string earnedReceipt;

    public static bool IsShowing => instance != null && instance.showing;
    public static bool IsReady => instance != null && instance.ready;
    public static bool HasInterstitial => instance != null && instance.interstitial != null;
    public static string DiagnosticStatus => instance == null ? "Advertising service is unavailable." : instance.diagnosticStatus;
    public static bool HasAdvertisingChoice => PlayerPrefs.HasKey(ConsentKey);
    public static bool AdvertisingAllowed => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

    public static void SetAdvertisingAllowed(bool allowed)
    {
        PlayerPrefs.SetInt(ConsentKey, allowed ? 1 : 0);
        PlayerPrefs.Save();
        if (instance == null) return;
        if (allowed)
        {
            if (instance.ready) instance.diagnosticStatus = "Advertising initialized.";
            else instance.Initialize();
        }
        else
        {
            instance.generation++;
            instance.loading = false;
            instance.preloadWhenReady = false;
            instance.diagnosticStatus = "Advertising is disabled.";
            instance.interstitial?.Destroy();
            instance.interstitial = null;
            instance.FinishRewarded(RewardedDisplayResult.Unavailable);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartService()
    {
        var go = new GameObject("Bitz mGAds");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BitzAds>();
        instance.LoadSettings();
        if (AdvertisingAllowed) instance.Initialize();
    }

    private void LoadSettings()
    {
        var config = Resources.Load<TextAsset>("MGAdsConfig");
        if (config == null) { diagnosticStatus = "mGAds configuration is missing."; return; }
        try { settings = JsonUtility.FromJson<Settings>(config.text); }
        catch (Exception e) { Debug.LogWarning("mGAds configuration could not be read: " + e.Message); return; }
    }

    private void Initialize()
    {
        if (!AdvertisingAllowed || settings == null || ready || initializing) return;
#if UNITY_EDITOR
        if (settings.editorMock)
        {
            initializing = true;
            diagnosticStatus = "Initializing editor mock ads.";
            MGAdsSdk.InitializeMock(HandleInitialized);
            return;
        }
#endif
        if (string.IsNullOrWhiteSpace(settings.sandboxSdkKey)) { diagnosticStatus = "Sandbox SDK key is missing."; return; }
        var sdkConfig = new MGAdsConfig { Environment = MGAdsEnvironment.Sandbox, LogLevel = MGAdsLogLevel.Info };
        if (!string.IsNullOrWhiteSpace(settings.baseUrl)) sdkConfig.BaseUrl = settings.baseUrl.Trim();
        initializing = true;
        diagnosticStatus = "Initializing sandbox ads.";
        MGAdsSdk.Initialize(settings.sandboxSdkKey.Trim(), sdkConfig, HandleInitialized);
    }

    private void HandleInitialized(InitializationStatus status)
    {
        if (this == null) return;
        initializing = false;
        ready = status.IsSuccess;
        verifiedAppId = ready ? status.AppId : null;
        diagnosticStatus = !AdvertisingAllowed ? "Advertising is disabled." : ready ? "Advertising initialized." : $"Initialization failed: {status.ErrorCode} (HTTP {status.HttpStatus}).";
        if (!ready) Debug.LogWarning($"mGAds initialization failed: {status.ErrorCode}");
        var shouldPreload = preloadWhenReady;
        preloadWhenReady = false;
        if (ready && shouldPreload && AdvertisingAllowed) Preload();
    }

    public static void PreloadInterstitial()
    {
        if (instance == null || !AdvertisingAllowed) return;
        if (!instance.ready)
        {
            instance.preloadWhenReady = true;
            instance.Initialize();
            return;
        }
        instance.Preload();
    }

    private void Preload()
    {
        if (!AdvertisingAllowed || !ready || loading || rewardLoading || showing || interstitial != null || settings == null) return;
        if (string.IsNullOrWhiteSpace(settings.interstitialAdUnitId)) { diagnosticStatus = "Interstitial ad unit is missing."; return; }
        loading = true;
        diagnosticStatus = "Loading interstitial ad.";
        int requestGeneration = ++generation;
        InterstitialAd.Load(settings.interstitialAdUnitId.Trim(), new AdRequest(), (ad, error) =>
        {
            if (this == null || requestGeneration != generation)
            {
                ad?.Destroy();
                return;
            }
            loading = false;
            if (error != null)
            {
                diagnosticStatus = $"Interstitial load failed: {error.Code} (HTTP {error.HttpStatusCode}).";
                Debug.LogWarning($"mGAds interstitial load failed: {error.Code}, HTTP {error.HttpStatusCode}, request {error.RequestId}");
                return;
            }
            interstitial = ad;
            diagnosticStatus = ad == null ? "Interstitial load returned no ad." : "Interstitial ad is ready.";
            loadedAt = Time.realtimeSinceStartup;
        });
    }

    public static void ShowInterstitialThen(Action continueAction)
    {
        if (instance == null) { continueAction?.Invoke(); return; }
        instance.ShowThen(continueAction);
    }

    private void ShowThen(Action continueAction)
    {
        if (showing || rewardLoading) return;
        // A load completing after navigation must not show on a later song.
        generation++;
        loading = false;
        var ad = interstitial;
        interstitial = null;
        if (ad == null) { continueAction?.Invoke(); return; }
        if (!AdvertisingAllowed) { ad.Destroy(); continueAction?.Invoke(); return; }
        if (Time.realtimeSinceStartup - loadedAt >= 290f)
        {
            ad.Destroy();
            continueAction?.Invoke();
            return;
        }
        showing = true;
        bool finished = false;
        void Finish()
        {
            if (finished) return;
            finished = true;
            showing = false;
            diagnosticStatus = "Interstitial closed.";
            ad.Destroy();
            continueAction?.Invoke();
        }
        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentOpened += () => diagnosticStatus = "Interstitial is showing.";
        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"mGAds interstitial playback failed: {error.Code}, request {error.RequestId}");
            Finish();
        };
        try { ad.Show(); }
        catch (Exception e) { Debug.LogWarning("mGAds could not show an interstitial: " + e.Message); Finish(); }
    }

    // customData must come from a prepared server-owned attempt. This only displays an ad:
    // the callback tells the caller to query the game server; it never grants a reward.
    public static void ShowRewarded(string appId, string adUnitId, string customData, Action<RewardedDisplayResult> finished)
    {
        if (instance == null) { finished?.Invoke(RewardedDisplayResult.Unavailable); return; }
        instance.LoadRewarded(appId, adUnitId, customData, (result, _) => finished?.Invoke(result), false, null);
    }

    // The receipt is untrusted client input until the game server verifies it.
    public static void ShowRewardedWithReceipt(string appId, string adUnitId, string customData, Action<RewardedDisplayResult, string> finished, Action<string> receiptAvailable = null)
    {
        if (instance == null) { finished?.Invoke(RewardedDisplayResult.Unavailable, null); return; }
        instance.LoadRewarded(appId, adUnitId, customData, finished, false, receiptAvailable);
    }

    // Developer-only sandbox test; no player binding is sent and no game reward is granted.
    public static void ShowSandboxRewardedTest(Action<RewardedDisplayResult> finished)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (instance == null || instance.settings == null) { finished?.Invoke(RewardedDisplayResult.Unavailable); return; }
        instance.LoadRewarded(instance.verifiedAppId, instance.settings.rewardedAdUnitId, null, (result, _) => finished?.Invoke(result), true, null);
#else
        finished?.Invoke(RewardedDisplayResult.Unavailable);
#endif
    }

    private void LoadRewarded(string appId, string adUnitId, string customData, Action<RewardedDisplayResult, string> finished, bool diagnosticOnly, Action<string> receiptAvailable)
    {
        if (!AdvertisingAllowed || !ready || showing || rewardLoading || string.IsNullOrWhiteSpace(appId) ||
            string.IsNullOrWhiteSpace(adUnitId) || (!diagnosticOnly && string.IsNullOrWhiteSpace(customData)))
        { diagnosticStatus = "Rewarded ad is unavailable or not configured."; finished?.Invoke(RewardedDisplayResult.Unavailable, null); return; }
        generation++;
        loading = false;
        interstitial?.Destroy();
        interstitial = null;
        int requestGeneration = generation;
        rewardLoading = true;
        earnedReceipt = null;
        diagnosticStatus = "Loading rewarded ad.";
        rewardFinished = finished;
        var request = new AdRequest { AppId = appId, RewardVerificationCustomData = customData };
        try
        {
            RewardedAd.Load(adUnitId, request, (ad, error) =>
            {
                if (this == null || requestGeneration != generation || !rewardLoading)
                { ad?.Destroy(); return; }
                rewardLoading = false;
                if (error != null || ad == null || !AdvertisingAllowed)
                {
                    if (error != null) diagnosticStatus = $"Rewarded load failed: {error.Code} (HTTP {error.HttpStatusCode}).";
                    else diagnosticStatus = "Rewarded ad is unavailable.";
                    ad?.Destroy(); FinishRewarded(RewardedDisplayResult.Unavailable); return;
                }
                rewarded = ad;
                showing = true;
                bool playbackCompleted = false;
                ad.OnAdFullScreenContentOpened += () => diagnosticStatus = "Rewarded ad is showing.";
                ad.OnUserEarnedReward += reward =>
                {
                    playbackCompleted = true; earnedReceipt = reward?.Receipt;
                    if (!string.IsNullOrEmpty(earnedReceipt)) receiptAvailable?.Invoke(earnedReceipt);
                };
                ad.OnAdFullScreenContentClosed += () => FinishRewarded(playbackCompleted ? RewardedDisplayResult.VerificationPending : RewardedDisplayResult.Closed);
                ad.OnAdFullScreenContentFailed += _ => FinishRewarded(RewardedDisplayResult.Unavailable);
                try { ad.Show(null); }
                catch (Exception) { FinishRewarded(RewardedDisplayResult.Unavailable); }
            });
        }
        catch (Exception) { FinishRewarded(RewardedDisplayResult.Unavailable); }
    }

    private void FinishRewarded(RewardedDisplayResult result)
    {
        var callback = rewardFinished;
        if (callback == null && !rewardLoading && rewarded == null) return;
        rewardFinished = null;
        rewardLoading = false;
        showing = false;
        if (result == RewardedDisplayResult.VerificationPending) diagnosticStatus = "Rewarded playback completed; server verification is required.";
        else if (result == RewardedDisplayResult.Closed) diagnosticStatus = "Rewarded ad closed without a verified game reward.";
        else if (diagnosticStatus == "Loading rewarded ad." || diagnosticStatus == "Rewarded ad is showing.") diagnosticStatus = "Rewarded ad is unavailable.";
        var ad = rewarded;
        var receipt = earnedReceipt;
        earnedReceipt = null;
        rewarded = null;
        ad?.Destroy();
        callback?.Invoke(result, receipt);
    }

    private void OnDestroy()
    {
        generation++;
        interstitial?.Destroy();
        FinishRewarded(RewardedDisplayResult.Unavailable);
        if (instance == this) instance = null;
    }
}
