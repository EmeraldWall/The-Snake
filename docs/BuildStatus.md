# The Snake Build Status

Build 0.2.0 (version code 3), October 2026. Package `com.vishvplus.thesnakegame`.

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

## What changed in 0.2.0

* **Faster touch.** In 0.1.x the snake was drawn sliding into the cell it had just entered, so the screen was always one tick behind the game, and a turn waited for the next tick. Together that could add up to about 0.4 seconds at the start of a run. Now the renderer draws the snake moving into the cell it will enter next, so a turn shows on the very next frame. If the head is already 45 percent or more into its next cell when a turn arrives, that move is finished at once and the turn starts straight away. Swipes register after 14 dp instead of 24 dp, thumb taps react on touch down instead of release, and drawing runs on Android's Choreographer at the screen's own refresh rate (60, 90 or 120 Hz).
* **Modern popups.** Every system alert, action sheet, prompt and picker is replaced by in-app bottom sheets and chips that follow the theme. The Retro 97 picker is one sheet with board, speed and style chips.
* **More theme options.** 18 themes (13 smooth, including two light themes, and 5 Retro screens), 8 of them free from the start. The whole app now follows the chosen theme, including status bar icons. New free look options: 13 snake colours, 5 skins (Solid, Stripes, Gradient, Dots, Outline), 3 body shapes (Rounded, Square, Beads), 3 food styles (Orb, Apple, Gem), 3 board styles and a glow switch, all with live previews in Settings. All smooth themes were checked for at least 3 to 1 contrast for game objects and 4.5 to 1 for text.
* **Rendering checked offline.** The renderer was compiled into a desktop harness with Microsoft.Maui.Graphics.Skia and frames were rendered to images to check the forward slide, queued turns, wraparound, every look option and the Retro screens.

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
| 4.2 Smooth movement between cells | Done, drawn at the display refresh rate with Choreographer |
| 5 Endless, Daily Run, Weekly Arena, Challenge codes, Retro 97 (walls off, walls on, 5 mazes, levels 1 to 9, Purist option) | Done |
| 6 XP, 50 levels, 18 themes (8 free, the rest unlocked by level), free snake look options, 3 daily missions, streak with freezes, optional break reminder | Done |
| 8 Swipe, thumb turns, D pad, keyboard and game controller, back gesture pauses | Done |
| 9.1 One bottom banner, reserved height, 24 dp gap with divider, quiet house message | Done |
| 9.2 Remove ads one time purchase, acknowledge, restore on start and resume, offline cache | Done in code. Can only be tested once the app is in a Play Console test track with the `remove_ads` product |
| 9.3 UMP consent and Privacy options | Done |
| 10 Google Play Games sign in, leaderboards, achievements, saved games | Not yet. Needs the Play Console game project ID. Daily and Weekly scores are kept on the phone for now |
| 5.2 Nearby Battle | Not yet |
| Snake skins and looks | Done as free options. Extra Endless arenas and trails are not built yet |
| 14.1 Unit tests | 32 tests for the Core project, all passing |

## Changes from the spec layout

* The game rules live in a separate `TheSnake.Core` class library instead of a `Core` folder inside the app, so the MSTest project can test them without Android.
* A paused run is kept in memory, not saved to disk. If Android closes the app while a run is paused, that run is lost.
