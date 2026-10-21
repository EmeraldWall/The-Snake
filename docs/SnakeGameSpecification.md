# The Snake for Android 17

Software Requirements and Design Specification
Version 1.0, October 2026

## 1. Purpose

This document describes a Snake game for Android 17 that looks, sounds and plays like the classic Snake games on old monochrome Nokia phones. It is the single source of truth for the people building, testing and reviewing the app.

The app is built only with .NET MAUI inside Visual Studio. It uses no game engine, no third party NuGet packages and no other IDE.

## 2. Scope

### 2.1 In scope

* One player Snake with the classic Nokia rules (grow on eating, die on collision)
* Nine speed levels and a Classic mode plus a Maze mode
* Monochrome "LCD" look with chunky pixels and a pixel font
* Touch controls (swipe and an on screen keypad), hardware keyboard and game controller support
* Short beeps and vibration feedback
* Local high score table and settings that survive app restarts
* Pause and resume when the app goes to the background

### 2.2 Out of scope for version 1.0

* Online leaderboards, accounts or cloud sync
* Multiplayer of any kind
* Ads, in app purchases or analytics
* iOS, Windows and macOS builds (the code must stay portable, but only Android is shipped and tested)

## 3. Technology constraints

| Item | Requirement |
|---|---|
| IDE | Visual Studio 2026 on Windows with the ".NET Multi platform App UI development" workload installed |
| Framework | .NET 11 with the .NET MAUI workload |
| Target framework | `net11.0-android` |
| Android compile and target SDK | API level 37 (Android 17) |
| Minimum Android version | The default set by the Visual Studio MAUI template. Do not lower it |
| Language | C# for logic, XAML for menus and screens |
| Rendering | `GraphicsView` with an `IDrawable` from Microsoft.Maui.Graphics, which ships inside MAUI |
| Device features | Built in MAUI Essentials APIs only (Preferences, HapticFeedback, Vibration, DeviceDisplay) |
| Sound | Android `ToneGenerator` called from the `Platforms/Android` folder of the MAUI project |
| Extra packages | None. Only the packages the Visual Studio MAUI template adds by itself |
| Emulator and deployment | Visual Studio Android Device Manager and the Visual Studio Archive Manager |

### 3.1 Why .NET 11

A `net10.0-android` project defaults to API 36, while a `net11.0-android` project defaults to API 37, which is Android 17. .NET 11 RC2 shipped on 13 October 2026 with a go live license and works in Visual Studio 2026 Insiders. The stable release is expected about a month later. Development can start on RC2 and move to the stable SDK when it ships.

Fallback plan: if the team must stay on .NET 10 LTS, target `net10.0-android36.0`. The app will still install and run on Android 17 phones, it just will not opt into the Android 17 behaviour changes.

### 3.2 Android 17 rules that affect this game

* On large screens (smallest width of 600 dp or more), apps that target API 37 can no longer opt out of orientation and resizability restrictions. The game must therefore never lock orientation and must redraw correctly at any window size, including split screen, foldables and tablets. See section 7.
* The layout must respect system bars and display cutouts (safe areas) so the playfield and keypad are never hidden behind them.

## 4. Gameplay

### 4.1 Playfield

* The playfield is a fixed grid of **20 columns by 28 rows** in portrait and **28 columns by 20 rows** in landscape. A game keeps the grid it started with. Rotating during play pauses the game and redraws the same grid, letterboxed and centred.
* Each cell is a square. Cell size equals the largest whole number of device pixels that fits the available area, so the pixels stay crisp.
* A one cell thick border is drawn around the playfield, like the frame on the Nokia screen.

### 4.2 The snake

* A new game starts with a snake of **4 segments** in the middle of the grid, moving right.
* The snake moves exactly one cell per game tick.
* The head is drawn with a small "eye" pixel and an open mouth when food is in the cell directly ahead. Body segments are solid squares with a one pixel gap so the snake looks segmented, as on the original phones.
* When the snake eats, the eaten food is shown as a bulge travelling down the body until it reaches the tail, and the snake grows by one segment at that moment.

### 4.3 Food

* Exactly one piece of normal food is on the grid at any time. It appears on a random empty cell.
* Eating normal food adds **level x 1** points (level 1 gives 1 point, level 9 gives 9 points).
* After every **5** normal foods eaten, a **bonus critter** appears for **20 ticks** on a random empty 2 cell wide spot. A countdown of ticks left is shown next to the score.
* Eating a bonus critter gives **(ticks left x level)** points, rounded down to a whole number, with a minimum of level x 5. The snake does not grow from a critter.
* There are 6 critter shapes (bug, spider, fish, bird, mouse, beetle) drawn as small pixel sprites. The shape is picked at random.

