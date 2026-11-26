# Bitz ↔ mGAds & Bitz-Server Setup Guide

A quick setup guide for teammates to run **Bitz (Unity)** with **Bitz-Server (.NET)** and **mGAds** (Banner, Interstitial, and Rewarded ads).

> **Security rule:** Never commit real SDK keys, ad unit IDs, or signing secrets. Both `Assets/Resources/MGAdsConfig.json` (Unity) and `appsettings.RewardLocal.json` (`Bitz-Server`) are git-ignored — keep them local.

---

## 1. Prerequisites

- **Unity:** `6000.0.50f1` (with Android Build Support if building an APK)
- **.NET SDK:** `.NET 10` (for `Bitz-Server`)
- **mGAds Developer Portal account** (`https://mgads.maik.io.vn`) with:
  - A registered Unity app (e.g., `Bitz-Ads_demo`, package name `com.test.bitz`)
  - A **Sandbox SDK Key** (`am_test_...`)
  - **3 separate active Ad Units** under that app (one **Banner**, one **Interstitial**, one **Rewarded**)

### Where to find each ID in the mGAds Portal

| Value | Where to find it | Example format |
|---|---|---|
| **App ID** (`AppId`) | Open your app in the portal and copy the UUID from the browser URL: `/developer/apps/<APP_ID>?name=...` | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| **Sandbox SDK Key** | App detail page → **SDK Key** card (sandbox/test key) | `am_test_...` |
| **Ad Unit IDs (×3)** | App detail page → **Ad Units** (create 1 Banner, 1 Interstitial, 1 Rewarded) | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| **Reward Key ID + Signing Secret** *(only for real receipt/SSV verification)* | App detail page → **Callback phần thưởng (Rewarded video)** → click **Tạo secret mới** and copy **both** values from the yellow box immediately (the secret is shown only once) | Key ID: `rk_...`<br>Secret: 64 hex chars |

---

## 2. Unity Project Setup (`Bitz`)

### 2.1 Open the project & verify packages
1. Open the `Bitz` folder in Unity **6000.0.50f1**.
2. `Packages/manifest.json` already pins `com.yorokobi.mgads-sdk` (`v0.3.0`) and MagicOnion dependencies — let Unity resolve packages on open and confirm there are no Console errors.

### 2.2 Create `Assets/Resources/MGAdsConfig.json`
1. Copy `Assets/Resources/MGAdsConfig.example.json` to `Assets/Resources/MGAdsConfig.json` (git-ignored).
2. Fill in your sandbox SDK key and all **3 distinct** ad unit UUIDs:

```json
{
  "sandboxSdkKey": "<YOUR_SANDBOX_SDK_KEY>",
  "bannerAdUnitId": "<YOUR_BANNER_AD_UNIT_UUID>",
  "interstitialAdUnitId": "<YOUR_INTERSTITIAL_AD_UNIT_UUID>",
  "rewardedAdUnitId": "<YOUR_REWARDED_AD_UNIT_UUID>",
  "baseUrl": "",
  "editorMock": false
}
```

- Leave `"baseUrl": ""` to use the default gateway (`https://api.mgads.maik.io.vn`), or set a custom HTTPS gateway URL if testing another deployment.
- Keep `"editorMock": false` for real sandbox ads (set `true` only if you want offline Editor mock overlays with no network calls).
- Each format must have its **own** matching ad unit UUID; reusing a banner slot for interstitial/rewarded will fail format validation.

### 2.3 Optional: Configure `Bitz-Server` URL (`Assets/Resources/BitzLocalServerConfig.json`)
- **Local PC testing (Editor / Development Build):** You can skip this file — Unity automatically connects to `http://127.0.0.1:5030`.
- **Android APK / Hosted server:** Create `Assets/Resources/BitzLocalServerConfig.json` (git-ignored):

```json
{
  "serverUrl": "https://<YOUR_HOSTED_BITZ_SERVER>",
  "allowSimulatedConfirmation": true
}
```

> Non-development release builds require `https://` and automatically compile out `allowSimulatedConfirmation`.

### 2.4 Android Build Settings (when building an APK)
1. **Project Settings → Player → Android**:
   - **Other Settings → Identification → Override Default Package Name:** enable and set to `com.test.bitz` (must match the package name registered in mGAds).
   - **Other Settings → Configuration → Internet Access:** set to `Require`.
