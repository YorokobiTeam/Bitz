using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yorokobi.mGAds.Api;

// This service is invoked on Unity's main thread. A missing configuration leaves the game ad-free.
public sealed class BitzAds : MonoBehaviour
{
    public enum RewardedDisplayResult { Unavailable, Closed, VerificationPending }

    public enum AdFailureReason
    {
        None,
        AdvertisingNotAllowed,
        SdkNotReady,
        NotConfigured,
        Busy,
        NoFill,
        FormatMismatch,
        NetworkError,
        LoadFailed
    }

    [Serializable]
    private sealed class Settings
    {
        public string sandboxSdkKey;
        public string bannerAdUnitId;
        public string interstitialAdUnitId;
        public string rewardedAdUnitId;
        public string baseUrl;
        public bool editorMock;
    }

    private static BitzAds instance;
    private const string ConsentKey = "bitz.sandboxAdsChoice.v1";
    private const string MainMenuSceneName = "MainMenu";

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
    private bool bannerRequested;
    private bool bannerLoaded;
    private GameObject bannerCanvasRoot;
    private BannerAdView bannerView;
    private RawImage bannerImage;
    private string currentSceneName;
    private string diagnosticStatus = "Advertising has not been initialized.";
    private string bannerDiagnosticStatus = "Banner has not been loaded.";
    private AdFailureReason lastRewardedFailure = AdFailureReason.None;
    private string verifiedAppId;
    private Action<RewardedDisplayResult, string> rewardFinished;
    private string earnedReceipt;

    public static bool IsShowing => instance != null && instance.showing;
    public static bool IsReady => instance != null && instance.ready;
    public static bool IsInitializing => instance != null && instance.initializing;
    public static bool HasInterstitial => instance != null && instance.interstitial != null;
    public static bool HasBannerLoaded => instance != null && instance.bannerLoaded;
    public static bool HasBannerAdUnit => instance != null && instance.settings != null && !string.IsNullOrWhiteSpace(instance.settings.bannerAdUnitId);
    public static bool HasInterstitialAdUnit => instance != null && instance.settings != null && !string.IsNullOrWhiteSpace(instance.settings.interstitialAdUnitId);
    public static bool HasRewardedAdUnit => instance != null && instance.settings != null && !string.IsNullOrWhiteSpace(instance.settings.rewardedAdUnitId);
    public static string DiagnosticStatus => instance == null ? "Advertising service is unavailable." : instance.diagnosticStatus;
    public static string BannerDiagnosticStatus => instance == null ? "Advertising service is unavailable." : instance.bannerDiagnosticStatus;
    public static AdFailureReason LastRewardedFailure => instance == null ? AdFailureReason.NotConfigured : instance.lastRewardedFailure;
    public static bool HasAdvertisingChoice => PlayerPrefs.HasKey(ConsentKey);
    public static bool AdvertisingAllowed => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

    internal static string VerifiedAppId => instance != null ? instance.verifiedAppId : null;
    internal static string ConfiguredRewardedAdUnitId =>
        instance != null && instance.settings != null && !string.IsNullOrWhiteSpace(instance.settings.rewardedAdUnitId)
            ? instance.settings.rewardedAdUnitId.Trim()
            : null;

    /// <summary>Checks whether a rewarded ad can currently be requested without leaking config values.</summary>
    public static AdFailureReason CheckRewardedAvailability()
    {
        if (instance == null || instance.settings == null) return AdFailureReason.NotConfigured;
        if (!AdvertisingAllowed) return AdFailureReason.AdvertisingNotAllowed;
        if (instance.showing || instance.rewardLoading) return AdFailureReason.Busy;
        if (!instance.ready)
        {
            if (!instance.initializing) instance.Initialize();
            return AdFailureReason.SdkNotReady;
        }
        if (string.IsNullOrWhiteSpace(instance.verifiedAppId) || string.IsNullOrWhiteSpace(instance.settings.rewardedAdUnitId))
            return AdFailureReason.NotConfigured;
        return AdFailureReason.None;
    }