### 4.4 Movement and input rules

* The player can turn left, right, up or down.
* A turn straight back into the snake's own neck (a 180 degree reverse) is ignored.
* Input is stored in a queue of at most **2 pending turns**. Each tick takes one turn from the queue. This lets fast players do quick double turns without losing a key press.
* Holding the key or finger in the current direction does nothing extra. There is no boost, matching the original.

### 4.5 Death

The game ends when the head moves into:

* the wall, in Classic mode with walls switched on and in every Maze layout
* a maze block
* any part of its own body (the tail cell that is moving away in the same tick does not count)

On death the snake blinks **3 times** (on and off every 250 ms), a long beep plays and the phone vibrates for 300 ms. The Game Over screen then shows the score and, if earned, a "New high score" message with name entry.

### 4.6 Winning

If the snake fills every free cell, the player wins. Show "Well done!" and award a bonus of 100 x level points.

### 4.7 Modes

| Mode | Rules |
|---|---|
| Classic, walls off | The snake passes through an edge and comes out on the opposite edge (wraparound). This is the default. |
| Classic, walls on | Touching an edge ends the game |
| Maze | Walls always on, plus one of 5 fixed maze layouts chosen in the menu (Box, Tunnel, Mill, Rails, Apartment) |

Maze layouts are stored as plain text files in `Resources/Raw/Mazes`, one character per cell, where `#` is a block and `.` is empty. The layout is scaled to fit the portrait or landscape grid by rotating it 90 degrees for landscape.

### 4.8 Levels and speed

The player picks a level from 1 to 9 before starting. Speed never changes during a game, as on the original phones.

| Level | Tick length (ms) |
|---|---|
| 1 | 400 |
| 2 | 340 |
| 3 | 290 |
| 4 | 245 |
| 5 | 205 |
| 6 | 170 |
| 7 | 140 |
| 8 | 115 |
| 9 | 95 |

### 4.9 Score display

A status bar above the playfield shows the score with 4 digits and leading zeros (for example `0042`) on the left, and the critter countdown on the right while a critter is on screen.

## 5. Screens and flow

### 5.1 Screens

| Screen | Contents |
|---|---|
| Splash | Pixel art snake logo for 1.5 seconds, tap to skip |
| Main menu | New game, Continue (only when a paused game exists), Level, Mode, Top scores, Settings, Instructions |
| Game | Status bar, playfield, on screen keypad (when enabled) |
| Pause overlay | "Paused" text, Continue, Restart, Main menu |
| Game over | Score, high score name entry if earned, Play again, Main menu |
| Top scores | Top 10 table per mode with name (up to 8 characters), score, level and date |
| Settings | Sound on or off, Vibration on or off, Controls (Swipe, Keypad, Both), Keypad side (left or right handed), Colour theme, Reset scores |
| Instructions | Short text and a picture of the controls |

### 5.2 Menu style

Menus look like the old phone menus. A single column list with the selected item shown in inverted colours (light text on a dark bar). The menu can be used by tapping, by the keypad 2 and 8 keys to move and 5 to select, or by a keyboard or controller.

### 5.3 State machine

The game logic has these states and only these transitions.

| From | Event | To |
|---|---|---|
| Ready | First direction input or Start | Running |
| Running | Pause button, back gesture, app goes to background, rotation | Paused |
| Paused | Continue | Countdown (3, 2, 1 shown for 1 second each) |
| Countdown | Countdown finished | Running |
| Running | Collision | Dying |
| Dying | Blink animation finished | GameOver |
| Running | Grid full | Won |
| GameOver or Won | Play again | Ready |
| Any | Main menu | Leaves the game page |

## 6. Controls

### 6.1 Swipe

* A swipe is recognised after the finger moves at least **24 dp**. The longer axis decides the direction.
* Swipes are read with a `PanGestureRecognizer` on the playfield so one long drag can produce several turns, one every time the finger travels 24 dp in a new direction.

### 6.2 On screen keypad

* A 3 by 3 Nokia style number pad below the playfield in portrait, or beside it in landscape.
* Keys 2, 4, 6 and 8 turn up, left, right and down. Key 5 pauses.
* Each key press gives a 10 ms haptic tick when vibration is on.
* Keys respond on press, not on release, to keep input fast.

### 6.3 Hardware keyboard and controllers

* Arrow keys, WASD and the number keys 2, 4, 6, 8 turn the snake. Space, Enter and P pause.
* A game controller D pad and left stick turn the snake. The A button selects and Start pauses.
* Key events are caught in `Platforms/Android/MainActivity.cs` by overriding `DispatchKeyEvent` and passing a simple `Direction` value to the shared game code through a small interface.

### 6.4 Back gesture

