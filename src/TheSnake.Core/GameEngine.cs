namespace TheSnake.Core;

public sealed record PowerUpItem(PowerUpKind Kind, GridPoint Position, int RemainingMs);

public sealed record ActivePowerUp(PowerUpKind Kind, int RemainingMs, int TotalMs);

public sealed record Critter(GridPoint Left, int TicksLeft, int Shape)
{
    public GridPoint Right => new(Left.X + 1, Left.Y);

    public bool Covers(GridPoint p) => p == Left || p == Right;
}

/// <summary>
/// Pure game rules. Knows nothing about time, drawing or sound: the app calls
/// <see cref="Tick"/> every <see cref="TickLengthMs"/> and reacts to the returned events.
/// </summary>
public sealed class GameEngine
{
    public const int ModernStartTickMs = 220;
    public const int ModernTickStepMs = 3;
    public const int ModernMinTickMs = 80;
    public const int ComboWindowMs = 3000;
    public const int MaxCombo = 5;
    public const int NearMissMaxTickMs = 140;
    public const int NearMissCooldownMs = 2000;
    public const int FoodsPerPowerUp = 12;
    public const int PowerUpLifeMs = 10_000;
    public const int FoodsPerGolden = 15;
    public const int GoldenLifeMs = 6000;
    public const int FoodsPerCritter = 5;
    public const int CritterLifeTicks = 20;

    /// <summary>The highest score one tick can give: golden food at x5 combo with Double.</summary>
    public const int MaxPointsPerTick = 500;

    private static readonly int[] RetroTickMs = [400, 340, 290, 245, 205, 170, 140, 115, 95];

    private readonly IRandomSource _random;
    private readonly List<GridPoint> _snake = [];
    private readonly int[,] _occupied;
    private readonly Queue<Direction> _turns = new();
    private long _lastNearMissMs = -NearMissCooldownMs;
    private int _foodsSincePowerUp;

    public GameEngine(GameSetup setup, IRandomSource? random = null)
    {
        Setup = setup;
        _random = random ?? new SeedRandom(setup.Seed);
        _occupied = new int[setup.Width, setup.Height];

        var start = setup.Start;
        for (int i = 0; i < Arenas.StartLength; i++)
        {
            var p = new GridPoint(start.X - i, start.Y);
            _snake.Add(p);
            _occupied[p.X, p.Y]++;
        }

        Heading = Direction.Right;
        Food = SpawnCell() ?? start;
    }

    public GameSetup Setup { get; }

    /// <summary>Snake cells, head first.</summary>
    public IReadOnlyList<GridPoint> Snake => _snake;

    public GridPoint Head => _snake[0];

    /// <summary>Where the tail was before the last tick, or null if the tail did not move.</summary>
    public GridPoint? PreviousTail { get; private set; }

    /// <summary>True if the head crossed an edge on the last tick (no smooth slide for that step).</summary>
    public bool LastMoveWrapped { get; private set; }

    public Direction Heading { get; private set; }

    /// <summary>The direction of the next move: the first queued turn, or the current heading.</summary>
    public Direction NextHeading => _turns.Count > 0 ? _turns.Peek() : Heading;

    /// <summary>True when no turn is waiting to be applied.</summary>
    public bool TurnQueueEmpty => _turns.Count == 0;

    /// <summary>
    /// Where the head will go on the next tick, or null if that move would hit a wall, an obstacle or the body.
    /// Lets the renderer draw the snake moving into its next cell instead of lagging a tick behind.
    /// </summary>
    public GridPoint? PredictNextCell(out bool grows)
    {
        grows = false;
        var next = Head.Step(NextHeading);
        if (!InBounds(next))
        {
            return null; // Wrapping or a wall: the renderer does not slide across the edge.
        }

        if (IsObstacle(next))
        {
            return null;
        }

        grows = next == Food || next == Golden;
        bool tailLeaves = !grows && next == _snake[^1] && _occupied[next.X, next.Y] == 1;
        if (_occupied[next.X, next.Y] > 0 && !tailLeaves && ActivePower?.Kind != PowerUpKind.Phase)
        {
            return null;
        }

        return next;
    }

