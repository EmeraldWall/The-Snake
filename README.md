# The Snake

A modern, Nokia inspired Snake game for Android, built with .NET MAUI in Visual Studio.
Package name `com.vishvplus.thesnakegame`.

## Documents

* [docs/SnakeGameSpecification.md](docs/SnakeGameSpecification.md) is the full game specification (version 2.0, Android 17, .NET MAUI, Visual Studio, ads, Remove ads purchase, Google Play Games).
* [docs/UIUXAudit.md](docs/UIUXAudit.md) is the UI and UX audit and the TPB, Innovation Diffusion Theory and VRIO reasoning behind the design choices.
* [docs/MarketStrategy.md](docs/MarketStrategy.md) covers points of parity and difference and the Blue Ocean strategy.
* [docs/BuildStatus.md](docs/BuildStatus.md) says what build 0.1.0 contains and what is still to come.

## Projects

| Project | What it is |
|---|---|
| `src/TheSnake.Core` | Pure C# game rules, seeds, challenge codes and progression. No MAUI or Android code |
| `src/TheSnake` | The .NET MAUI Android app: screens, rendering, controls, ads and billing |
| `tests/TheSnake.Core.Tests` | MSTest unit tests for the Core project |

## Build in Visual Studio 2026

1. Install the ".NET Multi platform App UI development" workload.
2. Open `TheSnake.sln`, set `TheSnake` as the startup project and pick an Android emulator or phone.
3. Press F5 to run. Run the tests from Test Explorer.
4. For a release, right click `TheSnake`, choose **Publish**, and sign the AAB with your own upload key in the Archive Manager.

## Build from the command line

```
dotnet test tests/TheSnake.Core.Tests
dotnet publish src/TheSnake -f net10.0-android -c Release -p:AndroidPackageFormats=apk
```

## Before a Play Store release

* Replace the AdMob test IDs in `src/TheSnake/Services/Monetisation.cs` and `src/TheSnake/Platforms/Android/AndroidManifest.xml` with your real ones.
* Create the one time product `remove_ads` in Play Console.
* Sign with your own upload key, not the debug key.
* Set up Play Games Services in Play Console, create four leaderboards (Endless, Daily Run, Weekly Arena, Retro 97), and put the project ID and leaderboard IDs in `src/TheSnake/Platforms/Android/Resources/values/games.xml`.
