# The Snake UI and UX Audit

Audit of `SnakeGameSpecification.md` version 1.0, with design decisions backed by the Theory of Planned Behaviour, Innovation Diffusion Theory and VRIO. Section 10 audits the version 2.0 changes (ads, purchase, online play, modern rules)
October 2026

## 1. Summary

The 1.0 spec has a strong base. The controls are flexible, the two colour themes have high contrast, the game pauses safely, and nothing has to be set up before you play. The audit found **2 high, 8 medium and 6 low** severity problems. The two most important are a score display that can overflow, and no visible pause control when swipe only controls are chosen.

The three theories were then used to decide what to add, change or drop. They point the same way. Make the first game happen within seconds, make the game feel like the Nokia original people remember, and make a good score easy to show to others. They also warn against adding online features, which cost a lot and give little advantage.

Every accepted change is listed in section 7 and has been applied in spec version 1.1.

## 2. Method

1. **Heuristic review.** Each screen, control and state in the spec was checked against Nielsen's 10 usability heuristics. These are broad rules of thumb for judging an interface, not detailed guidelines [1].
2. **Accessibility checks.** The spec was checked against the WCAG 2.2 rules for text contrast (4.5 to 1 for normal text), non text contrast (3 to 1), and flashing (no more than three flashes in any one second) [2] [3] [4]. It was also checked against Android's minimum touch target of 48 by 48 dp [5].
3. **Platform checks.** Android 17 and Android 12 launch behaviour were checked against Google's own documentation [6] [7].
4. **Theory based decisions.** Each finding and each candidate feature was then judged with the Theory of Planned Behaviour [8], Innovation Diffusion Theory [9] and VRIO [10].

Severity scale used in this audit:

| Severity | Meaning |
|---|---|
| High | Breaks the game or blocks a common task. Must fix before release |
| Medium | Causes confusion, friction or excludes some players. Should fix in 1.0 |
| Low | Polish. Fix if time allows |
| Pass | Checked and found good. No change needed |

This is an expert review of a written spec. It does not replace testing with real players, who find different problems [1]. Section 8 lists the player tests to run once a build exists.

## 3. Findings

### 3.1 High severity

**UX 01. The score can overflow its 4 digit display.**
The status bar shows the score with 4 digits. On a 20 by 28 grid at level 9, normal food alone can give about 5,000 points (556 foods x 9). Critters can add up to about 20,000 more (111 critters x 20 ticks x 9), plus the 900 point win bonus. That makes about 25,900 points in theory, which needs 5 digits.
*Heuristic.* Visibility of system status.
*Fix.* Show 5 digits (`00042`) and cap the score at 99999.

**UX 02. No visible pause control in swipe only mode.**
Pause is only on keypad key 5, a keyboard, a controller or the back gesture. If a player picks swipe only controls, the only way to pause is the back gesture, which many players will not think of.
*Heuristic.* User control and freedom.
*Fix.* Add a pause icon at the right of the status bar in every control mode, with a 48 dp touch target.

### 3.2 Medium severity

**UX 03. Two splash screens in a row.**
The spec adds a 1.5 second Splash page. On Android 12 and later the system already shows its own splash screen at launch, so a separate splash activity or page appears as a second splash after the system one [6]. This delays play and conflicts with the "under 2 seconds to menu" target.
*Heuristic.* Aesthetic and minimalist design.
*Fix.* Remove `SplashPage`. Keep only the launch image in `Resources/Splash`, and show the pixel snake logo at the top of the main menu instead.

**UX 04. Reset scores has no confirmation.**
One tap in Settings deletes every high score for good.
*Heuristic.* Error prevention.
*Fix.* Show a confirmation dialog. Its default button is "Cancel", and the delete button says "Delete all scores" instead of "OK".

**UX 05. New players have to find the instructions on their own.**
Instructions are a separate menu item. A first time player who taps New game sees a still snake and no hint about what to do.
*Heuristic.* Help and documentation, and recognition rather than recall.
*Fix.* In the Ready state, show a one line hint over the playfield that matches the chosen controls, such as "Swipe to start" or "Press 2, 4, 6 or 8". On the very first game also show a small picture of the controls. The hint goes away on the first input.

**UX 06. Swipes near the screen edge clash with Android's back gesture.**
With gesture navigation, a swipe that starts at the left or right edge is the system back gesture. A player swiping left or right near the edge will pause the game by accident.
*Heuristic.* Error prevention.
*Fix.* Only start a swipe when the finger first touches at least 24 dp in from the left and right screen edges. If gesture navigation is on and controls are set to Swipe, show the hint "Tip. Swipe from the middle of the screen" once.

