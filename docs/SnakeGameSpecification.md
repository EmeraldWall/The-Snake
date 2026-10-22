# The Snake for Android 17

Software Requirements and Design Specification
Version 2.0, October 2026

## 0. What changed in 2.0

Version 2.0 replaces the "exact 1990s replica, no ads, no internet" direction of versions 1.0 to 1.2.

* **Tooling rule.** The only rule is Visual Studio. NuGet packages are allowed. Microsoft published .NET for Android bindings of Google's own libraries are preferred over community plugins.
* **Revenue.** One AdMob banner at the bottom of the screen. A small one time purchase removes it for good, and the purchase is kept through every update, reinstall and new phone.
* **Progressive game.** A modern Snake built for today's bigger phones, with smooth motion, power ups, combos and progression. The 1990s look and rules live on as the **Retro 97** mode and theme for the nostalgia crowd.
* **Online play through Google Play only.** Google Play Games Services for sign in, leaderboards, achievements and cloud saves. Google ended its own real time multiplayer service in 2020 [1], so live battles use Google Play services' **Nearby Connections** for players in the same place, and global battles are **seeded leaderboard battles** where everyone plays the same board.

Reasons for each choice are in `MarketStrategy.md` (version 2) and `UIUXAudit.md` section 10.

## 1. Purpose

This document describes a modern, highly replayable Snake game for Android 17, made in Visual Studio with .NET MAUI. It keeps the simple joy of the Snake many people played on their first mobile phone and adds what Android players expect today. It is the single source of truth for the people building, testing and reviewing the app.

## 2. Scope

### 2.1 In scope for 2.0

* Modern single player Snake with smooth movement, speed that rises during a run, power ups, combos and near miss bonuses
* Retro 97 mode with the classic rules, nine fixed speed levels, critters, mazes and the two colour LCD look
* Daily Run and Weekly Arena, global seeded battles ranked on Google Play Games leaderboards
* Nearby Battle, real time play for 2 to 4 players in the same room, with no internet needed
* Challenge codes to race a friend on the same board
* Player level, XP, daily missions, streaks, unlockable themes, skins and arenas (earned only, never sold)
* Google Play Games Services v2 sign in, leaderboards, achievements and cloud saved progress
* One bottom banner ad (AdMob) with a Google certified consent message where the law needs one
* One time "Remove ads" purchase through Google Play Billing that is restored automatically
* Touch, keyboard and game controller controls, haptics and sound
* Works on every Android phone that meets the minimum version (section 3), from small budget phones to foldables and tablets

### 2.2 Out of scope for 2.0

* Real time battles over the internet with strangers. This needs a game server (Google pointed developers to Firebase or Open Match when it closed its multiplayer service [1]), which goes beyond "Google Play only". It is listed in section 14 as a later option
* Video, interstitial, rewarded or any other ad format besides the single banner
* Any other purchase. Themes, skins and arenas are earned by playing
* Loot boxes, energy or lives systems, and pay to win items
* iOS, Windows and macOS builds

## 3. Technology

### 3.1 Tools and platform

| Item | Requirement |
|---|---|
| IDE | Visual Studio 2026 on Windows with the ".NET Multi platform App UI development" workload |
| Framework | .NET 11 with the .NET MAUI workload (RC2 now, stable when it ships) [2] |
| Target framework | `net11.0-android`, which defaults to API 37 [3] |
| Android compile and target SDK | API level 37 (Android 17) [4] |
| Minimum Android version | The highest minimum required by MAUI and the packages below. Check it at build time and record it in the store listing |
| Rendering | `GraphicsView` and an `IDrawable` from Microsoft.Maui.Graphics. Frame timing from Android `Choreographer` in `Platforms/Android` |
| Language | C# for logic, XAML for menus |

### 3.2 Packages

All four Google library bindings below are published on NuGet with Microsoft as the author. Versions are the latest at the time of writing. Always update to the newest stable version before release.