2. **File → Build Profiles (or Build Settings) → Android**:
   - Ensure `MainMenu` and `BitzPlayer` are checked in the scene list.
   - Check **Development Build** if you are using the local demo server's simulated revive confirmation and the hidden debug overlay.

---

## 3. Game Server Setup (`Bitz-Server`)

### 3.1 Quick Start (Local Demo Mode)
For local Unity Play Mode testing, you don't need PostgreSQL or HTTPS. Run from the `Bitz-Server` directory:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile
```

- Listens on `http://localhost:5030` (HTTP demo routes) and `http://localhost:5031` (MagicOnion).
- Unity auto-signs in on startup with a persistent demo player (`3` starting lives).
- Demo player state is stored locally in the git-ignored `demo-data/players.json`.

### 3.2 Configure `appsettings.RewardLocal.json` (for real SSV / Receipt Verification)
If you are setting up **real** mGAds reward verification (signed PoP receipts or SSV callbacks), copy `appsettings.RewardLocal.example.json` to `appsettings.RewardLocal.json` (git-ignored) and fill in:

```json
{
  "MgAdsCallback": {
    "AppId": "<YOUR_MGADS_APP_UUID>",
    "ReceiptProcessingEnabled": false,
    "Keys": {
      "<YOUR_RK_KEY_ID>": "<YOUR_64_HEX_SIGNING_SECRET>"
    }
  }
}
```

**Common mistakes to avoid:**
1. **`AppId` must be the Application UUID**, *not* the Rewarded Ad Unit UUID. Grab it from the portal URL (`/developer/apps/<APP_ID>`).
2. **`Keys` maps `Key ID → Signing Secret`**:
   - The **left side** (JSON property name) is the `rk_...` Key ID (replace `"REPLACE_WITH_KEY_ID"`).
   - The **right side** (JSON value) is the **64-character hex signing secret** shown once when you click **Tạo secret mới**.
   - Do **not** paste `rk_...` on the right side — if you didn't save the 64-hex secret when creating it, generate a new secret in the portal and use the new pair.
3. **Leave `URL callback (https)` blank in the portal while testing on localhost** (mGAds rejects `localhost` / private IP callback URLs). Only set `https://<your-domain>/mgads/reward` once `Bitz-Server` is deployed on a public HTTPS host.
4. Set `"ReceiptProcessingEnabled": true` (or run `scripts/setup-local-reward.ps1` with PostgreSQL installed) when ready to switch from `/demo/*` simulated grants to full signed receipt verification.

---

## 4. Testing All 3 Ad Formats (60-Second Walkthrough)

1. Start `Bitz-Server` (Section 3.1), then press **Play** in Unity starting from `MainMenu`.
2. On first launch, accept **Allow sandbox ads** (can be toggled anytime under **Ad Privacy**).
3. **Banner:** Appears automatically at the bottom of `MainMenu`, and hides automatically when entering a song (`BitzPlayer`).
4. **Rewarded (Revive):**
   - Start any song and miss 5 notes so HP reaches `0`.
   - New demo accounts start with **3 lives** (`Use one life`). Spend those 3 lives (or use the `F1` overlay's **Simulate song start** button) so lives = `0` and the button changes to **Watch ad to revive**.
   - Test **early close** once: closing the video early warns *"skipping means no rewards"* and does **not** restore HP.
   - Tap **Watch ad to revive** again and watch to completion: `Bitz-Server` confirms the revive and gameplay resumes at full HP.
5. **Interstitial:**
   - Die in a song and tap **Back to menu** on the death panel — the preloaded interstitial plays before returning to `MainMenu`.
   - Interstitials also play at the end of a completed song when tapping **Retry** or **Back Home**.

### Hidden Developer Overlay
In Unity Editor and Development builds, press **`F1`** (desktop) or **four-finger tap** (phone) to toggle:
- **Server panel:** live connection state, editable server URL override, and manual life/revive test buttons.
- **mGAds diagnostics panel:** SDK/Banner status and manual **Show/Hide banner**, **Load/Show interstitial**, and **Test rewarded ad (no revive)** buttons.