    public GridPoint Food { get; private set; }

    public GridPoint? Golden { get; private set; }

    public int GoldenRemainingMs { get; private set; }

    public PowerUpItem? PowerUp { get; private set; }

    public ActivePowerUp? ActivePower { get; private set; }

    public Critter? Critter { get; private set; }

    public int Score { get; private set; }

    public int Combo { get; private set; } = 1;

    public int MaxComboReached { get; private set; } = 1;

    public int ComboWindowLeftMs { get; private set; }

    public int FoodsEaten { get; private set; }

    public int PowerUpsTaken { get; private set; }

    public int NearMisses { get; private set; }

    public int Ticks { get; private set; }

    public long ElapsedMs { get; private set; }

    public bool IsDead { get; private set; }

    public bool IsWon { get; private set; }

    public bool IsOver => IsDead || IsWon;

    public int TickLengthMs
    {
        get
        {
            if (Setup.Rules == RuleSet.Retro)
            {
                return RetroTickMs[Math.Clamp(Setup.RetroLevel, 1, 9) - 1];
            }

            int baseMs = Math.Max(ModernMinTickMs, ModernStartTickMs - ModernTickStepMs * FoodsEaten);
            return ActivePower?.Kind == PowerUpKind.SlowTime ? (int)(baseMs * 1.6) : baseMs;
        }
    }

    public bool IsObstacle(GridPoint p) => Setup.Obstacles.Contains(p);

    public bool IsOccupiedBySnake(GridPoint p) => InBounds(p) && _occupied[p.X, p.Y] > 0;

    /// <summary>Queues an absolute turn. Reverses and repeats are ignored.</summary>
    public bool QueueTurn(Direction direction)
    {
        if (IsOver || _turns.Count >= Setup.TurnQueueSize)
        {
            return false;
        }

        var last = _turns.Count > 0 ? _turns.Last() : Heading;
        if (direction == last || direction == last.Opposite())
        {
            return false;
        }

        _turns.Enqueue(direction);
        return true;
    }

    /// <summary>Queues a turn relative to where the snake will be heading.</summary>
    public bool QueueRelativeTurn(bool right)
    {
        var last = _turns.Count > 0 ? _turns.Last() : Heading;
        return QueueTurn(right ? last.TurnRight() : last.TurnLeft());
    }

    public TickEvents Tick()
    {
        if (IsOver)
        {
            return TickEvents.None;
        }

        var events = TickEvents.None;
        int tickMs = TickLengthMs;
        Ticks++;
        ElapsedMs += tickMs;

        if (_turns.Count > 0)
        {
            Heading = _turns.Dequeue();
        }

        var next = Head.Step(Heading);
        LastMoveWrapped = false;
        if (!InBounds(next))
        {
            if (!Setup.Wrap)
            {
                return Die(events);
            }

            next = new GridPoint((next.X + Setup.Width) % Setup.Width, (next.Y + Setup.Height) % Setup.Height);
            LastMoveWrapped = true;
            events |= TickEvents.Wrapped;
        }

        if (IsObstacle(next))
        {
            return Die(events);
        }

        bool eatsFood = next == Food;
        bool eatsGolden = Golden == next;
        bool willGrow = eatsFood || eatsGolden;
        var tail = _snake[^1];
        bool tailMovesAway = !willGrow;
        bool phase = ActivePower?.Kind == PowerUpKind.Phase;
        if (!phase && _occupied[next.X, next.Y] > 0 && !(next == tail && tailMovesAway && _occupied[next.X, next.Y] == 1))
        {
            return Die(events);
        }

        // Move.
        if (!willGrow)
        {
            _snake.RemoveAt(_snake.Count - 1);
            _occupied[tail.X, tail.Y]--;
            PreviousTail = tail;
        }
        else
        {
            PreviousTail = null;
        }

        _snake.Insert(0, next);
        _occupied[next.X, next.Y]++;

        events |= AdvanceTimers(tickMs);

        if (eatsFood)
        {
            events |= EatFood();
        }

        if (eatsGolden)
        {
            Golden = null;
            AddPoints(50 * Combo * DoubleFactor);
            events |= TickEvents.AteGolden;
        }

        if (PowerUp is { } item && item.Position == next)
        {
            PowerUp = null;
            PowerUpsTaken++;
            events |= TickEvents.PowerUpTaken | ApplyPowerUp(item.Kind);
        }

        if (Critter is { } critter && critter.Covers(next))
        {
            Critter = null;
            int level = Setup.RetroLevel;
            AddPoints(Math.Max(critter.TicksLeft * level, 5 * level));
            events |= TickEvents.CritterEaten;
        }

        if (ActivePower?.Kind == PowerUpKind.Magnet)
        {
            PullFood();
        }

        if (Setup.Rules == RuleSet.Modern && tickMs <= NearMissMaxTickMs && IsNearMiss()
            && ElapsedMs - _lastNearMissMs >= NearMissCooldownMs)
        {
            _lastNearMissMs = ElapsedMs;
            NearMisses++;
            AddPoints(5);
            events |= TickEvents.NearMiss;
        }

        if (FreeCellCount() == 0)
        {
            IsWon = true;
            AddPoints(Setup.Rules == RuleSet.Retro ? 100 * Setup.RetroLevel : 1000);
            events |= TickEvents.Won;
        }

        return events;
    }