The Android back gesture pauses the game during play. A second back on the pause overlay returns to the main menu. Implement this with `Shell` back button handling or `OnBackButtonPressed` on the game page.

## 7. Look and feel

### 7.1 Colour themes

| Theme | Background | Pixels |
|---|---|---|
| Classic LCD (default) | `#C7D3A6` | `#2B3A1F` |
| Blue backlight | `#9FC6D9` | `#10283A` |
| Night | `#101410` | `#8FD18F` |

The whole app, menus included, uses only the 2 colours of the chosen theme, so it feels like a monochrome phone screen.

### 7.2 Pixel font

* Use a free pixel font licensed under the SIL Open Font License, added to `Resources/Fonts` and registered in `MauiProgram.cs` with `ConfigureFonts`.
* Check the font licence before shipping and include the licence text in the app's About section.

### 7.3 LCD ghosting (optional, Settings switch)

Old phone screens left a faint trail when pixels changed. When this option is on, the cell the tail just left is drawn at 30 percent opacity for one tick.

### 7.4 Adaptive layout

* No locked orientation. Do not set `ScreenOrientation` on `MainActivity`.
* The game page listens to `SizeChanged` and recalculates cell size, keypad placement and letterboxing.
* On screens 600 dp wide or more, the keypad is drawn at a fixed comfortable size rather than stretched.
* Use safe area aware layout so nothing sits under the status bar, navigation bar or camera cutout.

## 8. Sound and vibration

| Event | Sound (ToneGenerator) | Vibration |
|---|---|---|
| Eat food | Short beep, 40 ms | 10 ms click |
| Critter appears | Two short beeps | None |
| Eat critter | Three rising beeps | 20 ms |
| Death | Long low beep, 400 ms | 300 ms |
| Menu move | Very short tick, 15 ms | None |

* Sound and vibration each have their own setting.
* Sound is played through a small `ISoundService` interface. The Android implementation lives in `Platforms/Android/SoundService.cs` and wraps `Android.Media.ToneGenerator`. Other platforms get an empty implementation.
* Vibration uses MAUI `HapticFeedback` for clicks and `Vibration` for longer buzzes. Add the `VIBRATE` permission to `AndroidManifest.xml`.

## 9. Architecture

### 9.1 Solution layout

```
TheSnake.sln
  TheSnake/                      .NET MAUI app (net11.0-android)
    App.xaml, AppShell.xaml
    MauiProgram.cs
    Core/                        Pure C#, no MAUI references
      GameEngine.cs
      GameState.cs
      Snake.cs
      Grid.cs
      Direction.cs
      Food.cs
      Critter.cs
      MazeLoader.cs
      ScoreRules.cs
      IRandomSource.cs
    Rendering/
      GameDrawable.cs            IDrawable that paints the grid
      Sprites.cs                 Critter and font bitmaps as byte arrays
      Theme.cs
    Services/
      ISoundService.cs
      IHapticsService.cs
      SettingsService.cs         Wraps Preferences
      HighScoreService.cs        Wraps Preferences (JSON string)
    ViewModels/
      MenuViewModel.cs
      GameViewModel.cs
      SettingsViewModel.cs
      HighScoresViewModel.cs
    Views/
      SplashPage.xaml
      MenuPage.xaml
      GamePage.xaml
      SettingsPage.xaml
      HighScoresPage.xaml
      InstructionsPage.xaml
    Platforms/Android/
      MainActivity.cs            Key events
      SoundService.cs
      AndroidManifest.xml
    Resources/
      Fonts/, Images/, Raw/Mazes/, AppIcon/, Splash/
  TheSnake.Tests/                Unit tests for Core (Visual Studio MSTest template)
```

### 9.2 Key design rules

* **Core is pure logic.** `GameEngine` has a `Tick()` method and a `QueueTurn(Direction)` method. It knows nothing about time, drawing or sound. It raises plain C# events such as `FoodEaten`, `CritterEaten` and `Died`. This makes it fully unit testable.
* **Randomness is injected** through `IRandomSource` so tests can use a fixed seed.
* **Timing.** `GameViewModel` owns an `IDispatcherTimer` whose `Interval` is the tick length from section 4.8. Each timer tick calls `GameEngine.Tick()` and then `GraphicsView.Invalidate()`.
* **Drawing.** `GameDrawable.Draw` reads a read only snapshot of the game state and paints rectangles with `canvas.FillRectangle`. Antialiasing is turned off so pixels stay sharp. Nothing is allocated inside `Draw` during play.
* **MVVM without extra packages.** View models implement `INotifyPropertyChanged` by hand and expose `Command` objects from MAUI. Pages are registered in `MauiProgram.cs` with the built in dependency injection.
* **Navigation** uses MAUI `Shell` routes.

