using UnityEngine;

/// <summary>Optional local diagnostics panel. Hidden by default; toggle with F1 or a four-finger tap. It never grants a reward.</summary>
public sealed class BitzAdsDiagnostics : MonoBehaviour
{
    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!BitzDebugOverlay.Visible) return;
        GUILayout.BeginArea(new Rect(12, 434, 420, 290), GUI.skin.box);
        GUILayout.Label("mGAds sandbox diagnostics");
        GUILayout.Label("SDK: " + BitzAds.DiagnosticStatus);
        GUILayout.Label("Banner: " + BitzAds.BannerDiagnosticStatus);
        if (!BitzAds.AdvertisingAllowed && GUILayout.Button("Allow sandbox ads")) BitzAds.SetAdvertisingAllowed(true);
        GUI.enabled = BitzAds.AdvertisingAllowed && BitzAds.HasBannerAdUnit;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Show banner")) BitzAds.ShowBanner();
        if (GUILayout.Button("Hide banner")) BitzAds.HideBanner();
        GUILayout.EndHorizontal();
        GUI.enabled = BitzAds.AdvertisingAllowed && !BitzAds.IsShowing;
        if (GUILayout.Button("Load interstitial")) BitzAds.PreloadInterstitial();
        GUI.enabled = BitzAds.HasInterstitial && !BitzAds.IsShowing;
        if (GUILayout.Button("Show loaded interstitial")) BitzAds.ShowInterstitialThen(null);
        GUI.enabled = BitzAds.AdvertisingAllowed && BitzAds.IsReady && !BitzAds.IsShowing;
        if (GUILayout.Button("Test rewarded ad (no revive)"))
            BitzAds.ShowSandboxRewardedTest(result => Debug.Log("Sandbox rewarded test ended: " + result));
        GUI.enabled = true;
        GUILayout.EndArea();
#endif
    }
}
