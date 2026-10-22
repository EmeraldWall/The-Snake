# The Snake Positioning and Blue Ocean Strategy

Points of parity, points of difference and a Blue Ocean plan for The Snake on Android 17
October 2026

## 1. Summary

The Snake game market on Android is already crowded, but almost all of the big players compete the same way. They offer online multiplayer, bright 3D or cartoon graphics, skins, and income from ads and in app purchases. Snake Clash!, Snake.io and Snake Lite each show 100M+ downloads on Google Play [1] [2] [3]. Fighting them on those terms (a red ocean) would need servers, content teams and an ad budget this project does not have.

Our recommendation is to leave that fight and build a different offer. The game is aimed at people who grew up with Snake on 1990s phones and at players who are tired of ad heavy games. In plain words, the pitch is "the Snake you remember, the way a modern Android game should work, and nothing else". The app shows no ads, needs no account, never goes online, starts in seconds and plays perfectly on any screen or controller. Two new ideas that need no servers set it apart. One is a **daily challenge** that every player in the world gets the same, and the other is **seed challenge codes** that let friends race on the same board.

Section 7 lists every change that this strategy adds to the spec (S1 to S14). They are applied in spec version 1.2.

## 2. Frame of reference

Positioning starts by choosing who the product is compared with [4]. The Snake is placed in **"offline casual arcade games for short breaks"**, and within that, **classic Snake games**. The real alternatives for our target players are

| Group | Examples | What they offer |
|---|---|---|
| Online snake battle games | Snake Clash! (100M+ on Google Play), Snake.io (100M+), Snake Lite (100M+) [1] [2] [3] | Multiplayer, growth and cosmetics, very large audiences |
| Small classic snake clones | "Snake" by Infinità Game Studio (100K+), "Snake Game" by Puzzle Pixel Studio (1K+) [5] [6] | Simple grid snake, usually free |
| Other ways to get a quick retro fix | Retro handhelds, emulators and re releases | The broader retro market, which secondary reports estimate at a few billion US dollars a year and say is driven by millennials and Gen X [7] |

The retro market figures in the last row come from secondary statistics pages whose original source is unclear, so they show a direction, not a fact to rely on [7].

## 3. Points of parity (the minimum Android gamers expect today)

A point of parity is a feature a product needs just to be considered. It is the minimum requirement of the category, not a reason to choose it [4] [8]. If any of these is missing, players will reject the game however good the nostalgia is.

| POP | Why it is expected today | How The Snake meets it (all within MAUI and Visual Studio) |
|---|---|---|
| Free to try | The leading snake games are free downloads [1] [2] [3] | Free on Google Play |
| Progress that survives a new phone | Review analysis of mobile games finds lost saves and device transfers lead to uninstalls, and calls cloud save "table stakes" [9] | **Android Auto Backup**, which is built into Android. It saves app data, including shared preferences, to a private Google Drive area, up to 25 MB per app, without counting against the player's Drive storage [10]. MAUI `Preferences` on Android are stored as shared preferences, so settings, scores and medals are covered. No package is needed (S1) |
| Tight, fair controls and haptics | Expected in any action game | Already in spec 1.1. Two turn input queue, keys react on press, haptic ticks |
| Instant resume | Mobile play happens in short breaks | Auto save on background and instant Continue (spec 1.1) |
| Dark theme | Most modern apps offer one | Night theme (spec 1.1) |
| Modern Android behaviour | Back gesture, edge to edge layout, any orientation and window size on large screens for API 37 apps [11] | Spec 1.1 sections 3.2, 6.4 and 7.4 |
| Some sense of progress | Most current games give goals beyond "beat your high score" | **Local medals.** 12 medals stored on the phone, with no Google Play Games package needed (S6) |
| Accessibility basics | Expected by players, and part of the UI and UX audit | Contrast, 48 dp touch targets, text scaling, TalkBack menus (UIUXAudit.md) |
| A clear privacy declaration | Every app on Google Play must fill in the Data safety form, even one that collects nothing, and link a privacy policy [12] | Declare "no data collected or shared" and publish a one page privacy policy (S11) |
| Store quality | A modern target API level, a working icon, screenshots that look current | API 37 target, adaptive icon, screenshots on phone, tablet and foldable |

**Parity we choose not to match.** Online multiplayer, a skin shop and live events are standard in the online snake battle group. We deliberately do not match them. Keller and colleagues point out that parity is about the frame of reference you choose, so by placing The Snake among classic, offline games these are not required [4].

## 4. Points of difference

A good point of difference must be **desirable** to the players, **deliverable** by the team, and **different** from competitors. Contradictions between POPs and PODs must be removed [8].

