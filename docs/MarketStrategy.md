# The Snake Positioning and Blue Ocean Strategy

Version 2, October 2026. Replaces version 1, which assumed no ads, no purchases and no internet.

## 0. What changed

The developer needs income to keep the game alive, so the strategy now includes one banner ad and a small one time purchase to remove it. An exact 1990s replica would also waste the big screens of modern phones, so the core game is now a modern, progressive Snake. The 1990s experience lives on as the Retro 97 mode. Online competition comes only from Google Play services.

## 1. Summary

Big snake games on Google Play compete on online multiplayer with strangers, cosmetics and ad income. Snake Clash!, Snake.io and Snake Lite each show 100M+ downloads [1] [2] [3]. Copying them would mean running game servers and fighting for the same players, which is a red ocean.

The Snake goes another way. It combines four things that are rarely found together (an assumption to check, section 8)

1. **A modern Snake made for big phones** with smooth motion, power ups, combos and progression, plus a faithful **Retro 97** mode for people who grew up with Snake.
2. **Fair global battles without servers.** Every player gets the same board in the Daily Run and Weekly Arena and is ranked on Google Play Games leaderboards.
3. **Real time battles in the same room without internet,** through Google Play services Nearby Connections.
4. **Respectful money.** One banner, never a video or full screen ad, and a small one time payment removes it for good.

Positioning statement. For people who loved Snake on their first phone and for today's casual Android players, The Snake is the classic game rebuilt for modern phones. Unlike ad heavy snake battle games, it never interrupts play, ranks everyone fairly on the same board, and lets friends battle face to face with no internet. One small payment removes the only ad forever.

## 2. Frame of reference

| Group | Examples | What they offer |
|---|---|---|
| Online snake battle games | Snake Clash!, Snake.io, Snake Lite, all 100M+ on Google Play [1] [2] [3] | Online multiplayer, cosmetics, huge audiences |
| Classic snake clones | "Snake" by Infinità Game Studio (100K+), "Snake Game" by Puzzle Pixel Studio (1K+) [4] [5] | Simple grid Snake |
| Retro and nostalgia gaming | Retro handhelds, emulators, re releases | A market secondary reports put at a few billion US dollars a year, driven by millennials and Gen X [6] (original source unclear) |

The Snake is placed as **"the best Snake game on Android"**. It is compared with both the battle games (for modern players) and the classic clones (for nostalgic players).

## 3. Points of parity (the minimum Android players expect in 2026)

A point of parity is something the product must have just to be considered. It is not a reason to choose it [7] [8].

| POP | Evidence or reason | How The Snake meets it |
|---|---|---|
| Free to download | The leading snake games are free [1] [2] [3] | Free, with one banner |
| Ads that players accept | About a third of US and UK gamers find in game ads intrusive [9], while 71 percent of US mobile gamers report a positive ad experience [10]. Players accept ads when they are not in the way | One banner, outside the play area, never video or full screen |
| A way to pay to remove ads | Common in casual games (assumption to check in the store scan) | One time `remove_ads` purchase, restored automatically |
| Progress that survives a new phone | Review analysis calls cloud save "table stakes" and links lost saves to uninstalls [11] | Play Games saved games, plus Android Auto Backup [12] |
| Sign in, leaderboards and achievements | Standard features of Google Play Games Services, which kept leaderboards, achievements and cloud saves when the multiplayer APIs closed [13] | Play Games Services v2 |
| Something new every day | Daily content is a normal reason to come back (assumption) | Daily Run, daily missions, streaks |
| Progression and unlocks | Most current casual games have levels and unlockables (assumption) | XP, 50 levels, earned themes, skins and arenas |
| Smooth, modern visuals | Today's phones have 90 or 120 Hz screens | Smooth movement at the display refresh rate |
| Big screen and foldable support | Required for apps targeting API 37 on large screens [14] | Fit to screen arena, landscape layouts |
| Some kind of multiplayer or competition | The top snake games are all multiplayer [1] [2] [3] | Seeded global battles and Nearby Battle |
| Privacy and consent done right | Google requires a certified consent platform for ads in the EEA, UK and Switzerland [15]. Every app must fill in the Data safety form [16] | UMP consent, Privacy options, honest Data safety form |
| Controls that feel fair | Expected in any action game | Turn queue, near miss forgiveness, several control methods |

**Parity we choose not to match.** Real time online battles with strangers. Google's own multiplayer service closed on 31 March 2020 and Google pointed developers to Firebase or Open Match [13]. Both need servers and go beyond "Google Play only". The seeded battles and Nearby Battle cover the need for competition in a different way.