| Package | Version seen | Wraps | Used for |
|---|---|---|---|
| `Xamarin.GooglePlayServices.Ads.Lite` | 124.0.0.9 | `play-services-ads-lite`. Also pulls in `Xamarin.Google.UserMessagingPlatform` | Banner ads and the consent message |
| `Xamarin.Android.Google.BillingClient` | 9.1.0.2 | Google Play Billing Library | Remove ads purchase |
| `Xamarin.GooglePlayServices.Games.V2` | 122.1.0 | `play-services-games-v2` | Sign in, leaderboards, achievements, saved games |
| `Xamarin.GooglePlayServices.Nearby` | 119.5.0 | `play-services-nearby` | Nearby Battle |

**Why not community plugins.** `Plugin.InAppBilling` is no longer maintained by its author [5]. `Plugin.AdMob` is an MIT licensed community package that wraps the same Microsoft Ads binding [6]. It may be used if it saves time, but the app talks to it only through `IAdService`, so it can be swapped out.

**Billing version rule.** Google's deprecation table set 31 August 2026 (extension to 1 November 2026) as the last date for new apps and updates on Billing Library 7. Billing 8 is accepted until 31 August 2027 [7]. The 9.x binding meets this.

**Known binding risk.** One vendor says the Games V2 binding lacks some leaderboard methods [8]. Week 1 of development is a 2 day technical spike that signs in, submits a score, shows a leaderboard, unlocks an achievement and writes a saved game. If any call is missing, add a small Java bridge class through an Android binding library project in the same Visual Studio solution.

### 3.3 Android 17 rules

* Apps that target API 37 cannot opt out of orientation and resizability rules on large screens (600 dp smallest width or more) [9]. The game never locks orientation and redraws at any window size.
* Edge to edge layout with safe area insets so nothing hides behind the status bar, navigation bar or camera cutout.

## 4. Core gameplay (Modern rules)

### 4.1 Arenas and screen sizes

The game must feel made for big modern phones but stay fair and playable on small ones.

| Arena | Size (portrait) | Used in |
|---|---|---|
| Standard | 24 columns by 36 rows | Daily Run, Weekly Arena, Nearby Battle, Challenge codes, all leaderboards |
| Fit to screen | Columns = available width / 16 dp, rows = available height / 16 dp, at least 18 by 26 | Endless practice and unlocked Endless arenas |

* Landscape swaps columns and rows.
* Cell size is worked out from the space left after the HUD, the controls and the ad strip. The smallest allowed cell is 10 dp. On a 360 by 640 dp phone, after the system bars, HUD, ad strip and gap (about 194 dp in total), the Standard arena fits at about 12.4 dp cells, which is above the limit.
* Ranked modes always use the Standard arena so a big tablet gives no advantage on leaderboards.

### 4.2 Movement and feel

* The snake moves one cell per logic tick, but it is **drawn smoothly** between cells at the display's refresh rate (60, 90 or 120 Hz). Each frame the head and tail are drawn at the fraction of the tick that has passed.
* Turns use a queue of up to 2 pending turns. A 180 degree reverse is ignored.
* **Speed ramps up.** Start tick 220 ms. Each food eaten takes 3 ms off, down to a floor of 80 ms. Slow Time power ups change this for a while (4.4).
* **Wraparound edges** in Endless by default. Each arena can add walls and obstacles.
* A new run starts with 4 segments in the centre. Retry from Game over takes one tap and under 1 second.

### 4.3 Food, combos and near misses

* One food on the board at a time. Eating gives 10 points times the combo multiplier.
* **Combo.** Eating within 3 seconds of the last food raises the multiplier by 1, up to x5. Missing the window resets it to x1. The HUD shows the multiplier and a ring that empties as the window runs out.
* **Near miss.** When the head passes right next to the body, a wall or another snake at a tick length of 140 ms or less, show "Close!", give 5 points and a 10 ms haptic tick. At most one near miss per 2 seconds.
* **Golden food** appears once every 15 foods for 6 seconds and gives 50 points times the multiplier.

### 4.4 Power ups

A power up appears every 12 foods, stays for 10 seconds, and is drawn with both a unique colour and a unique shape so it can be read without colour.

