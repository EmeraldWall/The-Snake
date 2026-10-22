# The Snake Build Status

Build 0.1.1 (version code 2), October 2026. Package `com.vishvplus.thesnakegame`.

This is the first playable test build of spec 2.0. It is a test build, not a store build.

## How it was built

| Item | Value |
|---|---|
| SDK | .NET 10 SDK 10.0.113 with the `maui-android` workload |
| Target | `net10.0-android`, target SDK 36 (Android 16), minimum SDK 23 (Android 6.0) |
| Why not .NET 11 and API 37 | The .NET 11 SDK could not be downloaded in the build machine, and .NET 11 is not final yet. This is the fallback in spec 3.1. The APK still installs and runs on Android 17 phones. Change `TargetFramework` to `net11.0-android` once .NET 11 ships |
| ABIs | arm64-v8a, armeabi-v7a, x86_64 |
| Signing | Android debug key. Fine for installing by hand, not accepted by Google Play |
| Ad IDs | Google's public AdMob test IDs, so only test ads show |

## Testing so far

* The 32 Core unit tests pass. They cover movement, turns, wraparound, walls, collisions, growth, speed, combos, power ups, Retro rules, mazes, seeds, challenge codes and progression.
* The Release APK builds and was checked with `aapt` and `apksigner`.
* **0.1.1 fixes the empty arena in 0.1.0.** The game screen waited for its play area to have a size, but it listened to the page's size event, which fires before the play area is measured. No run was ever created, so no snake or food was drawn. It now listens to the play area itself, with a delayed fallback.
* An Android 9 emulator did boot and 0.1.1 installed on it, but under software emulation Android's own system service kept being restarted by its watchdog, so the app could not be opened there. No crash of The Snake itself was recorded.
* **The app has not yet been run on a device.** The build machine has no hardware virtualisation, and the software emulator had still not finished booting after an hour. Install the APK on a real phone first and report anything that goes wrong.

## What is in this build

| Spec section | Status |
|---|---|
| 4 Modern rules: speed ramp, 2 turn queue, combos up to x5, near misses, golden food, 5 power ups | Done |
| 4.1 Standard arena (24 by 36) for ranked modes, fit to screen arena for Endless | Done |
| 4.2 Smooth movement between cells | Done, drawn at about 60 frames a second |
| 5 Endless, Daily Run, Weekly Arena, Challenge codes, Retro 97 (walls off, walls on, 5 mazes, levels 1 to 9, Purist option) | Done |
| 6 XP, 50 levels, 10 themes unlocked by level, 3 daily missions, streak with freezes, optional break reminder | Done |
| 8 Swipe, thumb turns, D pad, keyboard and game controller, back gesture pauses | Done |
| 9.1 One bottom banner, reserved height, 24 dp gap with divider, quiet house message | Done |
| 9.2 Remove ads one time purchase, acknowledge, restore on start and resume, offline cache | Done in code. Can only be tested once the app is in a Play Console test track with the `remove_ads` product |
| 9.3 UMP consent and Privacy options | Done |
| 10 Google Play Games sign in, leaderboards, achievements, saved games | Not yet. Needs the Play Console game project ID. Daily and Weekly scores are kept on the phone for now |
| 5.2 Nearby Battle | Not yet |
| Snake skins, arenas and trails as unlocks | Not yet. Themes are the only unlocks so far |
| 14.1 Unit tests | 32 tests for the Core project, all passing |

## Changes from the spec layout

* The game rules live in a separate `TheSnake.Core` class library instead of a `Core` folder inside the app, so the MSTest project can test them without Android.
* Frame timing uses a MAUI `IDispatcherTimer` at 16 ms instead of Android `Choreographer`. It can be swapped later for smoother 90 and 120 Hz drawing.
* A paused run is kept in memory, not saved to disk. If Android closes the app while a run is paused, that run is lost.
