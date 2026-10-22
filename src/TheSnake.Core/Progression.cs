namespace TheSnake.Core;

public enum MissionKind
{
    EatFood,
    ReachCombo,
    UsePowerUps,
    ScoreInOneRun,
    PlayRuns,
    NearMisses,
}

public sealed class Mission
{
    public MissionKind Kind { get; set; }

    public int Target { get; set; }

    public int Progress { get; set; }

    public int RewardXp { get; set; }

    public bool Done => Progress >= Target;

    public string Text => Kind switch
    {
        MissionKind.EatFood => $"Eat {Target} food",
        MissionKind.ReachCombo => $"Reach combo x{Target}",
        MissionKind.UsePowerUps => $"Use {Target} power ups",
        MissionKind.ScoreInOneRun => $"Score {Target} in one run",
        MissionKind.PlayRuns => $"Play {Target} runs",
        _ => $"Get {Target} near misses",
    };
}

/// <summary>Everything the player has earned. Saved as JSON.</summary>
public sealed class PlayerProgress
{
    public int Xp { get; set; }

    public Dictionary<string, int> Best { get; set; } = [];

    public string MissionsDate { get; set; } = "";

    public List<Mission> Missions { get; set; } = [];

    public bool RerollUsed { get; set; }

    public string LastDailyDate { get; set; } = "";

    public int DailyScoreToday { get; set; }

    public int StreakDays { get; set; }

    public int StreakFreezes { get; set; }

    public int TotalRuns { get; set; }

    public int TotalFoods { get; set; }
}

public sealed record RunSummary(GameMode Mode, string BestKey, int Score, int FoodsEaten, int MaxCombo, int PowerUpsTaken, int NearMisses);

public sealed record RunReward(
    int XpGained,
    int OldLevel,
    int NewLevel,
    bool NewBest,
    int PreviousBest,
    IReadOnlyList<Mission> MissionsCompleted,
    IReadOnlyList<string> Unlocked,
    bool RankedDaily);

public static class Progression
{
    public const int MaxLevel = 50;

    /// <summary>Themes and the player level that unlocks each one. All earned by playing, never sold.</summary>
    public static readonly IReadOnlyList<(string Theme, int Level)> ThemeUnlocks =
    [
        ("Neon LCD", 1), ("Classic LCD", 1), ("Blue Backlight", 3), ("Night", 5), ("Sunset", 8),
        ("Ocean", 12), ("Candy", 16), ("Mono", 20), ("Ember", 25), ("Aurora", 30),
    ];

    /// <summary>XP needed to go from level n to n + 1. Each level needs 10 percent more.</summary>
    public static int XpForNextLevel(int level) => (int)Math.Round(100 * Math.Pow(1.1, level - 1));

    public static int LevelForXp(int xp)
    {
        int level = 1;
        while (level < MaxLevel && xp >= XpForNextLevel(level))
        {
            xp -= XpForNextLevel(level);
            level++;
        }

        return level;
    }

    /// <summary>Progress through the current level, from 0 to 1.</summary>
    public static double LevelFraction(int xp)
    {
        int level = 1;
        while (level < MaxLevel && xp >= XpForNextLevel(level))
        {
            xp -= XpForNextLevel(level);
            level++;
        }

        return level >= MaxLevel ? 1 : (double)xp / XpForNextLevel(level);
    }

    public static IEnumerable<string> UnlockedThemes(int level) =>
        ThemeUnlocks.Where(t => t.Level <= level).Select(t => t.Theme);

    /// <summary>Makes sure today's three missions exist. They are the same for every player on a UTC day.</summary>
    public static void EnsureMissions(PlayerProgress progress, DateOnly utcToday)
    {
        string key = utcToday.ToString("yyyy-MM-dd");
        if (progress.MissionsDate == key && progress.Missions.Count == 3)
        {
            return;
        }

        progress.MissionsDate = key;
        progress.RerollUsed = false;
        progress.Missions = PickMissions(new SeedRandom(Seeds.Daily(utcToday) ^ 0x5EED), 3);
    }

    public static bool Reroll(PlayerProgress progress, int index, DateOnly utcToday)
    {
        EnsureMissions(progress, utcToday);
        if (progress.RerollUsed || index < 0 || index >= progress.Missions.Count || progress.Missions[index].Done)
        {
            return false;
        }

        var random = new SeedRandom((ulong)DateTime.UtcNow.Ticks);
        Mission replacement;
        do
        {
            replacement = PickMissions(random, 1)[0];
        }
        while (progress.Missions.Any(m => m.Kind == replacement.Kind));

        progress.Missions[index] = replacement;
        progress.RerollUsed = true;
        return true;
    }