## 4. Points of difference

Each POD must be **desirable**, **deliverable** and **different**, with contradictions removed [8].

| POD | Desirable | Deliverable | Different |
|---|---|---|---|
| **1. Two Snakes in one.** Modern Snake for big phones plus a faithful Retro 97 mode | Snake shipped on Nokia phones for about ten years from the 6110 in 1997, on hundreds of millions of handsets [17] [18] [19]. Modern players want a modern game | Same engine with two rule sets | The battle games drop the classic feel, the clones do not modernise (assumption) |
| **2. Respectful money.** One banner, no interruptions, removed for good by one small payment | A third of gamers find ads intrusive [9] | Microsoft's AdMob and Billing bindings | Many free games use full screen ads (assumption to check) |
| **3. Fair global battles.** Same board for everyone, ranked daily and weekly on Google Play | Fair competition and a clear "next person to beat" | Seeded engine plus Play Games leaderboards with daily and weekly spans [20] | Battle games rank on luck of the lobby. Seeded boards are rare in snake games (assumption) |
| **4. Face to face battles with no internet.** 2 to 4 phones in the same room | Social play with friends and family | Nearby Connections works over Bluetooth and Wi Fi without internet and Google lists local multiplayer games as a use [21] | Nostalgia hook. Nokia's managers wanted to see what the 6110's infrared could do when Snake was made [17] |
| **5. Plays great on every Android phone.** From small budget phones to 120 Hz flagships and foldables | "Modern phones are bigger" but many players still use small phones | Standard arena fits a 360 by 640 dp phone at about 12 dp cells. Fit to screen arenas use big screens | Small clones often just stretch (assumption) |

**Contradiction checks**

* *Ads versus respectful.* Solved by one banner only, a reserved strip with a clear gap from the controls, and a cheap permanent opt out.
* *Online versus Google Play only.* Solved by using Google Play Games leaderboards for global battles and Google Play services Nearby Connections for real time play. No server of our own.
* *Addictive versus healthy.* Solved by "one more run" design (fast retry, combos, near misses, missions, ranks) without energy timers, loot boxes, guilt messages or pay to win. A break reminder is available.
* *Modern versus nostalgic.* Solved by keeping both. Modern is the default, Retro 97 is one tap away.

**Legal care.** Never use the Nokia name, logo or a copy of a real phone design in the app or listing. This is a project rule, not legal advice.

## 5. Blue Ocean strategy

Blue Ocean Strategy (Kim and Mauborgne) creates new market space by raising buyer value and cutting cost at the same time, using the strategy canvas and the Eliminate, Reduce, Raise, Create grid [22] [23].

### 5.1 Strategy canvas

Scores are the team's estimates for a typical game in each group (0 none, 10 very high). Check them with the store scan in section 8.

| Factor | Online snake battle games | Classic clones | **The Snake 2.0** |
|---|---|---|---|
| Online battles with strangers | 9 | 0 | 0 |
| Cosmetic shop and currencies | 9 | 1 | 0 |
| Ad interruptions (video, full screen) | 8 | 5 | 0 |
| Ad footprint overall | 8 | 5 | 2 |
| Pay to remove ads, one time | 3 | 3 | 10 |
| Fairness of competition | 4 | 2 | 9 |
| Real time play with friends nearby | 1 | 0 | 8 |
| Modern visuals and smooth motion | 8 | 3 | 8 |
| Faithful 90s Snake mode | 1 | 5 | 10 |
| Progression earned by play only | 3 | 1 | 9 |
| Daily and weekly challenges | 5 | 1 | 9 |
| Big screen, foldable and controller support | 4 | 2 | 9 |
| Works offline | 1 | 8 | 8 |

### 5.2 ERRC grid

| Eliminate | Reduce |
|---|---|
| Video, interstitial and rewarded ads | Ads to one bottom banner |
| Cosmetic shop, currencies and loot boxes | Purchases to one item |
| Energy, lives and wait timers | Menus to one Home screen |
| Pay to win items | Time from tap to play (under 1 second) |
| Own game servers | Download size (under 60 MB) |
| Forced account creation (Play Games signs in by itself) | Permission prompts (Nearby asks only when used) |