| Power up | Shape | Effect | Lasts |
|---|---|---|---|
| Slow Time | Hourglass | Tick length x 1.6 | 6 s |
| Phase | Ghost | Pass through your own body | 5 s |
| Magnet | Horseshoe | Food within 3 cells moves to the head | 8 s |
| Double | "x2" | Points doubled on top of the combo | 10 s |
| Trim | Scissors | Tail shortened by 30 percent, at least 4 segments kept | Instant |

Only one timed power up is active at a time. A new one replaces the old one. A bar under the HUD shows the time left.

### 4.5 Death and Game over

* Death on hitting a wall, an obstacle, your own body (unless Phase is active) or another snake.
* Death effect lasts 600 ms. It is a short shake and a fade, not a flash. With "Remove animations" on, the snake simply fades out.
* Game over shows the score, best score, XP earned, mission progress, the challenge code, and buttons for Play again (selected by default), Share and Main menu. A "Remove ads" text button is shown only if the player has no ad free purchase, and at most once per session.

### 4.6 Scoring cap

Scores are 32 bit integers. The HUD shows up to 7 digits.

## 5. Game modes

| Mode | Rules | Leaderboard |
|---|---|---|
| Endless | Modern rules, Fit to screen arena, power ups | Endless all time |
| Daily Run | Standard arena, Modern rules, board and power up order from the daily seed | Google Play daily leaderboard |
| Weekly Arena | Standard arena with a weekly obstacle layout, from the weekly seed. Unlimited attempts, best score counts | Google Play weekly leaderboard |
| Nearby Battle | 2 to 4 players, real time, Standard arena | None (local results and achievements) |
| Challenge | Play the board in a friend's 8 character code | None |
| Retro 97 | Classic rules from version 1.2 (fixed level 1 to 9 speeds, critters, 5 mazes, walls on or off, two colour LCD look, beeps) | Retro 97 all time |

### 5.1 Daily Run and Weekly Arena (global seeded battles)

* Google Play Games leaderboards support daily, weekly and all time time spans [10]. Each ranked mode has one leaderboard and the game shows the time span that matches the mode.
* Seeds come from the **UTC** date (Daily) or the UTC year and ISO week number (Weekly), plus a fixed salt. Every player in the world gets the same board. Check during the spike that Google Play's daily and weekly reset times line up with UTC, and if they do not, use Google's reset time.
* Daily Run allows one ranked attempt per day. Later runs that day are practice.
* The results screen shows the player's rank and the scores just above and below, so there is always a "next person to beat".
* Before submitting, the game checks the score is possible for the run length (no more than 500 points per tick played, the most that golden food with a x5 combo and Double can give). Impossible scores are not sent. Also set any score limits the Play Console offers for each leaderboard.

### 5.2 Nearby Battle (real time, same room)

* Uses Google Play services Nearby Connections, which finds and connects to nearby devices over Bluetooth and Wi Fi with no internet needed, and which Google lists for local multiplayer games [11].
* One player hosts and the others join. Star topology with the host in the middle.
* **Host authoritative.** The host runs the only `GameEngine`. Clients send turn inputs. The host sends the full game state each tick (a few hundred bytes).
* Tick length is fixed at 150 ms in battles so it stays fair on slower connections.
* Rules. Each snake has its own colour and its own body pattern (stripes, dots, chevrons, solid). Running into any body kills you. Head on crashes kill both snakes. Last snake alive wins, or the longest snake after 2 minutes.
* If a client drops, its snake turns grey and stops. If the host drops, everyone sees "Host left" and the match ends with no result.
* Runtime permissions for Bluetooth, Wi Fi and nearby devices are asked only when the player opens Nearby Battle, after a plain screen that says why. Use the permission list from the Nearby Connections documentation for the target API level.

### 5.3 Challenge codes

Same as version 1.2. 8 characters in Crockford Base32, holding the seed, mode, arena and a check value. Shared with the MAUI Share API [12]. Example text is "Beat my 12,480 on The Snake. Code K7Q2M9XA".

### 5.4 Retro 97

Keeps the classic rules from version 1.2 of this spec (git commit `56cf65e`, sections 4.1 to 4.9 and 7.1 to 7.3). In short