**UX 07. Keypad key size is not specified.**
The spec does not say how large keypad keys are. Android recommends touch targets of at least 48 by 48 dp, and larger is better [5]. In a fast game, small keys cause missed turns.
*Heuristic.* Error prevention. Accessibility.
*Fix.* Keypad keys are at least 64 by 64 dp on phones, never below 48 dp, with at least 8 dp between keys.

**UX 08. Text scaling is not specified.**
The spec does not say what happens when a player sets a large font size in Android settings. A pixel font at a small size is hard to read.
*Heuristic.* Accessibility.
*Fix.* Menus, settings, instructions and dialogs follow the system font scale up to 200 percent and stay usable. Text drawn inside the playfield (score digits, "Paused", countdown) is drawn as pixels and does not scale, but it is never smaller than 16 dp tall.

**UX 09. No way to share a good score.**
Players can only see their scores inside the app. Nothing makes a score visible to friends. The theories in section 4 and 5 show this matters for adoption.
*Heuristic.* Not a heuristic issue. It comes from TPB (subjective norm) and IDT (observability).
*Fix.* Add a "Share" button on the Game over screen. It uses the built in MAUI `Share.Default.RequestAsync` with a `ShareTextRequest` [11], so no extra package is needed. Example text is "I scored 01234 on level 7 in The Snake (Classic, walls off)".

**UX 10. Sound settings and volume behaviour are not specified.**
The spec does not say whether sound is on by default or which volume controls it. Beeps that ignore the volume buttons annoy players in quiet places.
*Heuristic.* User control and freedom.
*Fix.* Sound is on by default and plays on the media volume stream, so the phone's volume buttons control it. Vibration is on by default. Both are set on the first page of Settings.

### 3.3 Low severity

**UX 11. Name entry every time a high score is set.**
Typing a name after every high score slows down the "one more go" loop.
*Fix.* Fill the name box with the last name used. The keyboard does not open until the player taps the box.

**UX 12. Mazes are picked by name only.**
"Box", "Tunnel" and "Mill" mean nothing until you have played them.
*Heuristic.* Recognition rather than recall.
*Fix.* Show a small preview of each maze in the Mode menu, drawn with the same `GameDrawable`.

**UX 13. The critter countdown is shown as an abstract number.**
"Ticks left" is not a unit players understand.
*Heuristic.* Match between the system and the real world.
*Fix.* Show the critter icon with a bar that shrinks, next to the number.

**UX 14. Retrying takes one tap too many.**
*Heuristic.* Flexibility and efficiency of use.
*Fix.* On Game over, "Play again" is the selected item, so key 5, Enter, the A button or one tap restarts with the same level and mode.

**UX 15. Animations cannot be reduced.**
The death blink and the bulge moving down the snake are movement that some players want to turn off.
*Fix.* When the Android "Remove animations" setting is on (read through platform code in `Platforms/Android`), skip the bulge and show the snake solid for 750 ms instead of blinking.

**UX 16. LCD ghosting lowers contrast.**
The trail is drawn at 30 percent opacity, which gives about 1.66 to 1 contrast on the Classic LCD theme, below the 3 to 1 non text contrast level. It is decorative, so this is allowed, but it can look like part of the snake.
*Fix.* Keep ghosting off by default and describe it in Settings as "Retro screen blur (decorative)".

### 3.4 Passed checks

| Check | Result |
|---|---|
| Text and pixel contrast, Classic LCD theme (`#2B3A1F` on `#C7D3A6`) | 7.7 to 1. Passes AA and AAA for text [2] |
| Blue backlight theme (`#10283A` on `#9FC6D9`) | 8.33 to 1. Passes AA and AAA [2] |
| Night theme (`#8FD18F` on `#101410`) | 10.34 to 1. Passes AA and AAA [2] |
| Food, critters and maze told apart by shape, not by colour | Pass. Works for colour blind players |
| Death blink, 3 blinks at 250 ms on and off | 2 flashes per second, below the limit of 3 per second [4] |
| Countdown before resuming | Pass. Stops sudden deaths after a pause |
| Pause on background, rotation and back gesture | Pass. Matches Android 17's rule that apps targeting API 37 must handle any window size and orientation on large screens [7] |
| Left and right handed keypad | Pass |
| Several control methods | Pass. Good for accessibility and players' sense of control |

The contrast ratios were worked out with the WCAG relative luminance formula from the hex values in the spec.

## 4. Theory of Planned Behaviour (TPB)

### 4.1 The theory in brief