### 9.3 Persistence

| Data | Storage | Key |
|---|---|---|
| Settings | `Preferences` | `settings.sound`, `settings.vibration`, `settings.controls`, `settings.theme`, `settings.ghosting`, `settings.keypadSide` |
| Last level and mode | `Preferences` | `game.level`, `game.mode`, `game.maze` |
| Top 10 per mode | `Preferences`, JSON with `System.Text.Json` | `scores.classic`, `scores.walls`, `scores.maze` |
| Paused game | `Preferences`, JSON | `game.saved`, cleared when the game ends |

### 9.4 App lifecycle

* When the window is deactivated or the app is stopped (`Window.Deactivated` or `Window.Stopped`), a running game moves to Paused and is saved.
* When the app comes back, the pause overlay is shown. The game never restarts by itself.
* A configuration change such as rotation or a window resize must not lose the game. The state lives in the view model and in the saved JSON, never only in the page.

## 10. Performance and quality targets

| Target | Value |
|---|---|
| Cold start to main menu on a mid range Android 17 phone | Under 2 seconds in Release build |
| Tick timing drift | Under 10 ms on average over one minute at level 9 |
| Input delay from key press to turn | Next tick at the latest |
| Memory during play | Under 150 MB |
| Release APK or AAB size | Under 40 MB |
| Crashes | Zero in the test matrix below |

* Build Release with the default MAUI trimming and AOT settings from the template.
* Accessibility. Every button has a `SemanticProperties.Description`. Menus work with TalkBack. The Night theme gives a high contrast option.

## 11. Testing

### 11.1 Unit tests (TheSnake.Tests)

* Moving one tick in each direction
* 180 degree reverse is ignored
* Two queued turns in one tick are both applied over the next 2 ticks
* Wraparound at all 4 edges in Classic, walls off
* Death at the wall in Classic, walls on, and in Maze
* Death on self collision, and no death when the head moves into the cell the tail is leaving
* Growth by one segment after eating
* Food never spawns on the snake or a maze block
* Critter appears after every 5th food and leaves after 20 ticks
* Score formulas for food, critters and a full grid
* Maze files load and rotate correctly for landscape

### 11.2 Manual test matrix

Run on the Visual Studio Android emulator and on at least one real device.

| Device profile | Checks |
|---|---|
| Phone, Android 17, portrait | Full game at levels 1, 5 and 9, every mode |
| Phone, Android 17, landscape | Layout, keypad position, rotation pauses the game |
| Tablet or foldable, Android 17 | Resizing, split screen, no locked orientation, letterboxing |
| Phone with hardware keyboard or controller | All key bindings |
| Phone with TalkBack on | Menus readable and usable |
| Phone, Android 16 | Installs and runs (backward check) |

### 11.3 Acceptance criteria

The release is accepted when:

1. Every unit test passes in Visual Studio Test Explorer.
2. Every item in the manual test matrix passes.
3. A full game can be played with each control method.
4. High scores and settings survive closing and reopening the app.
5. Sending the app to the background and back never loses a game in progress.
6. No third party packages appear in the project file.

## 12. Build and release

1. Open `TheSnake.sln` in Visual Studio 2026.
2. Select the Android emulator or a connected device and press F5 to debug.
3. For release, switch to the Release configuration, right click the project and choose **Publish**. This opens the Archive Manager.
4. Create an Android App Bundle (AAB), sign it with the team's keystore through **Distribute**, and upload it to Google Play.
5. Bump `ApplicationDisplayVersion` and `ApplicationVersion` in the project file for every release.

## 13. Future ideas (not in 1.0)

* Two player mode on one tablet
* Daily challenge maze with a fixed seed
* Unlockable colour themes for high scores
* iOS and Windows builds from the same MAUI project

## 14. References

* Android Developers, "Set up the Android 17 SDK" (compileSdk and targetSdk 37). https://developer.android.com/about/versions/17/setup-sdk
* Android Developers Blog, "Prepare your app for the resizability and orientation changes in Android 17" (February 2026). https://android-developers.googleblog.com/2026/02/prepare-your-app-for-resizability-and.html
* Microsoft .NET Blog, ".NET 11 RC 2" (13 October 2026). https://devblogs.microsoft.com/dotnet/dotnet-11-rc-2/
* Start Debugging, "Migrate a .NET MAUI Android app to target Android API level 36" (default API levels for net10.0-android and net11.0-android). https://startdebugging.net/2026/09/migrate-a-dotnet-maui-android-app-to-target-android-api-level-36/

The gameplay numbers in this document (grid size, speeds, scores, critter timing) are design choices for this project. They are inspired by the Nokia games but are not claimed to be exact copies of them.