    private int DoubleFactor => ActivePower?.Kind == PowerUpKind.Double ? 2 : 1;

    private TickEvents EatFood()
    {
        var events = TickEvents.Ate;
        FoodsEaten++;
        _foodsSincePowerUp++;

        if (Setup.Rules == RuleSet.Retro)
        {
            AddPoints(Setup.RetroLevel);
            if (FoodsEaten % FoodsPerCritter == 0 && Critter is null && SpawnCritter())
            {
                events |= TickEvents.CritterSpawned;
            }
        }
        else
        {
            if (ComboWindowLeftMs > 0 && Combo < MaxCombo)
            {
                Combo++;
                MaxComboReached = Math.Max(MaxComboReached, Combo);
                events |= TickEvents.ComboUp;
            }

            ComboWindowLeftMs = ComboWindowMs;
            AddPoints(10 * Combo * DoubleFactor);

            if (FoodsEaten % FoodsPerGolden == 0 && Golden is null && SpawnCell() is { } goldenCell)
            {
                Golden = goldenCell;
                GoldenRemainingMs = GoldenLifeMs;
                events |= TickEvents.GoldenSpawned;
            }

            if (_foodsSincePowerUp >= FoodsPerPowerUp && PowerUp is null && SpawnCell() is { } powerCell)
            {
                _foodsSincePowerUp = 0;
                var kind = (PowerUpKind)_random.Next(Enum.GetValues<PowerUpKind>().Length);
                PowerUp = new PowerUpItem(kind, powerCell, PowerUpLifeMs);
                events |= TickEvents.PowerUpSpawned;
            }
        }

        if (SpawnCell() is { } foodCell)
        {
            Food = foodCell;
        }

        return events;
    }

    private TickEvents ApplyPowerUp(PowerUpKind kind)
    {
        if (kind == PowerUpKind.Trim)
        {
            int keep = Math.Max(Arenas.StartLength, (int)Math.Ceiling(_snake.Count * 0.7));
            while (_snake.Count > keep)
            {
                var t = _snake[^1];
                _snake.RemoveAt(_snake.Count - 1);
                _occupied[t.X, t.Y]--;
            }

            PreviousTail = null;
            return TickEvents.None;
        }

        int duration = kind switch
        {
            PowerUpKind.SlowTime => 6000,
            PowerUpKind.Phase => 5000,
            PowerUpKind.Magnet => 8000,
            _ => 10_000,
        };
        ActivePower = new ActivePowerUp(kind, duration, duration);
        return TickEvents.None;
    }