TPB, published by Icek Ajzen in 1991, says a person's **intention** to do something is the best predictor of whether they do it. Intention in turn is predicted by three things [8]:

* **Attitude** towards the behaviour, which is how good or bad the person thinks doing it would be
* **Subjective norm**, which is whether people who matter to them would approve
* **Perceived behavioural control**, which is how easy or hard they think it is for them to do it

For this app, the behaviours that matter are installing the game, playing a first game, and coming back to play again.

### 4.2 How TPB shaped the spec

| TPB factor | What players are likely to believe (assumption to test) | Spec decision |
|---|---|---|
| Attitude | "This will bring back the Nokia game I loved." "It will be quick fun with no hassle." | Keep the authentic 2 colour look, Nokia keypad and beeps. Keep the app free of ads, accounts and tracking. Show the snake logo at once rather than a slow splash (UX 03) |
| Attitude | "Losing feels fair." | Two queued turns, no 180 degree deaths, countdown after pause, keys that react on press |
| Subjective norm | "My friends would enjoy seeing my score." | Share score button (UX 09). A shareable result written in a fixed, easy to compare format |
| Subjective norm | "It is OK to play this for a minute in public." | Sound follows media volume, so it can be silent without a settings visit (UX 10) |
| Perceived behavioural control | "I will know how to play straight away." | First game hint (UX 05), visible pause (UX 02), maze previews (UX 12) |
| Perceived behavioural control | "I can control it well on my phone." | Large keypad keys (UX 07), edge safe swipes (UX 06), keyboard and controller support, text scaling (UX 08), reduced motion (UX 15) |
| Perceived behavioural control | "I can play in short breaks." | Auto save on background, instant Continue, one tap Play again (UX 14) |

The beliefs in the middle column are working assumptions, not research results. Section 8 lists how to check them.

## 5. Innovation Diffusion Theory (IDT)

### 5.1 The theory in brief

Everett Rogers' Diffusion of Innovations names five features of an innovation that affect how fast people take it up [9]:

* **Relative advantage.** How much better it seems than the alternatives. Often said to be the strongest of the five.
* **Compatibility.** How well it fits people's values, past experience and needs.
* **Complexity.** How hard it seems to understand and use. This is the only one that slows adoption.
* **Trialability.** How easily people can try it on a small scale first.
* **Observability.** How visible its results are to other people.

These features are judged by the people who might adopt it, not by the team that builds it [9].

### 5.2 How IDT shaped the spec

| IDT attribute | Spec decision | Why |
|---|---|---|
| Relative advantage | No ads, no accounts, works offline, small download (under 40 MB), controller and keyboard support, works well on tablets and foldables | These are the practical ways a free Snake game can be clearly better than the alternatives. Whether they are actually rare in the Play Store needs checking before launch (section 6) |
| Compatibility | Nokia rules, look, keypad layout and beeps. Android 17 conventions such as the back gesture, system splash, media volume and adaptive layouts | Fits both what players remember and how they expect a modern Android app to behave. Removing the custom splash (UX 03) is a compatibility fix |
| Complexity (keep it low) | One tap from the menu to play. Hints in the Ready state (UX 05). Only 7 menu items. Settings grouped on one page | Lower complexity means faster adoption |
| Trialability | Free with no sign up. Playable in a few seconds from launch. Level and mode remembered, so trying another level is one change | Lets someone "try one game" with almost no effort |
| Observability | Share score (UX 09). A distinctive 2 colour look that is easy to recognise in screenshots | Makes results visible to other people |

## 6. VRIO analysis of candidate features

### 6.1 The framework in brief

VRIO comes from Jay Barney's resource based view of the firm [10]. It asks four questions about a resource or capability:

* **Valuable.** Does it help the player, or the product, in a way that matters?
* **Rare.** Do few competing apps have it?
* **Inimitable.** Is it hard or costly for others to copy?
* **Organised.** Is the team set up to use it fully?

A resource that fails "Valuable" is a disadvantage. One that is valuable but common only gives parity. Passing all four can give a lasting advantage [10]. In this audit each spec feature is treated as a candidate resource.

### 6.2 Evaluation

Ratings of "Rare" are hypotheses. They have not been checked against the Play Store and must be checked before launch.