| Rule | Retro 97 |
|---|---|
| Grid | 20 by 28 in portrait, 28 by 20 in landscape |
| Speed | Level 1 to 9, fixed for the whole game, 400 ms down to 95 ms per tick |
| Food | Level x 1 points. A critter every 5 foods for 20 ticks |
| Modes | Walls off (wraparound), walls on, 5 mazes |
| Look and sound | Two colour LCD themes, pixel font, optional ghosting, ToneGenerator beeps |
| Input | 2 turn queue, or 1 turn in the Purist option |

It is the nostalgia mode. Its two colour LCD themes are also available as themes in other modes. The app never uses the Nokia name, logo or a copy of a real phone design.

## 6. Progression and retention

The aim is "one more run" fun that players come back to every day, without dark patterns.

| System | Rules |
|---|---|
| XP and player level | XP = score / 10, plus mission rewards. Levels 1 to 50, each level needs 10 percent more XP than the last |
| Unlocks | Every level unlocks something. 12 themes, 16 snake skins, 6 Endless arenas, 8 trail effects. All earned, none sold |
| Daily missions | 3 per day, reset at UTC midnight, such as "Reach combo x4", "Use 3 power ups", "Eat 40 food". Each gives 50 to 150 XP. One free reroll per day |
| Streak | Days in a row with a Daily Run. One streak freeze is earned every 7 days, up to 2 stored |
| Achievements | 25 Google Play Games achievements, including all 12 medals from version 1.2 plus battle and arena ones |
| Personal bests | "New best" banner and the score needed for the next rank on Google Play |

**Healthy play rules**
* No energy, no wait timers, no loot boxes, nothing that is bought for power.
* Missed streaks are never shown with guilt messages.
* Optional "Take a break" reminder after a chosen number of minutes, off by default, in Settings.

## 7. Screens and flow

| Screen | Contents |
|---|---|
| Launch | System splash only |
| First run consent | Google UMP consent form, only where it is needed (section 9.3). Then straight to the first run, with no menus in the way |
| Home | Big Play button (Endless), Daily Run with countdown to reset, Weekly Arena, Nearby Battle, Challenge, Retro 97. Player level bar, today's missions, streak |
| Game | HUD at the top (score, combo ring, power up bar, pause), arena, controls, ad strip at the bottom |
| Pause | Continue, Restart, Settings, Home, Remove ads (if not owned) |
| Game over | Section 4.5 |
| Leaderboards | Google Play Games leaderboard screens for each ranked mode |
| Collection | Themes, skins, arenas and trails, locked items show the level that unlocks them |
| Nearby lobby | Host or Join, list of found players, Ready buttons |
| Settings | Sound, vibration, controls, keypad side, theme, Remove animations override, Take a break, Privacy options (consent), Restore purchases, Remove ads, Google Play Games account, About |
| About | Version, licences, privacy policy link |

## 8. Controls

| Control | Details |
|---|---|
| Swipe (default) | Same rules as 1.2 section 6.1. Swipes start only inside the arena and at least 24 dp from the side edges |
| Thumb turns | Tap the left half of the screen to turn left, the right half to turn right (relative to the snake). Good for big phones held in one hand |
| D pad | A 4 way pad, keys at least 56 dp. Shown only when the arena keeps cells of 10 dp or more, else the Settings screen says "Use landscape or a bigger screen for the D pad" |
| Keyboard and controllers | As 1.2 section 6.3 |
| Back gesture | Pauses the game. Second back goes to Home |

## 9. Ads, purchase and privacy

### 9.1 The banner

* **One anchored adaptive banner** at the very bottom of every screen, above the system navigation bar, through `IAdService`.
* The ad strip height is **reserved from the start**, using the anchored adaptive banner height the Ads SDK reports for the current screen width, so the layout never jumps when an ad loads.
* **Keep clear of game controls.** AdMob says banners close to app elements and beside game play screens where users keep tapping are a main cause of accidental clicks and can put the account at risk [13]. So
  * a gap of at least 24 dp plus a 1 dp divider separates the ad strip from the arena and any on screen control
  * touches in the gap and in the bottom 24 dp of the arena area are ignored by the game, and swipes cannot start there
  * the D pad, when used, sits at least 48 dp above the ad strip, or beside the arena in landscape