| POD | Desirable? | Deliverable? | Different? |
|---|---|---|---|
| **1. The real 1990s phone Snake feeling.** Two colour LCD look, keypad, beeps, critters, mazes, plus a "Purist" option (S4) | Snake was built into the Nokia 6110 in 1997 and then shipped on Nokia phones for about ten years, on hundreds of millions of handsets by Microsoft's and ThePrint's estimates [13] [14] [15]. That is a very large group with a shared memory | Yes. 2D grid drawing in `GraphicsView` | The large snake games use a modern .io style. Faithful versions exist but appear to be small (assumption to check, see section 8) |
| **2. Calm and private.** No ads, no purchases, no account, no tracking, and no internet permission at all | About a third of US and UK gamers in an Attest survey found in game ads intrusive [16]. Other players accept ads [17], so this is a POD for a segment, not for everyone | Yes. Simply leave things out. The Release manifest asks for no internet permission (S10) | The top snake games are free to download, and free games commonly rely on ads (we have not checked each listing, see section 8) |
| **3. Instant.** From tap on the icon to first move in under 3 seconds | Players want quick sessions | Yes. One system splash, menu remembers level and mode | A measurable promise we can show in a store video |
| **4. Plays best on every Android screen.** Phones, tablets, foldables, keyboards and controllers, with a classic handset frame on big screens (S7) | Android 17 forces large screen readiness anyway [11], so most small clones will look stretched until they update | Yes. Already in spec 1.1 plus the frame | Uncommon for small classic clones (assumption) |
| **5. Play with friends without going online.** Daily challenge and seed challenge codes (S2, S3) | Social comparison drives intention (TPB subjective norm) and visibility drives adoption (IDT observability), see `UIUXAudit.md` | Yes. The game engine already uses a seedable `IRandomSource`. Codes are shared as plain text with the MAUI Share API | New. We found no classic snake game doing this (assumption to check) |

**Contradiction check.** "Play with friends" could clash with "never goes online". It is solved by sharing codes and scores as text through apps the player already uses, so The Snake itself never connects. "Progress survives a new phone" could clash with "no account". It is solved by Android Auto Backup, which uses the player's existing Google account at system level, not ours [10].

**Legal care with the nostalgia.** "Nokia" is a trademark of another company. The app name, icon, store listing and handset frame must not use the Nokia name, logo or a copy of a real Nokia phone design. Say "classic 90s phone Snake" instead. This is a project rule, not legal advice, so check the final listing with someone qualified.

## 5. Blue Ocean strategy

### 5.1 The idea

Blue Ocean Strategy, by W. Chan Kim and Renée Mauborgne, says a company should stop competing head on in a crowded market and create new market space. It does this by raising value for buyers while cutting cost at the same time, which the authors call value innovation. The main tools are the strategy canvas and the Four Actions Framework, which asks what to **eliminate**, **reduce**, **raise** and **create**. The ERRC grid turns these four questions into a working table [18] [19].

### 5.2 Strategy canvas

The scores below (0 means none, 10 means very high) are the team's estimates of a typical game in each group. They have not yet been checked against real listings (section 8).

| Factor that games compete on | Online snake battle games | Small classic clones | **The Snake** |
|---|---|---|---|
| Online multiplayer | 9 | 0 | 0 |
| Skins, shop and cosmetics | 9 | 2 | 0 |
| Graphics detail and effects | 8 | 3 | 2 |
| Ads and interruptions | 8 | 6 | 0 |
| Account or login prompts | 5 | 1 | 0 |
| Download size (higher means bigger) | 6 | 3 | 1 |
| Time to first move | 4 | 6 | 9 |
| Faithfulness to 90s phone Snake | 1 | 4 | 10 |
| Fair, precise controls | 5 | 4 | 9 |
| Privacy (no data collected, no internet) | 2 | 4 | 10 |
| Large screen, keyboard and controller support | 4 | 2 | 9 |
| Accessibility | 4 | 2 | 8 |
| Playing with friends without servers | 0 | 0 | 8 |
| Daily shared challenge | 2 | 0 | 8 |

The shape is what matters. The Snake drops to zero where the big games spend the most money, and rises high on factors nobody else in the category competes on.

### 5.3 ERRC grid

| Eliminate | Reduce |
|---|---|
| Ads of every kind | Graphics to 2 colours and pixel sprites |
| In app purchases, shops and currencies | Download size (target under 40 MB) |
| Accounts, logins and sign in prompts | Menu depth (one level, 7 items) |
| Online servers and multiplayer | Number of settings (one page) |
| Energy, lives and wait timers | Text and tutorials (one line hints) |
| Push notifications | Battery use (no drawing while paused) |
| Data collection, analytics and the internet permission | Permissions (vibration only) |

