<img width="300" height="200" alt="slice1@2x" src="https://github.com/user-attachments/assets/7c8719b6-1ac9-4218-a2e5-f87800382c1a" />

**Bitz** is a Unity-based rhythm game inspired by *Taiko no Tatsujin*, *Audition*, and *Osu!Mania*. Featuring a unique **right-to-left arrow-based** gameplay, Bitz challenges players with diverse input patterns and fast-paced rhythm action.

<img width="939" height="532" alt="image" src="https://github.com/user-attachments/assets/16ada7c0-91fb-46ce-a91f-bd8b0ba761da" />

## 🕹️ Gameplay Overview

In **Bitz**, notes flow from the **right side** of the screen to the **left**, breaking away from conventional top-down or vertical scrolling found in most rhythm games.

### 🎯 Input Types

Bitz incorporates a variety of input types to keep gameplay dynamic:

- **Single Inputs** — Tap a key at the right moment
- **Combined Inputs** — Press multiple keys simultaneously (e.g., Left + Down)
- **Spamming Inputs** — Rapid key tapping in a short window
- **Directional Sequences** — Like in *Audition*, arrows must be input in rhythm
- **Advanced Hybrids** — Layered patterns and combos for high-skill maps

## ✨ Features

- ⬅️ Right-to-left rhythm charting
- 🎮 Keyboard and controller support
- 🔄 Modular input system
- 🎼 Support for custom beatmaps
- 💥 Combo scoring and timing precision
- 🎨 Built in Unity with cross-platform support


## Game server data contract

The [game data contract](Contracts/README.md) defines the DTOs for a future game server and explains how to check them against the Unity models.

## Local demo server connection

The Editor and development builds can connect to the local **simulated** Bitz server at `http://127.0.0.1:5030`. In the Bitz-Server repository, start it with `ASPNETCORE_ENVIRONMENT=Development` and `dotnet run --no-launch-profile`. Both processes must run on the same computer; a phone or emulator has its own localhost.

For a manual test, add an empty GameObject to a test scene and add `BitzLocalServerPanel`; Unity also adds `BitzLocalServerClient`. Enter a demo player name and use the panel to connect, refresh lives, simulate song starts, prepare a revive, and grant a simulated revive. The panel does not change the real gameplay flow. The demo uses a name-based local identity, and its simulated revive does not show or verify an ad.

Other scripts can add or reference `BitzLocalServerClient` and call `SignIn`, `GetState`, `StartSong`, `PrepareSimulatedRevive`, and `GrantSimulatedRevive`. Supply UUID request IDs and reuse the same ID when retrying an interrupted start or preparation. `GrantSimulatedRevive` takes the attempt ID returned by preparation. Results arrive asynchronously through callbacks and include a `success` flag and `code`. The project permits HTTP in development builds for this local test; release builds cannot use the demo client.

This temporary JSON/HTTP bridge uses the demo endpoints; it is not the authenticated MagicOnion service or a real mGAds integration. The existing game's start, retry and revive UI are not yet connected to it.

## Gameplay HP

The gameplay controller adds `BitzHealth` automatically. It starts each song with 5 HP, removes 1 HP for each missed bitz, and pauses the song at zero. The HP bar and death screen are created at runtime. A server-confirmed revive restores HP, but the revive button remains disabled until an `IBitzReviveProvider` is connected to the authenticated server and rewarded-ad flow. The localhost demo's simulated reward is not used as ad verification.

## Sandbox ad diagnostics

To check ads in the Editor or a development build, add `BitzAdsDiagnostics` to a GameObject in a test scene. Allow sandbox ads, then use **Load interstitial** and **Show loaded interstitial**, or use **Test rewarded ad (no revive)**. The panel reports initialization and load failures without displaying the SDK key. A real sandbox test requires a valid SDK key and matching ad unit in `Assets/Resources/MGAdsConfig.json` and a reachable configured gateway. Setting `editorMock` to true uses the SDK's offline Editor simulation; it does not request an ad from mGAds. The rewarded test does not attach a real player attempt or grant a revive. A rewarded revive additionally needs the authenticated server reward flow.