* If no ad fills, the strip shows a quiet house message ("The Snake") that **does nothing when tapped during play**. In menus the house message is a "Remove ads" button.
* No ads are requested until consent is settled (9.3).
* If the AdMob account ever shows invalid click warnings, the fallback is to hide the banner while a run is active and show it only on Home, Pause and Game over.

### 9.2 Remove ads purchase

| Item | Rule |
|---|---|
| Product | One time product `remove_ads` in Play Console. Not consumed |
| Price | The smallest price the developer is happy with, set per country in Play Console |
| Entry points | Settings, Pause, Home ad strip house message, and once per session on Game over |
| After buying | Banner and ad strip disappear at once and the layout reflows. Short "Thanks for supporting The Snake" message |
| Acknowledge | Acknowledge every purchase as soon as it reaches PURCHASED. Unacknowledged purchases are refunded after 3 days. Pending purchases are not acknowledged until they complete [14] |
| Restore | On every launch and every return to the foreground, call `QueryPurchasesAsync` for one time products, as Google's sample does to restore owned items after a reinstall [14]. A Restore purchases button in Settings does the same on demand |
| Kept through updates | Google Play stores the purchase on the player's Google account, so it comes back after updates, reinstalls and on new phones. The app also caches `store.adFree = true` in `Preferences`, which survives app updates. The entitlement is never tied to the app version |
| Offline | If the Play query fails, trust the cached value. Only a successful query that does not list the product turns ads back on (for example after a refund) |
| Startup cost | If the cached value is ad free, the Ads SDK is not initialised at all |
| Testing | License tester purchases are refunded after 5 minutes if not acknowledged, so test the acknowledge path with testers [14] |

### 9.3 Consent and privacy

* For users in the EEA, the UK and Switzerland, Google requires a Google certified consent platform for ads, and offers its User Messaging Platform (UMP) SDK for apps [15]. Use UMP (already pulled in by the Ads binding).
* Show the consent form on first launch where needed, before any ad request. Add **Privacy options** in Settings so players can change their choice, which AdMob says the publisher must provide [15].
* The Ads SDK uses the advertising ID. Apps targeting Android 13 or later must complete the advertising ID declaration in Play Console, and the `com.google.android.gms.permission.AD_ID` permission must be in the merged manifest [16]. Check the merged manifest in every Release build.
* Fill in the Play Console Data safety form honestly for the Ads SDK, Play Games Services and Billing, using Google's own data disclosure guides for those SDKs. Publish a privacy policy.
* Do not target children in the store listing.
* Permissions. Internet and network state (ads, Play Games), AD_ID, vibration, and the Nearby permissions asked only when Nearby Battle is opened.

## 10. Google Play Games Services

| Feature | Use |
|---|---|
| Sign in | Play Games Services v2 signs players in automatically at launch. If sign in fails or the player has no Play Games profile, the whole game still works offline. A "Sign in to Google Play Games" button in Settings retries |
| Leaderboards | Endless (all time), Daily Run (daily), Weekly Arena (weekly), Retro 97 (all time) |
| Achievements | 25, listed in `Resources/Raw/achievements.json` with their Play Console IDs |
| Saved games | One snapshot named `progress` with XP, level, unlocks, missions, streak and settings. Merge on conflict by keeping the higher XP and level, the union of unlocks and achievements, and the newer settings |
| Offline queue | Scores and achievement unlocks made offline are stored and sent when the device is back online and signed in |

## 11. Look and feel

* **Default theme "Neon LCD".** Dark background, soft grid, glowing green snake with rounded segments, gentle particle burst on eating (at most 24 particles), smooth turns. Modern, but it still says "Snake".
* **Retro 97 themes** from version 1.2 (Classic LCD, Blue backlight, Night) for nostalgia.
* 10 more unlockable themes, all checked for 4.5 to 1 text contrast and 3 to 1 contrast for the snake, food and obstacles.
* Fonts. A clean modern font for menus and the pixel font for Retro 97 and the score digits. Both under the SIL Open Font License.
* Particles, shake and glow follow the Android "Remove animations" setting and an in game override.
* Menus follow the system font scale up to 200 percent. Touch targets at least 48 dp.
* Classic handset frame on large screens is available only in Retro 97.