| Raise | Create |
|---|---|
| Faithfulness to 90s phone Snake, with a Purist option | **Daily challenge.** The board is generated from the date, so every player gets the same game that day without a server (S2) |
| Fairness and precision of controls | **Seed challenge codes.** "Beat my 01234 on board K7Q2M9XA" shared as text. A friend types the code and plays the same food sequence (S3) |
| Speed from launch to first move | **Purist and Modern modes.** Purist turns off modern aids, Modern keeps them (S4) |
| Privacy you can check yourself (no internet permission in the Play listing) | **Classic handset frame** on tablets and foldables, a generic 90s phone around the screen (S7) |
| Large screen, keyboard and controller quality | **Local medals**, earned and kept on the phone (S6) |
| Accessibility | **Memory lane card.** A share card that says when Snake first came out and the player's best score (S12) |

### 5.4 Noncustomers

Blue Ocean thinking also looks at people who do not buy in the category today [19]. For The Snake these are

* **Soon to be noncustomers.** People who still play a snake game now and then, but only because it is there, and who would leave for anything better. The instant start and daily challenge give them a reason to stay.
* **Refusing noncustomers.** People who uninstall games because of ads, purchases or privacy worries. The eliminate column is aimed straight at them.
* **Unexplored noncustomers.** Adults who do not think of themselves as gamers but remember Snake on their first phone. The nostalgia, the simple controls and the lack of purchases make the game feel safe to try.

Families with children are a possible group too. If the store listing targets children, Google Play's extra rules for apps aimed at children apply (check the current Families policy in Play Console Help), which adds work. Version 1.0 should therefore not target children in the store listing.

### 5.5 Business model

Value innovation also needs a price that fits [18]. Two options fit the "MAUI and Visual Studio only" rule, because in app billing would need an extra package.

| Option | Good | Bad |
|---|---|---|
| **Free, no ads (recommended for 1.0)** | Best for trialability and word of mouth. Fully matches the "nothing to sell you" promise | No income |
| Paid upfront at a low price | Some income, no extra package needed | Fewer people try it, and it competes with free snake games |

Recommendation: launch free to build an audience and reviews. Decide about a paid "Deluxe" edition only after the first 3 months of data from the Play Console, which needs no package in the app.

### 5.6 Positioning statement

For people who grew up with Snake on their first mobile phone, and for players tired of ad filled games, **The Snake** is the classic 90s phone Snake rebuilt for modern Android. Unlike online snake battle games, it has no ads, no accounts and no internet connection. It starts in seconds, plays perfectly on any screen or controller, and lets you challenge friends with a simple code.

## 6. How this fits the earlier theories

| Earlier finding | How the strategy uses it |
|---|---|
| TPB attitude (nostalgia, fun with no hassle) | POD 1, POD 2, POD 3 |
| TPB subjective norm (friends see my score) | Daily challenge, seed codes and memory lane card |
| TPB perceived behavioural control | POPs on controls, accessibility and progress that survives a new phone |
| IDT relative advantage and observability | The Eliminate column and the shareable codes |
| IDT trialability | Free, no account, under 3 seconds to play |
| VRIO, no feature is hard to copy and the real edge is organisation | Blue Ocean avoids a head on fight. The combination of choices and the quality of execution are the advantage, not any one feature |

## 7. Changes to the spec (version 1.2)

| ID | Change |
|---|---|
| S1 | Turn on Android Auto Backup for `Preferences` data, with a backup rules file. Exclude the paused game |
| S2 | Daily challenge mode. The seed is built from the local date, the same for every player. One scored attempt per day, then practice runs |
| S3 | Seed challenge codes. Any finished game can be shared as an 8 character code. A friend can enter it from the main menu and play the same food and critter sequence |
| S4 | Purist and Modern play styles. Purist uses a 1 turn input queue, no resume countdown and 4 digit score display with a 9999 cap. Modern is the default and keeps all aids from spec 1.1 |
| S5 | Short "Why no ads" note in About, to make the privacy POD visible |
| S6 | 12 local medals, kept in `Preferences` and covered by Auto Backup |
| S7 | Classic handset frame on screens 600 dp wide or more, generic design with no real brand |
| S8 | Launch to first move under 3 seconds on a mid range phone |
| S9 | No push notifications, no drawing while paused, timer stopped when the game is not running |
| S10 | Release build requests no internet permission. Only vibration is requested |
| S11 | Data safety form declares no data collected or shared, plus a one page privacy policy |
| S12 | Memory lane share card text, offered from the Top scores screen |
| S13 | No Nokia name, logo or real phone design anywhere in the app or listing |
| S14 | Store listing leads with "No ads. No account. No internet." and shows a phone, a tablet with the handset frame, and a controller |

