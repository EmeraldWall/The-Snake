namespace TheSnake.Core;

/// <summary>Variants of the Retro 97 mode: two classic modes plus five mazes.</summary>
public enum RetroVariant
{
    WallsOff = 0,
    WallsOn = 1,
    Box = 2,
    Tunnel = 3,
    Mill = 4,
    Rails = 5,
    Apartment = 6,
}

/// <summary>Everything the engine needs to start a run. Arena sizes are always portrait.</summary>
public sealed record GameSetup(
    GameMode Mode,
    RuleSet Rules,
    int Width,
    int Height,
    bool Wrap,
    IReadOnlySet<GridPoint> Obstacles,
    uint Seed,
    int RetroLevel = 5,
    RetroVariant Variant = RetroVariant.WallsOff,
    bool WeeklyObstacles = false,
    int TurnQueueSize = 2)
{
    public GridPoint Start => new(Width / 2, Height / 2);
}

public static class Arenas
{
    public const int StandardWidth = 24;
    public const int StandardHeight = 36;
    public const int RetroWidth = 20;
    public const int RetroHeight = 28;
    public const int StartLength = 4;

    private static readonly IReadOnlySet<GridPoint> NoObstacles = new HashSet<GridPoint>();

    public static GameSetup Endless(uint seed, int width = StandardWidth, int height = StandardHeight) =>
        new(GameMode.Endless, RuleSet.Modern, width, height, Wrap: true, NoObstacles, seed);

    public static GameSetup DailyRun(DateOnly utcDate) =>
        new(GameMode.DailyRun, RuleSet.Modern, StandardWidth, StandardHeight, Wrap: true, NoObstacles, Seeds.Daily(utcDate));

    public static GameSetup WeeklyArena(DateOnly utcDate) => WeeklyArena(Seeds.Weekly(utcDate), GameMode.WeeklyArena);

    public static GameSetup WeeklyArena(uint seed, GameMode mode) =>
        new(mode, RuleSet.Modern, StandardWidth, StandardHeight, Wrap: true,
            WeeklyLayout(seed, StandardWidth, StandardHeight), seed, WeeklyObstacles: true);

    public static GameSetup Retro(uint seed, int level, RetroVariant variant, bool purist = false)
    {
        level = Math.Clamp(level, 1, 9);
        bool wrap = variant == RetroVariant.WallsOff;
        return new GameSetup(GameMode.Retro97, RuleSet.Retro, RetroWidth, RetroHeight, wrap,
            RetroMaze(variant, RetroWidth, RetroHeight), seed, level, variant,
            TurnQueueSize: purist ? 1 : 2);
    }

    /// <summary>Cells the snake starts on, which no layout may ever block.</summary>
    public static bool IsStartZone(GridPoint p, int width, int height)
    {
        int cx = width / 2;
        int cy = height / 2;
        return Math.Abs(p.Y - cy) <= 1 && p.X >= cx - StartLength - 2 && p.X <= cx + 6;
    }

    /// <summary>Six mirrored obstacle bars built from the weekly seed.</summary>
    public static IReadOnlySet<GridPoint> WeeklyLayout(uint seed, int width, int height)
    {
        var random = new SeedRandom(seed ^ 0xA5A5A5A5UL);
        var cells = new HashSet<GridPoint>();
        for (int i = 0; i < 3; i++)
        {
            bool horizontal = random.Next(2) == 0;
            int length = 4 + random.Next(5);
            int x = 1 + random.Next(width / 2 - 2);
            int y = 2 + random.Next(height - 4);
            for (int j = 0; j < length; j++)
            {
                var p = horizontal ? new GridPoint(x + j, y) : new GridPoint(x, y + j);
                if (p.X <= 0 || p.X >= width / 2 || p.Y <= 0 || p.Y >= height - 1)
                {
                    continue;
                }

                AddMirrored(cells, p, width);
            }
        }

        cells.RemoveWhere(p => IsStartZone(p, width, height));
        return cells;
    }

    public static IReadOnlySet<GridPoint> RetroMaze(RetroVariant variant, int width, int height)
    {
        var cells = new HashSet<GridPoint>();
        void H(int y, int x0, int x1) { for (int x = x0; x <= x1; x++) cells.Add(new GridPoint(x, y)); }
        void V(int x, int y0, int y1) { for (int y = y0; y <= y1; y++) cells.Add(new GridPoint(x, y)); }

        int w = width - 1;
        int h = height - 1;
        switch (variant)
        {
            case RetroVariant.Box:
                H(0, 0, w); H(h, 0, w); V(0, 0, h); V(w, 0, h);
                break;
            case RetroVariant.Tunnel:
                H(0, 0, w); H(h, 0, w); V(0, 0, h); V(w, 0, h);
                foreach (var y in new[] { 0, h }) for (int x = width / 2 - 2; x <= width / 2 + 1; x++) cells.Remove(new GridPoint(x, y));
                foreach (var x in new[] { 0, w }) for (int y = height / 2 - 2; y <= height / 2 + 1; y++) cells.Remove(new GridPoint(x, y));
                H(height / 4, 4, w - 4); H(h - height / 4, 4, w - 4);
                break;
            case RetroVariant.Mill:
                V(width / 2, 2, height / 2 - 4); V(width / 2 - 1, height / 2 + 4, h - 2);
                H(height / 2 - 4, 2, width / 2 - 4); H(height / 2 + 4, width / 2 + 3, w - 2);
                break;
            case RetroVariant.Rails:
                V(width / 4, 3, h - 3); V(w - width / 4, 3, h - 3);
                break;
            case RetroVariant.Apartment:
                H(0, 0, w); H(h, 0, w); V(0, 0, h); V(w, 0, h);
                for (int y = height / 4; y < h; y += height / 4)
                {
                    H(y, 1, width / 2 - 3);
                    H(y, width / 2 + 2, w - 1);
                }
                break;
        }

        cells.RemoveWhere(p => IsStartZone(p, width, height) || p.X < 0 || p.Y < 0 || p.X >= width || p.Y >= height);
        return cells;
    }

    private static void AddMirrored(HashSet<GridPoint> cells, GridPoint p, int width)
    {
        cells.Add(p);
        cells.Add(new GridPoint(width - 1 - p.X, p.Y));
    }

    public static string RetroName(RetroVariant variant) => variant switch
    {
        RetroVariant.WallsOff => "Walls off",
        RetroVariant.WallsOn => "Walls on",
        _ => variant.ToString(),
    };
}