## 12. Architecture

### 12.1 Solution layout

```
TheSnake.sln
  TheSnake/                       .NET MAUI app (net11.0-android)
    Core/                         Pure C#, no MAUI or Android references
      GameEngine.cs               Modern and Retro rules, multi snake aware
      Rules/ModernRules.cs, Rules/RetroRules.cs, Rules/BattleRules.cs
      PowerUps.cs, Combo.cs, NearMiss.cs
      Arena.cs, ArenaLayouts.cs
      IRandomSource.cs, SeedRandomSource.cs, DailySeed.cs, WeeklySeed.cs
      ChallengeCode.cs
      Progression/Xp.cs, Missions.cs, Streak.cs, Unlocks.cs
      Battle/BattleState.cs, Battle/WireFormat.cs
    Rendering/                    GameDrawable, themes, particles, interpolation
    Services/
      IAdService.cs, IStoreService.cs, IConsentService.cs
      IGamesService.cs, INearbyService.cs
      ISoundService.cs, IHapticsService.cs
      SettingsService.cs, ProgressService.cs
    ViewModels/, Views/
    Platforms/Android/
      MainActivity.cs             Keys, controller, Choreographer
      AdMobAdService.cs           Banner view hosted in a MAUI handler
      PlayBillingStoreService.cs
      UmpConsentService.cs
      PlayGamesService.cs
      NearbyConnectionsService.cs
      SoundService.cs
  TheSnake.PlayGamesBridge/       Only if the spike finds missing binding calls
  TheSnake.Tests/                 MSTest unit tests for Core
```

### 12.2 Design rules

* **Core stays pure.** All rules, scoring, seeds, progression and battle state are plain C# and unit tested. The same engine runs single player and is the host's engine in battles.
* **Every Google SDK sits behind an interface** in `Services`. Each has a fake for tests and for devices without Google Play services, where the game still runs without ads, purchases or Play Games.
* **Logic tick and draw frame are separate.** An `IDispatcherTimer` drives logic ticks. `Choreographer` frame callbacks call `GraphicsView.Invalidate()` only while a run is active.
* **No allocation in the draw loop.** Particle and segment buffers are reused.
* **The banner is a native AdView** hosted by a MAUI handler in the bottom row of a shared page layout.

### 12.3 Persistence

| Data | Where |
|---|---|
| Settings, progress, unlocks, missions, streak | `Preferences` (JSON), synced to the Play Games saved game, and covered by Android Auto Backup as in 1.2 |
| Ad free entitlement cache | `Preferences` key `store.adFree`. Google Play is the source of truth |
| Paused run | `Preferences` under its own shared name, excluded from backup |
| Offline score queue | `Preferences` JSON list |

## 13. Quality targets

| Target | Value |
|---|---|
| Frame rate during play | Matches the display (60, 90 or 120 Hz) on mid range phones, at least 60 fps on low end phones with 3 GB RAM |
| Cold start to Home | Under 2.5 seconds on a mid range phone |
| Tap Play to first move | Under 1 second |
| Input to turn | Next tick at the latest |
| Memory | Under 200 MB |
| AAB size | Under 60 MB |
| Battle | 4 players for 2 minutes with no desync |
| Crash free sessions | 99.5 percent or better in Play Console vitals |

## 14. Testing and acceptance

### 14.1 Unit tests

All 1.2 tests still apply to Retro 97, plus speed ramp, combo window and cap, near miss limit, each power up, Standard arena fit maths, daily and weekly seeds across UTC midnight and year end, score plausibility check, saved game merge, battle collisions (body, head on, simultaneous), wire format round trip, and entitlement logic (cached, query success, query failure, refund).

### 14.2 Device tests

| Device | Checks |
|---|---|
| Small phone, 360 by 640 dp | Standard arena fits, cells at least 10 dp, banner gap kept |
| Large phone, 120 Hz | Smooth motion, thumb turns, fit to screen arena |
| Foldable and tablet | Resize, split screen, no locked orientation, landscape D pad |
| Phone without Google Play services | Game runs, no ads, no crashes |
| 2 to 4 phones | Nearby Battle full match, drop a client, drop the host |
| Airplane mode | Offline play, cached ad free state, queued scores sent later |
| EEA test device (UMP debug geography) | Consent form, Privacy options, no ad before consent |
| License tester account | Buy, acknowledge, uninstall, reinstall, check ads stay removed. Refund and check ads come back |