    public static string DescribeFailure(AdFailureReason reason)
    {
        switch (reason)
        {
            case AdFailureReason.AdvertisingNotAllowed:
                return "Ads are disabled. Enable 'Allow sandbox ads' in Main Menu -> Ad Privacy.";
            case AdFailureReason.SdkNotReady:
                return "Ads are still initializing. Please try again in a moment.";
            case AdFailureReason.NotConfigured:
                return "Rewarded ad slot is not configured in MGAdsConfig.json.";
            case AdFailureReason.Busy:
                return "An ad is already loading or showing.";
            case AdFailureReason.NoFill:
                return "No ad is available right now (no fill). Please try again.";
            case AdFailureReason.FormatMismatch:
                return "The configured ad slot format does not match a rewarded ad.";
            case AdFailureReason.NetworkError:
                return "Unable to reach the ad server. Check your network connection.";
            case AdFailureReason.LoadFailed:
                return "Could not load the rewarded ad. Please try again.";
            default:
                return "Revive is unavailable. Please try again later.";
        }
    }

    public static void SetAdvertisingAllowed(bool allowed)
    {
        PlayerPrefs.SetInt(ConsentKey, allowed ? 1 : 0);
        PlayerPrefs.Save();
        if (instance == null) return;
        if (allowed)
        {
            if (instance.ready)
            {
                instance.diagnosticStatus = "Advertising initialized.";
                if (instance.IsMainMenuActive()) instance.LoadBannerInternal();
            }
            else
            {
                if (instance.IsMainMenuActive()) instance.bannerRequested = true;
                instance.Initialize();
            }
        }
        else
        {
            instance.generation++;
            instance.loading = false;
            instance.preloadWhenReady = false;
            instance.bannerRequested = false;
            instance.diagnosticStatus = "Advertising is disabled.";
            instance.bannerDiagnosticStatus = "Advertising is disabled.";
            instance.interstitial?.Destroy();
            instance.interstitial = null;
            instance.DestroyBannerInternal();
            instance.FinishRewarded(RewardedDisplayResult.Unavailable);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartService()
    {
        var go = new GameObject("Bitz mGAds");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BitzAds>();
        instance.currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += instance.HandleSceneLoaded;
        instance.LoadSettings();
        if (AdvertisingAllowed) instance.Initialize();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        if (IsMainMenuActive())
        {
            if (HasBannerAdUnit) ShowBanner();
        }
        else
        {
            // Never cover the gameplay lanes or touch pads with a banner.
            DestroyBannerInternal();
        }
    }

    private bool IsMainMenuActive() =>
        string.Equals(currentSceneName, MainMenuSceneName, StringComparison.OrdinalIgnoreCase);

    private void LoadSettings()
    {
        var config = Resources.Load<TextAsset>("MGAdsConfig");
        if (config == null) { diagnosticStatus = "mGAds configuration is missing."; return; }
        try { settings = JsonUtility.FromJson<Settings>(config.text); }
        catch (Exception e) { Debug.LogWarning("mGAds configuration could not be read: " + e.Message); return; }
        ValidateSettingsForDev();
    }

    private void ValidateSettingsForDev()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (settings == null || settings.editorMock) return;

        var banner = string.IsNullOrWhiteSpace(settings.bannerAdUnitId) ? null : settings.bannerAdUnitId.Trim();
        var interstitialId = string.IsNullOrWhiteSpace(settings.interstitialAdUnitId) ? null : settings.interstitialAdUnitId.Trim();
        var rewardedId = string.IsNullOrWhiteSpace(settings.rewardedAdUnitId) ? null : settings.rewardedAdUnitId.Trim();

        if (banner == null)
            Debug.LogWarning("mGAds config: bannerAdUnitId is empty (MainMenu banner will not load).");
        else if (!Guid.TryParse(banner, out _))
            Debug.LogWarning("mGAds config: bannerAdUnitId is not a valid UUID.");

        if (interstitialId == null)
            Debug.LogWarning("mGAds config: interstitialAdUnitId is empty (post-song interstitials will not load).");
        else if (!Guid.TryParse(interstitialId, out _))
            Debug.LogWarning("mGAds config: interstitialAdUnitId is not a valid UUID.");

        if (rewardedId == null)
            Debug.LogWarning("mGAds config: rewardedAdUnitId is empty (revive rewarded ads will not load).");
        else if (!Guid.TryParse(rewardedId, out _))
            Debug.LogWarning("mGAds config: rewardedAdUnitId is not a valid UUID.");

        if (banner != null && interstitialId != null && string.Equals(banner, interstitialId, StringComparison.OrdinalIgnoreCase))
            Debug.LogWarning("mGAds config: bannerAdUnitId and interstitialAdUnitId are identical; each format requires its own ad unit.");
        if (banner != null && rewardedId != null && string.Equals(banner, rewardedId, StringComparison.OrdinalIgnoreCase))
            Debug.LogWarning("mGAds config: bannerAdUnitId and rewardedAdUnitId are identical; each format requires its own ad unit.");
        if (interstitialId != null && rewardedId != null && string.Equals(interstitialId, rewardedId, StringComparison.OrdinalIgnoreCase))
            Debug.LogWarning("mGAds config: interstitialAdUnitId and rewardedAdUnitId are identical; each format requires its own ad unit.");
#endif
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

        var shouldLoadBanner = bannerRequested || IsMainMenuActive();
        bannerRequested = false;
        if (ready && shouldLoadBanner && AdvertisingAllowed && HasBannerAdUnit) LoadBannerInternal();
    }

    public static void ShowBanner()
    {
        if (instance == null || !AdvertisingAllowed) return;
        if (!HasBannerAdUnit)
        {
            instance.bannerDiagnosticStatus = "Banner ad unit is missing.";
            return;
        }
        if (!instance.ready)
        {
            instance.bannerRequested = true;
            instance.Initialize();
            return;
        }
        instance.LoadBannerInternal();
    }

    public static void HideBanner()
    {
        if (instance == null) return;
        instance.bannerRequested = false;
        instance.DestroyBannerInternal();
    }

    private void LoadBannerInternal()
    {
        if (!AdvertisingAllowed || !ready || settings == null) return;
        if (string.IsNullOrWhiteSpace(settings.bannerAdUnitId))
        {
            bannerDiagnosticStatus = "Banner ad unit is missing.";
            return;
        }

        EnsureBannerView();
        if (bannerView == null) return;

        bannerLoaded = false;
        if (bannerImage != null) bannerImage.enabled = false;
        bannerDiagnosticStatus = "Loading banner ad.";
        try
        {
            bannerView.Load(settings.bannerAdUnitId.Trim(), new AdRequest());
        }
        catch (Exception e)
        {
            bannerDiagnosticStatus = "Banner load failed: " + e.Message;
            Debug.LogWarning("mGAds banner load failed: " + e.Message);
        }
    }

    private void EnsureBannerView()
    {
        if (bannerView != null) return;

        bannerCanvasRoot = new GameObject("Bitz Banner Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(bannerCanvasRoot);
        var canvas = bannerCanvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = bannerCanvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        var bannerGo = new GameObject("Banner Ad View", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(BannerAdView));
        bannerGo.transform.SetParent(bannerCanvasRoot.transform, false);

        bannerImage = bannerGo.GetComponent<RawImage>();
        bannerImage.color = Color.white;
        bannerImage.enabled = false;

        bannerView = bannerGo.GetComponent<BannerAdView>();
        BindBannerImageField(bannerView, bannerImage);
        bannerView.Anchor = BannerAnchor.Bottom;
        bannerView.HeightDp = 90f;
        bannerView.OnAdLoaded += HandleBannerLoaded;
        bannerView.OnAdFailedToLoad += HandleBannerFailed;
        bannerView.OnAdClicked += () => bannerDiagnosticStatus = "Banner clicked.";
    }

    private static void BindBannerImageField(BannerAdView view, RawImage image)
    {
        if (view == null || image == null) return;
        var field = typeof(BannerAdView).GetField("creativeImage", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(view, image);
    }

    private void HandleBannerLoaded(AdResponse response)
    {
        if (this == null) return;
        bannerLoaded = true;
        bannerDiagnosticStatus = "Banner ad is showing.";
    }

    private void HandleBannerFailed(LoadAdError error)
    {
        if (this == null) return;
        bannerLoaded = false;
        if (bannerImage != null) bannerImage.enabled = false;
        if (error == null)
        {
            bannerDiagnosticStatus = "Banner load failed.";
            return;
        }
        bannerDiagnosticStatus = $"Banner load failed: {error.Code} (HTTP {error.HttpStatusCode}).";
        LogLoadFailureWarning("banner", "bannerAdUnitId", error);
    }

    private void DestroyBannerInternal()
    {
        bannerLoaded = false;
        if (bannerView != null)
        {
            bannerView.OnAdLoaded -= HandleBannerLoaded;
            bannerView.OnAdFailedToLoad -= HandleBannerFailed;
            bannerView = null;
        }
        bannerImage = null;
        if (bannerCanvasRoot != null)
        {
            Destroy(bannerCanvasRoot);
            bannerCanvasRoot = null;
        }
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
                LogLoadFailureWarning("interstitial", "interstitialAdUnitId", error);
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
        lastRewardedFailure = AdFailureReason.None;
        if (!AdvertisingAllowed)
        {
            lastRewardedFailure = AdFailureReason.AdvertisingNotAllowed;
            diagnosticStatus = DescribeFailure(lastRewardedFailure);
            finished?.Invoke(RewardedDisplayResult.Unavailable, null);
            return;
        }
        if (showing || rewardLoading)
        {
            lastRewardedFailure = AdFailureReason.Busy;
            diagnosticStatus = DescribeFailure(lastRewardedFailure);
            finished?.Invoke(RewardedDisplayResult.Unavailable, null);
            return;
        }
        if (!ready)
        {
            if (!initializing) Initialize();
            lastRewardedFailure = AdFailureReason.SdkNotReady;
            diagnosticStatus = DescribeFailure(lastRewardedFailure);
            finished?.Invoke(RewardedDisplayResult.Unavailable, null);
            return;
        }
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(adUnitId) || (!diagnosticOnly && string.IsNullOrWhiteSpace(customData)))
        {
            lastRewardedFailure = AdFailureReason.NotConfigured;
            diagnosticStatus = "Rewarded ad is unavailable or not configured.";
            finished?.Invoke(RewardedDisplayResult.Unavailable, null);
            return;
        }

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
            RewardedAd.Load(adUnitId.Trim(), request, (ad, error) =>
            {
                if (this == null || requestGeneration != generation || !rewardLoading)
                { ad?.Destroy(); return; }
                rewardLoading = false;
                if (error != null || ad == null || !AdvertisingAllowed)
                {
                    if (!AdvertisingAllowed)
                    {
                        lastRewardedFailure = AdFailureReason.AdvertisingNotAllowed;
                        diagnosticStatus = "Advertising is disabled.";
                    }
                    else if (error != null)
                    {
                        lastRewardedFailure = ClassifyLoadError(error);
                        diagnosticStatus = $"Rewarded load failed: {error.Code} (HTTP {error.HttpStatusCode}).";
                        LogLoadFailureWarning("rewarded", "rewardedAdUnitId", error);
                    }
                    else
                    {
                        lastRewardedFailure = AdFailureReason.LoadFailed;
                        diagnosticStatus = "Rewarded ad is unavailable.";
                    }
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
                ad.OnAdFullScreenContentFailed += playError =>
                {
                    lastRewardedFailure = ClassifyLoadError(playError);
                    FinishRewarded(RewardedDisplayResult.Unavailable);
                };
                try { ad.Show(null); }
                catch (Exception)
                {
                    lastRewardedFailure = AdFailureReason.LoadFailed;
                    FinishRewarded(RewardedDisplayResult.Unavailable);
                }
            });
        }
        catch (Exception)
        {
            lastRewardedFailure = AdFailureReason.LoadFailed;
            FinishRewarded(RewardedDisplayResult.Unavailable);
        }
    }

    private static AdFailureReason ClassifyLoadError(LoadAdError error)
    {
        if (error == null) return AdFailureReason.LoadFailed;
        if (error.Code == AdErrorCode.NoFill) return AdFailureReason.NoFill;
        if (error.Code == AdErrorCode.NetworkError || error.Code == AdErrorCode.Timeout) return AdFailureReason.NetworkError;
        if (IsFormatMismatch(error)) return AdFailureReason.FormatMismatch;
        return AdFailureReason.LoadFailed;
    }

    private static bool IsFormatMismatch(LoadAdError error)
    {
        if (error == null || error.Code != AdErrorCode.InvalidResponse || string.IsNullOrEmpty(error.Message)) return false;
        return error.Message.IndexOf("creative", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.Message.IndexOf("non-banner", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void LogLoadFailureWarning(string expectedFormat, string configFieldName, LoadAdError error)
    {
        if (error == null) return;
        if (IsFormatMismatch(error))
        {
            Debug.LogWarning($"mGAds {expectedFormat} format mismatch ({error.Message}). Check that '{configFieldName}' in MGAdsConfig.json points to a {expectedFormat} ad unit.");
            return;
        }
        Debug.LogWarning($"mGAds {expectedFormat} load failed: {error.Code}, HTTP {error.HttpStatusCode}, request {error.RequestId}");
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
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        interstitial?.Destroy();
        DestroyBannerInternal();
        FinishRewarded(RewardedDisplayResult.Unavailable);
        if (instance == this) instance = null;
    }
}