| Candidate feature | V | R | I | O | Outcome | Decision |
|---|---|---|---|---|---|---|
| Core Snake rules | Yes | No | No | Yes | Parity | Keep. Players expect it |
| Faithful Nokia look, beeps, keypad and critters | Yes | Possibly | No | Yes | Short term advantage | Keep and polish. This is the product's main identity |
| No ads, no accounts, offline, privacy | Yes | Possibly | No (but a cost to rivals who rely on ad money) | Yes | Short term advantage | Keep. Say so on the store listing |
| Feel and fairness (input queue, press response, countdown) | Yes | Possibly | Partly. Hard to copy without careful tuning and testing | Yes, through unit tests and the timing target | Possible lasting advantage | Keep. Invest in timing tests |
| Large screen, foldable and controller support | Yes | Possibly | No | Yes | Short term advantage | Keep. Also needed for Android 17 [7] |
| Share score as text | Yes | No | No | Yes | Parity | Add. Cheap and supports TPB and IDT |
| Accessibility (contrast, text scaling, TalkBack menus, reduced motion) | Yes | Possibly | No | Yes | Short term advantage | Keep and extend (UX 07, 08, 15) |
| Custom 1.5 second splash page | No | No | No | No | Disadvantage | Drop (UX 03) |
| Online leaderboards and accounts | Yes for some players | No | No | No. Needs servers, sign in and moderation, and breaks the no accounts and no packages rules | Parity at high cost | Do not add in 1.0 |
| Ads or in app purchases | No for players | No | No | No | Disadvantage | Do not add |

### 6.3 What VRIO tells us

No single feature of a Snake game is hard to copy. The only real chance of a lasting advantage is **organisation**, meaning the team's ability to ship a game that feels exactly right and keeps that quality over time. This is why the spec keeps the core logic separate from the UI and unit tested, sets numeric timing targets, and keeps the feature list small.

## 7. Decisions applied to spec version 1.1

| ID | Change | Source |
|---|---|---|
| D1 | Score shown with 5 digits, capped at 99999 | UX 01 |
| D2 | Pause icon in the status bar for every control mode | UX 02, TPB control |
| D3 | Remove the Splash page and use only the system launch splash | UX 03, IDT compatibility, VRIO |
| D4 | Confirmation dialog before deleting scores | UX 04 |
| D5 | Ready state hint and first game controls picture | UX 05, TPB control, IDT complexity and trialability |
| D6 | Swipes must start 24 dp in from the side edges, plus a one time tip | UX 06 |
| D7 | Keypad keys at least 64 dp on phones, never under 48 dp, 8 dp gaps | UX 07 |
| D8 | Menus follow system font scale up to 200 percent, playfield text at least 16 dp tall | UX 08 |
| D9 | Share score on Game over with the MAUI Share API | UX 09, TPB norm, IDT observability, VRIO |
| D10 | Sound and vibration on by default, sound on the media volume stream | UX 10, TPB norm |
| D11 | Prefill the last used name | UX 11 |
| D12 | Maze previews in the Mode menu | UX 12 |
| D13 | Critter countdown shown as a shrinking bar plus number | UX 13 |
| D14 | "Play again" selected by default on Game over | UX 14, TPB control |
| D15 | Respect the Android "Remove animations" setting | UX 15 |
| D16 | Ghosting off by default and labelled as decorative | UX 16 |
| D17 | No online leaderboards, accounts, ads or purchases in 1.0 | VRIO |

## 8. Validation plan

The theory based assumptions above should be checked with real players.

1. **Play Store scan before launch.** Look at the top Snake games on Google Play and note which have ads, need accounts, support controllers and tablets, and copy the Nokia look. This tests the "Rare" column in section 6.
2. **First play test with 5 to 8 people** who remember Nokia phones and 3 to 5 who do not. Measure the time from launch to first move, whether they find pause, and missed turns at level 5.
3. **Short TPB style survey** after the test, with a few 7 point agree or disagree questions for each factor, such as "Playing this would be fun" (attitude), "Friends would like to see my score" (norm), and "I find the controls easy" (control), plus "I intend to play again this week" (intention).
4. **Store listing check** of the IDT attributes. Make sure the screenshots and first line of the description show the relative advantage (no ads, offline, authentic look) at a glance.

## 9. References

[1] Nielsen Norman Group, "Heuristic Evaluation of User Interfaces" (video and overview of the 10 heuristics). https://www.nngroup.com/videos/heuristic-evaluation/ . The full list is at https://www.nngroup.com/articles/ten-usability-heuristics/

[2] WebAIM, "Contrast and Color Accessibility" (WCAG 1.4.3 and 1.4.6 ratios). https://webaim.org/articles/contrast/

[3] MDN Web Docs, "Color contrast" (WCAG contrast and non text contrast). https://developer.mozilla.org/en-US/docs/Web/Accessibility/Understanding_WCAG/Perceivable/Color_contrast

[4] W3C, "Understanding Success Criterion 2.3.1, Three Flashes or Below Threshold" (WCAG 2.2). https://www.w3.org/WAI/WCAG22/Understanding/three-flashes-or-below-threshold.html

