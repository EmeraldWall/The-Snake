namespace TheSnake.Core;

/// <summary>A cell on the arena grid. X grows to the right, Y grows downwards.</summary>
public readonly record struct GridPoint(int X, int Y)
{
    public GridPoint Step(Direction direction)
    {
        var (dx, dy) = direction.Delta();
        return new GridPoint(X + dx, Y + dy);
    }

    public int ManhattanTo(GridPoint other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
}

/// <summary>Compass directions in clockwise order, so adding 1 turns right.</summary>
public enum Direction
{
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3,
}

public static class DirectionExtensions
{
    public static (int Dx, int Dy) Delta(this Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Right => (1, 0),
        Direction.Down => (0, 1),
        _ => (-1, 0),
    };

    public static Direction Opposite(this Direction direction) => (Direction)(((int)direction + 2) % 4);

    public static Direction TurnRight(this Direction direction) => (Direction)(((int)direction + 1) % 4);

    public static Direction TurnLeft(this Direction direction) => (Direction)(((int)direction + 3) % 4);

    /// <summary>
    /// Maps a direction pressed on a landscape screen to the engine's portrait grid.
    /// The renderer shows portrait cell (x, y) at landscape cell (y, width - 1 - x).
    /// </summary>
    public static Direction FromLandscapeScreen(this Direction screenDirection) => screenDirection.TurnRight();
}

public enum RuleSet
{
    /// <summary>Speed ramp, combos, near misses, golden food and power ups.</summary>
    Modern,

    /// <summary>The 1990s phone rules. Fixed speed per level and critters.</summary>
    Retro,
}

public enum GameMode
{
    Endless,
    DailyRun,
    WeeklyArena,
    Challenge,
    Retro97,
}

public enum PowerUpKind
{
    SlowTime,
    Phase,
    Magnet,
    Double,
    Trim,
}

/// <summary>Things that happened during one tick, so the app can play sounds and effects.</summary>
[Flags]
public enum TickEvents
{
    None = 0,
    Ate = 1 << 0,
    AteGolden = 1 << 1,
    ComboUp = 1 << 2,
    NearMiss = 1 << 3,
    PowerUpSpawned = 1 << 4,
    PowerUpTaken = 1 << 5,
    PowerUpEnded = 1 << 6,
    GoldenSpawned = 1 << 7,
    CritterSpawned = 1 << 8,
    CritterEaten = 1 << 9,
    Wrapped = 1 << 10,
    Died = 1 << 11,
    Won = 1 << 12,
}