| Raise | Create |
|---|---|
| Fairness (same seeded board for everyone, plausibility checks) | **Seeded global battles.** Daily Run and Weekly Arena on Google Play leaderboards |
| Feel (smooth motion, turn queue, near miss forgiveness) | **Nearby Battle.** Real time 2 to 4 player matches with no internet |
| Use of big screens | **Retro 97.** The faithful 1990s mode and themes inside a modern game |
| Value of the ad removal (permanent, restored everywhere) | **Challenge codes** to race a friend on the same board |
| Accessibility and colour safe design | **"Next rival" display.** The score just above yours on each leaderboard |

### 5.3 Noncustomers

* **Soon to be noncustomers.** People playing snake battle games who are tired of interruptions. The one banner promise and fair boards win them over.
* **Refusing noncustomers.** People who uninstall ad heavy games. A cheap, permanent ad removal turns them into paying players.
* **Unexplored noncustomers.** Adults who remember Snake but do not see themselves as gamers. Retro 97 and simple controls make it safe to try. Families who want to play together in one room, through Nearby Battle. The listing must still not target children.

### 5.4 Business model

| Revenue | Rule |
|---|---|
| Banner ads | One anchored adaptive banner. Measure eCPM and fill rate in AdMob from week 1 |
| Remove ads | One time, small price set per country. Shown in Settings, Pause, the empty ad strip in menus, and once per session on Game over. Never as a pop up during play |
| Cost | No servers. Costs are the Google Play developer account, AdMob setup and the team's time |

Banner income depends on country, fill rate and how many players remove ads. No figure is given here because there is no data yet. Review the numbers after the first 30 and 90 days.

## 6. How the earlier theories still apply

| Theory | Version 2 use |
|---|---|
| TPB attitude | Fun modern feel, nostalgia mode, no interruptions |
| TPB subjective norm | Leaderboards, "next rival", Nearby Battle with friends, challenge codes |
| TPB perceived behavioural control | Instant retry, fair controls, works offline, works on any phone |
| IDT relative advantage | Respectful ads and permanent removal, fair seeded battles |
| IDT compatibility | Google Play Games sign in and achievements that Android players already know |
| IDT trialability | Free, no account to create, Play in under 1 second |
| IDT observability | Leaderboard ranks, shareable codes, playing together in one room |
| VRIO | No single feature is hard to copy. The advantage is the combination and the quality of execution, so the team must keep the scope tight and the feel excellent |

## 7. Risks

| Risk | Plan |
|---|---|
| Banner income is low | Track from week 1. Keep the cost base near zero. Revisit price of ad removal after 90 days |
| Accidental clicks flagged by AdMob | Reserved strip, 24 dp gap, ignored touch zone. Fallback is to show the banner only outside active play |
| Missing calls in the Games V2 binding [24] | 2 day spike in week 1. Java bridge if needed |
| Nearby permissions confuse players | Ask only when Nearby Battle opens, with a plain explanation first |
| Leaderboard cheating | Plausibility checks before submit and any Play Console score limits |
| Devices without Google Play services | Game still works fully offline without ads, purchases or Play Games |
| Billing library deadlines | Use the 9.x binding. Google's table accepts Billing 8 until 31 August 2027 [25] |

## 8. What to check before launch

1. **Store scan** of the top 30 results for "snake". Record ad formats, a remove ads option, leaderboards, offline play, controller support and big screen layout. Update the canvas.
2. **Closed test** with 20 to 30 players. Measure day 1 and day 7 return rates, runs per session, and how many buy Remove ads.
3. **Listing test** in Play Console store listing experiments. Compare a "classic Snake, modern" listing with a "battle your friends" listing.
4. **Ad check** in AdMob for invalid click warnings during closed testing.

## 9. Decisions applied in spec 2.0

| ID | Decision |
|---|---|
| M1 | Modern rules are the default. Retro 97 keeps the 1.2 rules as a separate mode |
| M2 | Standard arena for all ranked modes, fit to screen arena for Endless |
| M3 | Smooth movement at the display refresh rate |
| M4 | Power ups, combos, near misses and golden food |
| M5 | XP, levels, earned unlocks, daily missions, streaks with freezes |
| M6 | Play Games Services v2 sign in, 4 leaderboards, 25 achievements, saved games |
| M7 | Daily Run and Weekly Arena seeded from UTC |
| M8 | Nearby Battle for 2 to 4 players |
| M9 | One anchored adaptive banner with a reserved strip and a 24 dp gap |
| M10 | One time `remove_ads` purchase, acknowledged at once, restored on every launch, cached locally |
| M11 | UMP consent, Privacy options, AD_ID declaration, honest Data safety form |
| M12 | No other ad formats, purchases, loot boxes or energy systems |
| M13 | Full game works without Google Play services and without internet |
| M14 | Microsoft bindings of Google libraries, each behind an interface |
| M15 | Optional break reminder, no guilt messages |
| M16 | No Nokia name, logo or real phone design |