[5] Android Developers, "Make apps more accessible" (48 by 48 dp touch targets). https://developer.android.com/guide/topics/ui/accessibility/apps

[6] Android Developers, "Migrate your splash screen implementation to Android 12 and later". https://developer.android.com/guide/topics/ui/splash-screen/migrate

[7] Android Developers Blog, "Prepare your app for the resizability and orientation changes in Android 17" (February 2026). https://android-developers.googleblog.com/2026/02/prepare-your-app-for-resizability-and.html

[8] Ajzen, I. (1991). "The Theory of Planned Behavior." Organizational Behavior and Human Decision Processes, 50(2), 179 to 211. https://doi.org/10.1016/0749-5978(91)90020-T

[9] Rogers, E. M. Diffusion of Innovations (first edition 1962, 5th edition 2003, Free Press). Summary of the five attributes at https://www.legalevolution.org/2017/05/variables-determining-the-rate-of-adoption-of-innovations-008/

[10] Barney, J. (1991). "Firm Resources and Sustained Competitive Advantage." Journal of Management, 17(1), 99 to 120. The 1991 paper used the VRIN criteria, and the "Organisation" question was added in Barney's later work (1995). Overview at https://en.wikipedia.org/wiki/VRIO

[11] Microsoft Learn, ".NET MAUI Share". https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/data/share

## 10. Addendum. Audit of spec version 2.0

Version 2.0 adds a banner ad, a Remove ads purchase, Google Play Games, Nearby Battle and modern rules. The VRIO rows in section 6 that rejected ads and online leaderboards are replaced by the business need for income, see `MarketStrategy.md` version 2. Decisions D1 to D16 still apply. D17 is replaced by the decisions below.

### 10.1 Findings

| ID | Severity | Finding | Fix in spec 2.0 |
|---|---|---|---|
| UX 17 | High | A banner right under the arena or keypad invites accidental taps. AdMob names banners near app elements and beside game play screens as a main cause of accidental clicks [12] | Reserved ad strip, 24 dp gap with a divider, ignored touch zone, D pad at least 48 dp above the strip (spec 9.1) |
| UX 18 | High | If the banner loads late, the layout jumps and the arena moves under the player's finger | Strip height reserved from the start using the adaptive banner height (spec 9.1) |
| UX 19 | High | Players who paid to remove ads and then see ads again (after an update, a reinstall or offline) feel cheated | Restore on every launch and resume, local cache, ads return only after a successful query without the product (spec 9.2) |
| UX 20 | Medium | A consent form before the first run slows the first play | Show the form only where needed, then go straight into the first run (spec 7) |
| UX 21 | Medium | Remove ads offers can turn into nagging | Fixed entry points and at most once per session on Game over. Never a pop up during play (spec 9.2) |
| UX 22 | Medium | An empty ad strip looks broken, and a tappable house ad near controls has the same accidental tap problem | Quiet house message that does nothing during play, a button only in menus (spec 9.1) |
| UX 23 | Medium | Many new systems (power ups, combo, missions) can overload new players | Power ups start appearing after 12 foods, one timed power up at a time, the HUD shows one ring and one bar. Missions only on Home |
| UX 24 | Medium | Power ups and battle snakes told apart only by colour fail colour blind players | Unique shapes for power ups and unique body patterns for battle snakes (spec 4.4 and 5.2) |
| UX 25 | Medium | Nearby permission prompts at first launch look suspicious | Asked only when Nearby Battle is opened, after a plain screen that says why (spec 5.2) |
| UX 26 | Medium | Sign in failures could block play | Play Games sign in is automatic and the game works fully without it (spec 10) |
| UX 27 | Medium | Shake, particles and glow can bother some players | Follow the Android "Remove animations" setting, plus an in game override (spec 11) |
| UX 28 | Low | "Addictive" design can slide into dark patterns | No energy, no loot boxes, no guilt on lost streaks, optional break reminder (spec 6) |
| UX 29 | Low | Small phones with a D pad leave too little room for the arena | D pad only when cells stay at least 10 dp, otherwise suggest landscape (spec 8) |
| UX 30 | Low | Players do not know who to beat next | "Next rival" scores just above and below on results (spec 5.1) |

### 10.2 Death effect check

Version 1.2 used a 3 times blink. Version 2.0 uses a 600 ms shake and fade with no flashing, so the WCAG three flashes rule [4] is met with room to spare.

### 10.3 Extra references

[12] AdMob Help, "Banner ad implementation guidance". https://support.google.com/admob/answer/6275345
