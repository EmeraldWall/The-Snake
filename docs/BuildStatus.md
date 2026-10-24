# The Snake Build Status

Build 0.3.2 (version code 6), October 2026. Package `com.vishvplus.thesnakegame`.

This is the first playable test build of spec 2.0. It is a test build, not a store build.

## How it was built

| Item | Value |
|---|---|
| SDK | .NET 10 SDK 10.0.113 with the `maui-android` workload |
| Target | `net10.0-android`, target SDK 36 (Android 16), minimum SDK 24 (Android 7.0). The minimum went up from 23 in 0.3.0 because Google Play services basement 18.12, pulled in by Play Games and Nearby, needs 24 |
| Why not .NET 11 and API 37 | The .NET 11 SDK could not be downloaded in the build machine, and .NET 11 is not final yet. This is the fallback in spec 3.1. The APK still installs and runs on Android 17 phones. Change `TargetFramework` to `net11.0-android` once .NET 11 ships |
| ABIs | arm64-v8a, armeabi-v7a, x86_64 |
| Signing | Android debug key. Fine for installing by hand, not accepted by Google Play |
| Ad IDs | Google's public AdMob test IDs, so only test ads show |

## What changed in 0.3.1 and 0.3.2

* **Fixed: 0.3.0 crashed as soon as it opened.** Captured on an Android 9 emulator: `java.lang.UnsatisfiedLinkError: No implementation found for void ...MainApplication.n_onCreate()`. 0.3.0 overrode `MainApplication.OnCreate` to start Play Games, and in the Release build the native link for that override was not registered, so Android stopped the app before any game code ran. `OnCreate` is no longer overridden; the start up work now runs in `CreateMauiApp`, which `MauiApplication.OnCreate` already calls. 0.3.1 and 0.3.2 were installed on the same emulator and keep running with no crash.
* **Crash reports.** .NET and Java crashes are now written to a small file. On the next launch the app shows a sheet that offers to share the report, which holds only the error details.
* **Safer centring.** 0.3.0 centred the Home, Settings and Battle columns, the sheets and the Game over and Pause cards by giving them a fixed width at run time. On the emulator this left Home with only its title placed. They now fill the width again, as in 0.2.0, and are centred on tablets with side padding or margins instead.
* **Emulator limit.** The software emulator here never draws MAUI screens (0.2.0, which works on real phones, also stays blank there), so it can confirm crashes but not how screens look.

## What changed in 0.3.0

* **Google Play Games leaderboards.** Play Games Services v2 through Microsoft's `Xamarin.GooglePlayServices.Games.V2` 122.1.0 binding. In this version the leaderboard calls that one vendor reported missing are all present, so no Java bridge was needed. Four boards: Endless (all time), Daily Run (daily), Weekly Arena (weekly), Retro 97 (all time). Only plausible scores are sent, and only today's first Daily Run. Scores made while signed out wait on the phone and are sent after the next sign in. The game over card shows your rank when Play Games returns one, and has a Leaderboard button; Home has a Leaderboards button.
* **Works before Play Console is set up.** All IDs live in `Platforms/Android/Resources/values/games.xml`. While the project ID there is 0, the Play Games SDK is never started (its own start up provider is removed in the manifest and `MainApplication` starts it only with real IDs), so the app cannot crash for a missing ID and scores simply stay on the phone.
* **Nearby Battle.** 2 to 4 phones in the same room through Google Play services Nearby Connections (`Xamarin.GooglePlayServices.Nearby` 119.5.0, P2P_STAR strategy), with no internet. One phone hosts and runs the only battle engine; the others send turns and draw the state the host sends every 150 ms tick (73 bytes for 4 players). Rules from spec 5.2: any body kills, head on crashes kill both, last snake alive wins or the longest after 2 minutes. A player who leaves turns grey and stops. Each player has a colour and a body pattern (solid, stripes, dots, outline). Your own turns show at once on your phone while the host catches up. Lobby, countdown, results and rematch are all in app. Bluetooth, Wi Fi and nearby device permissions follow Google's Nearby Connections guide and are asked only when you host or join. Bluetooth, Wi Fi and location are marked as optional features so phones without them can still install the game.
* **Layout fixes from the 0.2.0 screenshot.** The Game over and Pause cards no longer stretch to the full height or clip their buttons on the right: the scroll container inside the card is gone and the cards are centred at a fixed width (up to 400 dp). The dim layer behind them is a soft 40 percent shade instead of solid black. The top bar is now score on the left, a centred combo pill with a thin bar, and the pause button on the right with equal 16 dp margins. Sheets and the Home and Settings columns are also centred by width, so they look right on tablets.
* **New unit tests.** 9 battle and score check tests (start positions, simultaneous moves, kills, head on draws, time up, players leaving, wire format round trips). 43 tests in total, all passing.

## What changed in 0.2.0

* **Faster touch.** In 0.1.x the snake was drawn sliding into the cell it had just entered, so the screen was always one tick behind the game, and a turn waited for the next tick. Together that could add up to about 0.4 seconds at the start of a run. Now the renderer draws the snake moving into the cell it will enter next, so a turn shows on the very next frame. If the head is already 45 percent or more into its next cell when a turn arrives, that move is finished at once and the turn starts straight away. Swipes register after 14 dp instead of 24 dp, thumb taps react on touch down instead of release, and drawing runs on Android's Choreographer at the screen's own refresh rate (60, 90 or 120 Hz).
* **Modern popups.** Every system alert, action sheet, prompt and picker is replaced by in-app bottom sheets and chips that follow the theme. The Retro 97 picker is one sheet with board, speed and style chips.
* **More theme options.** 18 themes (13 smooth, including two light themes, and 5 Retro screens), 8 of them free from the start. The whole app now follows the chosen theme, including status bar icons. New free look options: 13 snake colours, 5 skins (Solid, Stripes, Gradient, Dots, Outline), 3 body shapes (Rounded, Square, Beads), 3 food styles (Orb, Apple, Gem), 3 board styles and a glow switch, all with live previews in Settings. All smooth themes were checked for at least 3 to 1 contrast for game objects and 4.5 to 1 for text.
* **Rendering checked offline.** The renderer was compiled into a desktop harness with Microsoft.Maui.Graphics.Skia and frames were rendered to images to check the forward slide, queued turns, wraparound, every look option and the Retro screens.

## Testing so far

* The 43 Core unit tests pass. They cover movement, turns, wraparound, walls, collisions, growth, speed, combos, power ups, Retro rules, mazes, seeds, challenge codes and progression.
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
| 10 Google Play Games sign in and leaderboards | Done in code. Switches on when real IDs are put in games.xml. Achievements and saved games are not built yet |
| 5.2 Nearby Battle | Done. Needs two or more real phones to test |
| Snake skins and looks | Done as free options. Extra Endless arenas and trails are not built yet |
| 14.1 Unit tests | 43 tests for the Core project, all passing |

## Changes from the spec layout

* The game rules live in a separate `TheSnake.Core` class library instead of a `Core` folder inside the app, so the MSTest project can test them without Android.
* A paused run is kept in memory, not saved to disk. If Android closes the app while a run is paused, that run is lost.