### 14.3 Acceptance criteria

1. All unit tests pass in Visual Studio Test Explorer.
2. All device tests pass.
3. Remove ads stays in effect after an app update, a reinstall and on a second phone with the same Google account.
4. No ad is requested before consent is settled, and Privacy options works.
5. The ad strip never overlaps or touches the arena or controls, and keeps the 24 dp gap.
6. The game is fully playable with no Google Play services and with no internet.
7. Every decision in `UIUXAudit.md` section 10 and `MarketStrategy.md` section 9 is implemented.

## 15. Build and release

1. Open `TheSnake.sln` in Visual Studio 2026.
2. Set up in Play Console the app, the `remove_ads` product, Play Games Services (leaderboards, achievements, saved games on) and the AdMob app and banner unit.
3. Use AdMob test ad units in Debug builds, and real units only in Release.
4. Publish through the Visual Studio Archive Manager as a signed AAB.
5. Complete the Data safety form, advertising ID declaration and content rating in Play Console.
6. Release to internal testing, then closed testing with license testers, then production.

## 16. Later options

* Online real time battles with strangers through a Google server product such as Firebase (needs a backend and changes the "Google Play only" rule)
* Ghost races that replay a friend's exact run from a shared file
* Seasonal Weekly Arena themes

## 17. References

[1] 9to5Google, "Google Play Games Services shutting down multiplayer API" (September 2019). https://9to5google.com/2019/09/16/google-play-games-services-shutting-down-multiplayer-api/

[2] Microsoft .NET Blog, ".NET 11 RC 2" (13 October 2026). https://devblogs.microsoft.com/dotnet/dotnet-11-rc-2/

[3] Start Debugging, "Migrate a .NET MAUI Android app to target Android API level 36". https://startdebugging.net/2026/09/migrate-a-dotnet-maui-android-app-to-target-android-api-level-36/

[4] Android Developers, "Set up the Android 17 SDK". https://developer.android.com/about/versions/17/setup-sdk

[5] GitHub, jamesmontemagno/InAppBillingPlugin README. https://github.com/jamesmontemagno/InAppBillingPlugin

[6] GitHub, marius-bughiu/Plugin.AdMob. https://github.com/marius-bughiu/Plugin.AdMob

[7] Android Developers, "Google Play Billing Library deprecation FAQ". https://developer.android.com/google/play/billing/deprecation-faq

[8] NuGet, ExzileGames.AndroidPlayGamesBridge (vendor claim about missing V2 binding calls). https://www.nuget.org/packages/ExzileGames.AndroidPlayGamesBridge

[9] Android Developers Blog, "Prepare your app for the resizability and orientation changes in Android 17". https://android-developers.googleblog.com/2026/02/prepare-your-app-for-resizability-and.html

[10] Android Developers, "Leaderboards" (Play Games Services). https://developer.android.com/games/pgs/leaderboards

[11] Google for Developers, "Nearby Connections". https://developers.google.com/nearby/connections

[12] Microsoft Learn, ".NET MAUI Share". https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/data/share

[13] AdMob Help, "Banner ad implementation guidance" (accidental clicks). https://support.google.com/admob/answer/6275345

[14] Android Developers, "Google Play Billing on PC with Unity" sample (acknowledge, restore with QueryPurchasesAsync, 3 day and 5 minute windows) https://developer.android.com/games/playgames/native-pc/unity/billing and RevenueCat, "Google Play edge cases" https://www.revenuecat.com/blog/engineering/google-play-edge-cases

[15] AdMob Help, "Google consent management requirements for serving ads in the EEA, the UK and Switzerland". https://support.google.com/admob/answer/13554116

[16] AdMob Help, "Advertising ID declaration". https://support.google.com/admob/answer/11402075

Package names and versions were read from the NuGet.org package feed on 22 October 2026.
