using TheSnake.Core;

namespace TheSnake.Core.Tests;

[TestClass]
public sealed class ChallengeCodeTests
{
    [TestMethod]
    public void RoundTripsEveryRetroLevelAndVariant()
    {
        foreach (var variant in Enum.GetValues<RetroVariant>())
        {
            for (int level = 1; level <= 9; level++)
            {
                var setup = Arenas.Retro(123456, level, variant);
                string code = ChallengeCode.Encode(setup);
                Assert.AreEqual(8, code.Length);
                Assert.IsTrue(ChallengeCode.TryDecode(code, out var decoded), code);
                Assert.AreEqual(setup.Seed, decoded.Seed);
                Assert.AreEqual(level, decoded.RetroLevel);
                Assert.AreEqual(variant, decoded.Variant);
                Assert.AreEqual(RuleSet.Retro, decoded.Rules);
            }
        }
    }

    [TestMethod]
    public void RoundTripsModernBoards()
    {
        var daily = Arenas.DailyRun(new DateOnly(2026, 10, 22));
        Assert.IsTrue(ChallengeCode.TryDecode(ChallengeCode.Encode(daily), out var d));
        Assert.AreEqual(daily.Seed, d.Seed);
        Assert.IsFalse(d.WeeklyObstacles);

        var weekly = Arenas.WeeklyArena(new DateOnly(2026, 10, 22));
        Assert.IsTrue(ChallengeCode.TryDecode(ChallengeCode.Encode(weekly), out var w));
        Assert.IsTrue(w.WeeklyObstacles);
        CollectionAssert.AreEquivalent(weekly.Obstacles.ToList(), w.Obstacles.ToList());
    }

    [TestMethod]
    public void RejectsAWrongCharacter()
    {
        string code = ChallengeCode.Encode(Arenas.DailyRun(new DateOnly(2026, 10, 22)));
        int rejected = 0;
        for (int i = 0; i < code.Length; i++)
        {
            char replacement = code[i] == 'A' ? 'B' : 'A';
            string broken = code[..i] + replacement + code[(i + 1)..];
            if (!ChallengeCode.TryDecode(broken, out _)) rejected++;
        }

        // A 6 bit check misses about 1 in 64 random changes. Most single changes must fail.
        Assert.IsTrue(rejected >= 6, $"only {rejected} of 8 broken codes were rejected");
        Assert.IsFalse(ChallengeCode.TryDecode("SHORT", out _));
        Assert.IsFalse(ChallengeCode.TryDecode("UUUUUUUU", out _));
    }

    [TestMethod]
    public void AcceptsLowerCaseAndLookalikes()
    {
        string code = ChallengeCode.Encode(Arenas.Retro(99, 3, RetroVariant.Box));
        Assert.IsTrue(ChallengeCode.TryDecode(code.ToLowerInvariant(), out _));
        Assert.IsTrue(ChallengeCode.TryDecode(code[..4] + "-" + code[4..], out _));
    }
}

[TestClass]
public sealed class SeedTests
{
    [TestMethod]
    public void DailySeedIsStablePerDayAndChangesNextDay()
    {
        var day = new DateOnly(2026, 12, 31);
        Assert.AreEqual(Seeds.Daily(day), Seeds.Daily(day));
        Assert.AreNotEqual(Seeds.Daily(day), Seeds.Daily(day.AddDays(1)));
        Assert.IsTrue(Seeds.Daily(day) <= Seeds.SeedMask);
    }

    [TestMethod]
    public void WeeklySeedIsTheSameAcrossTheIsoWeek()
    {
        var monday = new DateOnly(2026, 10, 19);
        Assert.AreEqual(Seeds.Weekly(monday), Seeds.Weekly(monday.AddDays(6)));
        Assert.AreNotEqual(Seeds.Weekly(monday), Seeds.Weekly(monday.AddDays(7)));
        Assert.AreEqual("2026-W43", Seeds.WeekKey(monday));
    }
}

[TestClass]
public sealed class ProgressionTests
{
    private static readonly DateOnly Day = new(2026, 10, 22);

