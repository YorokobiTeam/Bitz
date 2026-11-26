# mGAds sandbox integration

The project targets Unity **6000.0.50f1**. The manifest pins `com.yorokobi.mgads-sdk` at version **0.3.0** (`Documentation/Readme.md` in Package Manager documents the SDK API and platform requirements).

## Configuration

### 1. Ad platform (`Assets/Resources/MGAdsConfig.json`)

Copy `Assets/Resources/MGAdsConfig.example.json` to `Assets/Resources/MGAdsConfig.json` (git-ignored) and fill in the sandbox credentials and ad-unit UUIDs for your app (`com.test.bitz`):

```json
{
  "sandboxSdkKey": "<your-sandbox-sdk-key>",
  "bannerAdUnitId": "<your-banner-ad-unit-uuid>",
  "interstitialAdUnitId": "<your-interstitial-ad-unit-uuid>",
  "rewardedAdUnitId": "<your-rewarded-ad-unit-uuid>",
  "baseUrl": "",
  "editorMock": false
}
```

- `MGAdsConfig.json` and its `.meta` are git-ignored; do not commit keys or ad-unit IDs. It is embedded in builds and is not a secret store.
- Leave `baseUrl` blank for the SDK default gateway, or set your deployment's HTTPS gateway URL.
- Each ad format (`bannerAdUnitId`, `interstitialAdUnitId`, `rewardedAdUnitId`) must use a matching ad-unit format created in the mGAds portal. In development builds, `BitzAds` logs warnings if an ID is missing, malformed, duplicated, or returns a creative format mismatch.
- `editorMock` enables offline Editor simulation instead of real initialization and is ignored in player builds.
- Use sandbox credentials only.

### 2. Game server (`Assets/Resources/BitzLocalServerConfig.json` or `BitzRewardServerConfig.json`)

- **Auto-connected demo server (`BitzLocalServerClient` + `BitzDemoRewardGateway`):**
  - Automatically signs in before the first scene using a persistent local player name (`PlayerPrefs`) and retries with exponential backoff if unreachable. Gameplay and menus remain fully playable offline.
  - Server URL resolution order:
    1. Optional dev override saved in `PlayerPrefs` (editable from the hidden dev overlay).
    2. Optional `Assets/Resources/BitzLocalServerConfig.json`:
       ```json
       {
         "serverUrl": "https://your-hosted-bitz-server.example.com",
         "allowSimulatedConfirmation": true
       }
       ```
    3. Default `http://127.0.0.1:5030` in Unity Editor and Development builds only.
  - Non-development builds accept HTTPS URLs only, and `allowSimulatedConfirmation` is compiled out outside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- **Authenticated MagicOnion server (`BitzMagicRewardGateway`):**
  - Configured via `Assets/Resources/BitzRewardServerConfig.json` (`serverUrl` over HTTPS and optional `rootCertificateResource` for self-signed local CA certs).
  - Used automatically by `GameController` whenever configured and a Supabase user session is active; otherwise falls back to `BitzDemoRewardGateway`.

## Consent and privacy

On first `MainMenu` entry, Bitz asks whether the player allows sandbox ad requests. The saved choice defaults to disabled (`PlayerPrefs` key `bitz.sandboxAdsChoice.v1`) and can be changed anytime via **Ad Privacy** on the main menu. Declining prevents initialization and future loads, destroys any active banner or prepared interstitial, and surfaces a clear message if the player tries to watch a rewarded ad.

This all-or-nothing sandbox choice is not an IAB CMP, does not generate TCF/GPP strings, and is not an iOS ATT prompt. Before a production launch, provide a proper privacy notice, jurisdiction-specific CMP/consent integration, and iOS ATT flow.

## Ad lifecycle

`BitzAds` starts once before the first scene (`DontDestroyOnLoad`) and initializes on the main thread when allowed and configured:

- **Banner (`BannerAdView`):** Automatically loads and anchors to the bottom of `MainMenu` when `bannerAdUnitId` is configured. Automatically destroyed when entering `BitzPlayer` so gameplay lanes and touch pads are never covered. Supports both image creatives and Android/iOS HTML creatives (`MarkupWebView`).
- **Interstitial (`InterstitialAd`):** Preloads when a song starts, on death, after a revive, and when a song ends (`GameState.Ended`). Tapping **Back to menu** on the death panel or **Retry** / **Back to Home** on the end screen shows an unexpired prepared ad before navigating, or navigates immediately if none is ready. Loads completing after navigation are discarded.
- **Rewarded (`RewardedAd` + `BitzRewardedReviveProvider` + `BitzHealth`):**
  - Rewarded ads are **not** preloaded at startup because `AdRequest.RewardVerificationCustomData` is bound per server-prepared reward attempt.
  - When HP reaches 0, `BitzHealth` queries the server for lives (`Use one life` when lives > 0, or `Watch ad to revive` when lives == 0) and resolves promptly if offline.
  - Pressing **Watch ad to revive** ensures the player is signed in, checks ad availability, prepares a server-owned attempt, loads and plays the rewarded ad with the attempt's binding data, confirms completion with the server, and only restores HP on `BitzReviveResult.Confirmed`.
  - Closing a rewarded ad early never grants a revive. Clear status messages are shown in the death panel for offline, consent disabled, SDK initializing, no-fill, format mismatch, and early close.
- **Audio & input:** The SDK pauses and restores `AudioListener` audio during fullscreen playback, and Bitz suppresses hit input, hit SFX, and button actions while `BitzAds.IsShowing` is true.

## Developer debug overlay

In Unity Editor and Development builds (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`), `BitzDebugOverlay` starts hidden by default and toggles with **F1** (desktop) or a **four-finger tap** (touchscreen), revealing `BitzLocalServerPanel` (connection state, server URL override, manual life/revive controls) and `BitzAdsDiagnostics` (SDK/banner status and manual ad triggers).

## Measurement, verification, and testing

- The SDK reports signed impression and click events (with durable offline retry), and measures `VIDEO` / `REWARDED` impressions via Proof of Playback (`pop`) when enabled on the platform.
- Never grant rewards from the client callback alone. In `UNITY_EDITOR || DEVELOPMENT_BUILD`, `BitzDemoRewardGateway` can confirm a completed rewarded ad against the demo server's `/demo/revive/simulate` endpoint only after `RewardedDisplayResult.VerificationPending` fires; real server-to-server (SSV) or signed PoP receipt verification is handled through `BitzMagicRewardGateway`.
- HTML creatives require Android/iOS device testing (`NotSupported` in Editor). Ensure Android Package Name matches `com.test.bitz` in Unity Player Settings before building an APK.