## 8. What to check before launch

1. **Play Store scan.** Look at the top 30 results for "snake" and "classic snake". For each, note whether it has ads, purchases, login prompts, offline play, controller support and a large screen layout. Update the strategy canvas scores with real data.
2. **POD test.** Show 3 store listing versions (nostalgia first, privacy first, friends challenge first) to 10 to 15 people from the target groups and ask which they would install.
3. **Backup test.** Install, play, set scores, then install on a second phone with the same Google account and check that scores and medals come back. Auto Backup keeps only one backup at a time, so also check what happens after reinstalling [10].
4. **Trademark check** of the name, icon and listing (S13).

## 9. References

[1] Google Play, "Snake Clash!" listing. https://play.google.com/store/apps/details?id=io.supercent.linkedcubic&hl=en_US

[2] Google Play, "Snake.io - Fun Snake .io Games" listing. https://play.google.com/store/apps/details?id=com.amelosinteractive.snake&hl=en_US

[3] Google Play, "Snake Lite - Snake Game" listing. https://play.google.com/store/apps/details?id=com.snake.io.slither.worms&hl=en_US

[4] WARC, "Best in brief: The principle of positioning" (summary of Keller and Tybout's frame of reference, POP and POD process). https://www.warc.com/content/article/mkt/best-in-brief-the-principle-of-positioning/en-gb/77216 . Original article is Keller, K. L., Sternthal, B. and Tybout, A. (2002), "Three Questions You Need to Ask About Your Brand", Harvard Business Review.

[5] Google Play, "Snake" by Infinità Game Studio listing. https://play.google.com/store/apps/details?id=com.ericcbm.snake&hl=en

[6] Google Play, "Snake Game" by Puzzle Pixel Studio listing. https://play.google.com/store/apps/details?id=com.puzzlepixelstudio.snake_game&hl=en_US

[7] Shane the Gamer, "Retro Gaming Statistics 2026". https://www.shanethegamer.com/research/retro-gaming-statistics/ (secondary statistics, original source unclear)

[8] Mixpanel, "Points of parity vs points of difference". https://mixpanel.com/blog/points-of-parity-vs-points-of-difference/

[9] AppFollow, "Mobile Gaming Trends 2026. The Player Side Pillar". https://appfollow.io/blog/mobile-gaming-trends-2026

[10] Android Developers, "Back up user data with Auto Backup". https://developer.android.com/identity/data/autobackup

[11] Android Developers Blog, "Prepare your app for the resizability and orientation changes in Android 17" (February 2026). https://android-developers.googleblog.com/2026/02/prepare-your-app-for-resizability-and.html

[12] 9to5Google, "Google Play Store Data safety section" (all apps must complete the form, including apps that collect no data). https://9to5google.com/2022/04/26/google-play-store-data-safety/

[13] MEL Magazine, oral history of Snake on the Nokia 6110 with Taneli Armanto. https://melmagazine.com/en-us/story/snake-nokia-6110-oral-history-taneli-armanto

[14] Windows Blog, "Snake charmed. 10 fascinating facts about the world's most popular game" (2012). https://blogs.windows.com/devices/2012/02/02/snake-charmed-10-fascinating-facts-about-the-worlds-most-popular-game/

[15] ThePrint, "Nokia's Snake, the mobile game that became an entire generation's obsession". https://theprint.in/feature/nokias-snake-the-mobile-game-that-became-an-entire-generations-obsession/462873/

[16] Attest, "Game on. What 2026's gamers are playing, paying and prioritising". https://www.askattest.com/blog/research/gaming-consumer-insights-2025

[17] eMarketer, "Mobile gaming's high attention is untapped" (January 2026). https://www.emarketer.com/content/mobile-gaming-s-high-attention-untapped

[18] Blue Ocean Strategy (Kim and Mauborgne), "Creating your Eliminate Reduce Raise Create Grid" (official template). https://www.blueoceanstrategy.com/wp-content/uploads/2022/12/Creating-your-Eliminate-Reduce-Raise-Create-Grid.pdf

[19] Kim, W. C. and Mauborgne, R. (2005, expanded 2015). Blue Ocean Strategy. Harvard Business Review Press. Noncustomer tiers and the Four Actions Framework are described in the book.