    private TickEvents AdvanceTimers(int tickMs)
    {
        var events = TickEvents.None;
        if (ComboWindowLeftMs > 0)
        {
            ComboWindowLeftMs = Math.Max(0, ComboWindowLeftMs - tickMs);
            if (ComboWindowLeftMs == 0)
            {
                Combo = 1;
            }
        }

        if (Golden is not null)
        {
            GoldenRemainingMs -= tickMs;
            if (GoldenRemainingMs <= 0)
            {
                Golden = null;
            }
        }

        if (PowerUp is { } item)
        {
            int left = item.RemainingMs - tickMs;
            PowerUp = left > 0 ? item with { RemainingMs = left } : null;
        }

        if (ActivePower is { } active)
        {
            int left = active.RemainingMs - tickMs;
            if (left > 0)
            {
                ActivePower = active with { RemainingMs = left };
            }
            else
            {
                ActivePower = null;
                events |= TickEvents.PowerUpEnded;
            }
        }

        if (Critter is { } critter)
        {
            Critter = critter.TicksLeft > 1 ? critter with { TicksLeft = critter.TicksLeft - 1 } : null;
        }

        return events;
    }

    private void PullFood()
    {
        if (Food.ManhattanTo(Head) > 3 || Food == Head)
        {
            return;
        }

        int dx = Math.Sign(Head.X - Food.X);
        int dy = Math.Sign(Head.Y - Food.Y);
        var candidate = Math.Abs(Head.X - Food.X) >= Math.Abs(Head.Y - Food.Y)
            ? new GridPoint(Food.X + dx, Food.Y)
            : new GridPoint(Food.X, Food.Y + dy);
        if (candidate == Head)
        {
            return;
        }

        if (InBounds(candidate) && !IsObstacle(candidate) && _occupied[candidate.X, candidate.Y] == 0)
        {
            Food = candidate;
        }
    }

    private bool IsNearMiss()
    {
        var neck = _snake.Count > 1 ? _snake[1] : Head;
        foreach (var d in Enum.GetValues<Direction>())
        {
            var p = Head.Step(d);
            if (p == neck)
            {
                continue;
            }

            if (!InBounds(p))
            {
                if (!Setup.Wrap)
                {
                    return true;
                }

                continue;
            }

            if (IsObstacle(p) || _occupied[p.X, p.Y] > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool SpawnCritter()
    {
        var spots = new List<GridPoint>();
        for (int y = 0; y < Setup.Height; y++)
        {
            for (int x = 0; x < Setup.Width - 1; x++)
            {
                var a = new GridPoint(x, y);
                var b = new GridPoint(x + 1, y);
                if (IsFree(a) && IsFree(b))
                {
                    spots.Add(a);
                }
            }
        }

        if (spots.Count == 0)
        {
            return false;
        }

        Critter = new Critter(spots[_random.Next(spots.Count)], CritterLifeTicks, _random.Next(6));
        return true;
    }

    private GridPoint? SpawnCell()
    {
        int free = 0;
        for (int y = 0; y < Setup.Height; y++)
        {
            for (int x = 0; x < Setup.Width; x++)
            {
                if (IsFree(new GridPoint(x, y)))
                {
                    free++;
                }
            }
        }

        if (free == 0)
        {
            return null;
        }

        int pick = _random.Next(free);
        for (int y = 0; y < Setup.Height; y++)
        {
            for (int x = 0; x < Setup.Width; x++)
            {
                var p = new GridPoint(x, y);
                if (IsFree(p) && pick-- == 0)
                {
                    return p;
                }
            }
        }

        return null;
    }

    private bool IsFree(GridPoint p) =>
        _occupied[p.X, p.Y] == 0 && !IsObstacle(p) && p != Food && p != Golden
        && PowerUp?.Position != p && !(Critter?.Covers(p) ?? false);

    private int FreeCellCount()
    {
        int count = 0;
        for (int y = 0; y < Setup.Height; y++)
        {
            for (int x = 0; x < Setup.Width; x++)
            {
                if (_occupied[x, y] == 0 && !IsObstacle(new GridPoint(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private bool InBounds(GridPoint p) => p.X >= 0 && p.Y >= 0 && p.X < Setup.Width && p.Y < Setup.Height;

    private void AddPoints(int points) => Score = (int)Math.Min(int.MaxValue, (long)Score + points);

    private TickEvents Die(TickEvents events)
    {
        IsDead = true;
        PreviousTail = null;
        return events | TickEvents.Died;
    }
}