    public static RunReward ApplyRun(PlayerProgress progress, RunSummary run, DateOnly utcToday)
    {
        EnsureMissions(progress, utcToday);
        int oldLevel = LevelForXp(progress.Xp);
        int xp = run.Score / 10;

        progress.TotalRuns++;
        progress.TotalFoods += run.FoodsEaten;

        var completed = new List<Mission>();
        foreach (var mission in progress.Missions.Where(m => !m.Done))
        {
            mission.Progress = mission.Kind switch
            {
                MissionKind.EatFood => mission.Progress + run.FoodsEaten,
                MissionKind.ReachCombo => Math.Max(mission.Progress, run.MaxCombo),
                MissionKind.UsePowerUps => mission.Progress + run.PowerUpsTaken,
                MissionKind.ScoreInOneRun => Math.Max(mission.Progress, run.Score),
                MissionKind.PlayRuns => mission.Progress + 1,
                _ => mission.Progress + run.NearMisses,
            };

            if (mission.Done)
            {
                mission.Progress = mission.Target;
                xp += mission.RewardXp;
                completed.Add(mission);
            }
        }

        bool rankedDaily = false;
        if (run.Mode == GameMode.DailyRun)
        {
            string today = utcToday.ToString("yyyy-MM-dd");
            if (progress.LastDailyDate != today)
            {
                rankedDaily = true;
                UpdateStreak(progress, utcToday);
                progress.LastDailyDate = today;
                progress.DailyScoreToday = run.Score;
            }
        }

        progress.Best.TryGetValue(run.BestKey, out int previousBest);
        bool newBest = run.Score > previousBest;
        if (newBest)
        {
            progress.Best[run.BestKey] = run.Score;
        }

        progress.Xp = (int)Math.Min(int.MaxValue, (long)progress.Xp + xp);
        int newLevel = LevelForXp(progress.Xp);
        var unlocked = ThemeUnlocks.Where(t => t.Level > oldLevel && t.Level <= newLevel).Select(t => t.Theme).ToList();

        return new RunReward(xp, oldLevel, newLevel, newBest, previousBest, completed, unlocked, rankedDaily);
    }

    public static bool HasPlayedDailyToday(PlayerProgress progress, DateOnly utcToday) =>
        progress.LastDailyDate == utcToday.ToString("yyyy-MM-dd");

    private static void UpdateStreak(PlayerProgress progress, DateOnly today)
    {
        if (DateOnly.TryParse(progress.LastDailyDate, out var last))
        {
            int gap = today.DayNumber - last.DayNumber;
            if (gap == 1)
            {
                progress.StreakDays++;
            }
            else if (gap == 2 && progress.StreakFreezes > 0)
            {
                progress.StreakFreezes--;
                progress.StreakDays++;
            }
            else if (gap > 1)
            {
                progress.StreakDays = 1;
            }
        }
        else
        {
            progress.StreakDays = 1;
        }

        if (progress.StreakDays > 0 && progress.StreakDays % 7 == 0)
        {
            progress.StreakFreezes = Math.Min(2, progress.StreakFreezes + 1);
        }
    }

    private static List<Mission> PickMissions(SeedRandom random, int count)
    {
        var kinds = Enum.GetValues<MissionKind>().ToList();
        var result = new List<Mission>();
        while (result.Count < count && kinds.Count > 0)
        {
            var kind = kinds[random.Next(kinds.Count)];
            kinds.Remove(kind);
            int tier = random.Next(3);
            int target = kind switch
            {
                MissionKind.EatFood => new[] { 25, 40, 60 }[tier],
                MissionKind.ReachCombo => new[] { 3, 4, 5 }[tier],
                MissionKind.UsePowerUps => new[] { 1, 2, 3 }[tier],
                MissionKind.ScoreInOneRun => new[] { 300, 600, 1000 }[tier],
                MissionKind.PlayRuns => new[] { 3, 5, 8 }[tier],
                _ => new[] { 2, 4, 6 }[tier],
            };
            result.Add(new Mission { Kind = kind, Target = target, RewardXp = 50 + 50 * tier });
        }

        return result;
    }
}