    [TestMethod]
    public void LevelsNeedTenPercentMoreEachTime()
    {
        Assert.AreEqual(100, Progression.XpForNextLevel(1));
        Assert.AreEqual(110, Progression.XpForNextLevel(2));
        Assert.AreEqual(1, Progression.LevelForXp(99));
        Assert.AreEqual(2, Progression.LevelForXp(100));
        Assert.AreEqual(3, Progression.LevelForXp(210));
        Assert.AreEqual(Progression.MaxLevel, Progression.LevelForXp(int.MaxValue));
    }

    [TestMethod]
    public void MissionsAreTheSameForEveryoneOnADay()
    {
        var a = new PlayerProgress();
        var b = new PlayerProgress();
        Progression.EnsureMissions(a, Day);
        Progression.EnsureMissions(b, Day);
        Assert.AreEqual(3, a.Missions.Count);
        CollectionAssert.AreEqual(a.Missions.Select(m => m.Text).ToList(), b.Missions.Select(m => m.Text).ToList());
        Assert.AreEqual(3, a.Missions.Select(m => m.Kind).Distinct().Count());
    }

    [TestMethod]
    public void RunGivesXpBestAndMissionProgress()
    {
        var progress = new PlayerProgress();
        var reward = Progression.ApplyRun(progress, new RunSummary(GameMode.Endless, "endless", 1500, 70, 5, 3, 6), Day);
        Assert.IsTrue(reward.XpGained >= 150);
        Assert.IsTrue(reward.NewBest);
        Assert.AreEqual(1500, progress.Best["endless"]);
        Assert.IsTrue(progress.Missions.All(m => m.Done), "a big run completes every mission tier");
        Assert.IsTrue(reward.NewLevel > reward.OldLevel);
    }

    [TestMethod]
    public void OnlyFirstDailyRunIsRankedAndStreakGrows()
    {
        var progress = new PlayerProgress();
        var first = Progression.ApplyRun(progress, new RunSummary(GameMode.DailyRun, "daily", 300, 10, 2, 0, 0), Day);
        var second = Progression.ApplyRun(progress, new RunSummary(GameMode.DailyRun, "daily", 900, 30, 2, 0, 0), Day);
        Assert.IsTrue(first.RankedDaily);
        Assert.IsFalse(second.RankedDaily);
        Assert.AreEqual(300, progress.DailyScoreToday);
        Assert.AreEqual(1, progress.StreakDays);

        Progression.ApplyRun(progress, new RunSummary(GameMode.DailyRun, "daily", 10, 1, 1, 0, 0), Day.AddDays(1));
        Assert.AreEqual(2, progress.StreakDays);
        Progression.ApplyRun(progress, new RunSummary(GameMode.DailyRun, "daily", 10, 1, 1, 0, 0), Day.AddDays(5));
        Assert.AreEqual(1, progress.StreakDays, "missed days reset the streak");
    }

    [TestMethod]
    public void SevenDayStreakEarnsAFreezeThatSavesAMissedDay()
    {
        var progress = new PlayerProgress();
        for (int i = 0; i < 7; i++)
        {
            Progression.ApplyRun(progress, new RunSummary(GameMode.DailyRun, "daily", 10, 1, 1, 0, 0), Day.AddDays(i));
        }

        Assert.AreEqual(7, progress.StreakDays);
        Assert.AreEqual(1, progress.StreakFreezes);
        Progression.ApplyRun(progress, new RunSummary(GameMode.DailyRun, "daily", 10, 1, 1, 0, 0), Day.AddDays(8));
        Assert.AreEqual(8, progress.StreakDays);
        Assert.AreEqual(0, progress.StreakFreezes);
    }

    [TestMethod]
    public void ThemesUnlockByLevel()
    {
        CollectionAssert.Contains(Progression.UnlockedThemes(1).ToList(), "Neon LCD");
        CollectionAssert.DoesNotContain(Progression.UnlockedThemes(4).ToList(), "Night");
        CollectionAssert.Contains(Progression.UnlockedThemes(5).ToList(), "Night");
    }
}
