using System;
using UnityEngine;
using Yorokobi.mGAds.Api;

// This service is invoked on Unity's main thread. A missing configuration leaves the game ad-free.
public sealed class BitzAds : MonoBehaviour
{
    [Serializable]
    private sealed class Settings
    {
        public string sandboxSdkKey;
        public string interstitialAdUnitId;
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

    public static bool IsShowing => instance != null && instance.showing;
    public static bool HasAdvertisingChoice => PlayerPrefs.HasKey(ConsentKey);
    public static bool AdvertisingAllowed => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

    public static void SetAdvertisingAllowed(bool allowed)
    {
        PlayerPrefs.SetInt(ConsentKey, allowed ? 1 : 0);
        PlayerPrefs.Save();
        if (instance == null) return;
        if (allowed) instance.Initialize();
        else
        {
            instance.generation++;
            instance.loading = false;
            instance.interstitial?.Destroy();
            instance.interstitial = null;
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
        if (config == null) return;
        try { settings = JsonUtility.FromJson<Settings>(config.text); }
        catch (Exception e) { Debug.LogWarning("mGAds configuration could not be read: " + e.Message); return; }
    }

    private void Initialize()
    {
        if (!AdvertisingAllowed || settings == null || ready || initializing) return;
#if UNITY_EDITOR
        if (settings.editorMock)
        {
            MGAdsSdk.InitializeMock(status => ready = status.IsSuccess);
            return;
        }
#endif
        if (string.IsNullOrWhiteSpace(settings.sandboxSdkKey)) return;
        var sdkConfig = new MGAdsConfig { Environment = MGAdsEnvironment.Sandbox, LogLevel = MGAdsLogLevel.Info };
        if (!string.IsNullOrWhiteSpace(settings.baseUrl)) sdkConfig.BaseUrl = settings.baseUrl.Trim();
        initializing = true;
        MGAdsSdk.Initialize(settings.sandboxSdkKey.Trim(), sdkConfig, status =>
        {
            initializing = false;
            ready = status.IsSuccess;
            if (!ready) Debug.LogWarning($"mGAds initialization failed: {status.ErrorCode}");
        });
    }

    public static void PreloadInterstitial()
    {
        if (instance != null) instance.Preload();
    }

    private void Preload()
    {
        if (!AdvertisingAllowed || !ready || loading || showing || interstitial != null || settings == null ||
            string.IsNullOrWhiteSpace(settings.interstitialAdUnitId)) return;
        loading = true;
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
                Debug.LogWarning($"mGAds interstitial load failed: {error.Code}, HTTP {error.HttpStatusCode}, request {error.RequestId}");
                return;
            }
            interstitial = ad;
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
        if (showing) return;
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
            ad.Destroy();
            continueAction?.Invoke();
        }
        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"mGAds interstitial playback failed: {error.Code}, request {error.RequestId}");
            Finish();
        };
        try { ad.Show(); }
        catch (Exception e) { Debug.LogWarning("mGAds could not show an interstitial: " + e.Message); Finish(); }
    }

    private void OnDestroy()
    {
        generation++;
        interstitial?.Destroy();
        if (instance == this) instance = null;
    }
}