Version 1 decisions S2 (daily challenge), S3 (challenge codes), S6 (medals, now Play Games achievements), S7 (handset frame, now only in Retro 97), S12 (share card) and S13 (no Nokia branding) live on. S4 (Purist) lives on inside Retro 97. S5, S9 (no notifications stays), S10, S11 and S14 are replaced by M9 to M12.

## 10. References

[1] Google Play, "Snake Clash!". https://play.google.com/store/apps/details?id=io.supercent.linkedcubic&hl=en_US

[2] Google Play, "Snake.io - Fun Snake .io Games". https://play.google.com/store/apps/details?id=com.amelosinteractive.snake&hl=en_US

[3] Google Play, "Snake Lite - Snake Game". https://play.google.com/store/apps/details?id=com.snake.io.slither.worms&hl=en_US

[4] Google Play, "Snake" by Infinità Game Studio. https://play.google.com/store/apps/details?id=com.ericcbm.snake&hl=en

[5] Google Play, "Snake Game" by Puzzle Pixel Studio. https://play.google.com/store/apps/details?id=com.puzzlepixelstudio.snake_game&hl=en_US

[6] Shane the Gamer, "Retro Gaming Statistics 2026". https://www.shanethegamer.com/research/retro-gaming-statistics/

[7] WARC, "Best in brief: The principle of positioning" (Keller and Tybout). https://www.warc.com/content/article/mkt/best-in-brief-the-principle-of-positioning/en-gb/77216

[8] Mixpanel, "Points of parity vs points of difference". https://mixpanel.com/blog/points-of-parity-vs-points-of-difference/

[9] Attest, "Game on. What 2026's gamers are playing, paying and prioritising". https://www.askattest.com/blog/research/gaming-consumer-insights-2025

[10] eMarketer, "Mobile gaming's high attention is untapped" (January 2026). https://www.emarketer.com/content/mobile-gaming-s-high-attention-untapped

[11] AppFollow, "Mobile Gaming Trends 2026". https://appfollow.io/blog/mobile-gaming-trends-2026

[12] Android Developers, "Back up user data with Auto Backup". https://developer.android.com/identity/data/autobackup

[13] 9to5Google, "Google Play Games Services shutting down multiplayer API" (2019). https://9to5google.com/2019/09/16/google-play-games-services-shutting-down-multiplayer-api/

[14] Android Developers Blog, "Prepare your app for the resizability and orientation changes in Android 17". https://android-developers.googleblog.com/2026/02/prepare-your-app-for-resizability-and.html

[15] AdMob Help, "Google consent management requirements for serving ads in the EEA, the UK and Switzerland". https://support.google.com/admob/answer/13554116

[16] 9to5Google, "Google Play Store Data safety section". https://9to5google.com/2022/04/26/google-play-store-data-safety/

[17] MEL Magazine, oral history of Snake on the Nokia 6110. https://melmagazine.com/en-us/story/snake-nokia-6110-oral-history-taneli-armanto

[18] Windows Blog, "Snake charmed. 10 fascinating facts about the world's most popular game". https://blogs.windows.com/devices/2012/02/02/snake-charmed-10-fascinating-facts-about-the-worlds-most-popular-game/

[19] ThePrint, "Nokia's Snake, the mobile game that became an entire generation's obsession". https://theprint.in/feature/nokias-snake-the-mobile-game-that-became-an-entire-generations-obsession/462873/

[20] Android Developers, "Leaderboards" (Play Games Services). https://developer.android.com/games/pgs/leaderboards

[21] Google for Developers, "Nearby Connections". https://developers.google.com/nearby/connections

[22] Blue Ocean Strategy, "Creating your Eliminate Reduce Raise Create Grid". https://www.blueoceanstrategy.com/wp-content/uploads/2022/12/Creating-your-Eliminate-Reduce-Raise-Create-Grid.pdf

[23] Kim, W. C. and Mauborgne, R. (2005, expanded 2015). Blue Ocean Strategy. Harvard Business Review Press.

[24] NuGet, ExzileGames.AndroidPlayGamesBridge. https://www.nuget.org/packages/ExzileGames.AndroidPlayGamesBridge

[25] Android Developers, "Google Play Billing Library deprecation FAQ". https://developer.android.com/google/play/billing/deprecation-faq
