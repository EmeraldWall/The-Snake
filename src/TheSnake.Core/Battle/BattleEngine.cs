namespace TheSnake.Core.Battle;

public enum BattleStatus : byte
{
    Running = 0,
    Over = 1,
}

/// <summary>One player's snake in a battle.</summary>
public sealed class BattleSnake
{
    internal readonly Queue<Direction> Turns = new();

    public BattleSnake(int index, IEnumerable<GridPoint> body, Direction heading)
    {
        Index = index;
        Body = [.. body];
        Heading = heading;
    }

    public int Index { get; }

    /// <summary>Cells, head first.</summary>
    public List<GridPoint> Body { get; }

    public Direction Heading { get; internal set; }

    public bool Alive { get; internal set; } = true;

    /// <summary>The player left the match. The snake stops and stays on the board as an obstacle.</summary>
    public bool Dropped { get; internal set; }

    public int Kills { get; internal set; }

    public int Length => Body.Count;

    public Direction NextHeading => Turns.Count > 0 ? Turns.Peek() : Heading;
}

/// <summary>
/// Real time battle rules for 2 to 4 snakes on the Standard arena (spec 5.2). Pure logic, run only by the
/// host phone. All snakes move at once each tick. Running into any body kills you, head on crashes kill both,
/// and the last snake alive wins, or the longest one when the 2 minutes are up.
/// </summary>
public sealed class BattleEngine
{
    public const int TickMs = 150;
    public const int DurationMs = 120_000;
    public const int FoodCount = 3;
    public const int MaxPlayers = 4;

    private readonly IRandomSource _random;
    private readonly List<GridPoint> _foods = [];

    public BattleEngine(int players, uint seed, int width = Arenas.StandardWidth, int height = Arenas.StandardHeight)
    {
        if (players is < 1 or > MaxPlayers)
        {
            throw new ArgumentOutOfRangeException(nameof(players));
        }

        Width = width;
        Height = height;
        Seed = seed;
        _random = new SeedRandom(seed);
        for (int i = 0; i < players; i++)
        {
            Snakes.Add(StartSnake(i));
        }

        for (int i = 0; i < FoodCount; i++)
        {
            SpawnFood();
        }
    }

    public int Width { get; }

    public int Height { get; }

    public uint Seed { get; }

    public List<BattleSnake> Snakes { get; } = [];

    public IReadOnlyList<GridPoint> Foods => _foods;

    public int Ticks { get; private set; }

    public int ElapsedMs { get; private set; }

    public BattleStatus Status { get; private set; }

    /// <summary>Index of the winner, or -1 for a draw (or while running).</summary>
    public int Winner { get; private set; } = -1;

    public int RemainingMs => Math.Max(0, DurationMs - ElapsedMs);

    public bool QueueTurn(int player, Direction direction)
    {
        if (player < 0 || player >= Snakes.Count)
        {
            return false;
        }

        var snake = Snakes[player];
        if (!snake.Alive || snake.Turns.Count >= 2)
        {
            return false;
        }

        var last = snake.Turns.Count > 0 ? snake.Turns.Last() : snake.Heading;
        if (direction == last || direction == last.Opposite())
        {
            return false;
        }

        snake.Turns.Enqueue(direction);
        return true;
    }

    /// <summary>A player left. Their snake freezes and stays as an obstacle.</summary>
    public void Drop(int player)
    {
        if (player >= 0 && player < Snakes.Count && Snakes[player].Alive)
        {
            Snakes[player].Alive = false;
            Snakes[player].Dropped = true;
            CheckEnd();
        }
    }

    public void Tick()
    {
        if (Status == BattleStatus.Over)
        {
            return;
        }

        Ticks++;
        ElapsedMs += TickMs;
        var movers = Snakes.Where(s => s.Alive).ToList();

        // 1. Where does each live snake go?
        var next = new Dictionary<BattleSnake, GridPoint>();
        foreach (var s in movers)
        {
            if (s.Turns.Count > 0)
            {
                s.Heading = s.Turns.Dequeue();
            }

            var p = s.Body[0].Step(s.Heading);
            next[s] = new GridPoint((p.X + Width) % Width, (p.Y + Height) % Height);
        }

        var grows = movers.ToDictionary(s => s, s => _foods.Contains(next[s]));

        // 2. Occupied cells once the tails of non growing snakes have moved on.
        var occupied = new Dictionary<GridPoint, BattleSnake>();
        foreach (var s in Snakes)
        {
            int count = s.Alive && !grows.GetValueOrDefault(s) ? s.Body.Count - 1 : s.Body.Count;
            for (int i = 0; i < count; i++)
            {
                occupied[s.Body[i]] = s;
            }
        }

        // 3. Collisions: bodies, then head on crashes into the same cell.
        var dying = new HashSet<BattleSnake>();
        foreach (var s in movers)
        {
            if (occupied.TryGetValue(next[s], out var owner))
            {
                dying.Add(s);
                if (owner != s)
                {
                    owner.Kills++;
                }
            }
        }

        foreach (var group in movers.GroupBy(s => next[s]).Where(g => g.Count() > 1))
        {
            foreach (var s in group)
            {
                dying.Add(s);
            }
        }

        // 4. Move the survivors.
        foreach (var s in movers)
        {
            if (dying.Contains(s))
            {
                s.Alive = false;
                s.Body.Clear();
                continue;
            }

            s.Body.Insert(0, next[s]);
            if (grows[s])
            {
                _foods.Remove(next[s]);
            }
            else
            {
                s.Body.RemoveAt(s.Body.Count - 1);
            }
        }

        while (_foods.Count < FoodCount && SpawnFood())
        {
        }

        CheckEnd();
    }

    private void CheckEnd()
    {
        var alive = Snakes.Where(s => s.Alive).ToList();
        bool timeUp = ElapsedMs >= DurationMs;
        if (Snakes.Count > 1 && alive.Count <= 1)
        {
            Status = BattleStatus.Over;
            Winner = alive.Count == 1 ? alive[0].Index : -1;
        }
        else if (Snakes.Count == 1 && alive.Count == 0)
        {
            Status = BattleStatus.Over;
            Winner = -1;
        }
        else if (timeUp)
        {
            Status = BattleStatus.Over;
            int best = alive.Max(s => s.Length);
            var leaders = alive.Where(s => s.Length == best).ToList();
            Winner = leaders.Count == 1 ? leaders[0].Index : -1;
        }
    }

    /// <summary>Start corners, each 4 long, all facing into the arena.</summary>
    private BattleSnake StartSnake(int index)
    {
        int left = 6;
        int right = Width - 7;
        int top = 6;
        int bottom = Height - 7;
        var (head, heading) = index switch
        {
            0 => (new GridPoint(left, top), Direction.Right),
            1 => (new GridPoint(right, bottom), Direction.Left),
            2 => (new GridPoint(right, top), Direction.Down),
            _ => (new GridPoint(left, bottom), Direction.Up),
        };

        var back = heading.Opposite();
        var body = new List<GridPoint> { head };
        for (int i = 1; i < Arenas.StartLength; i++)
        {
            body.Add(body[^1].Step(back));
        }

        return new BattleSnake(index, body, heading);
    }

    private bool SpawnFood()
    {
        var free = new List<GridPoint>();
        var taken = new HashSet<GridPoint>(Snakes.SelectMany(s => s.Body).Concat(_foods));
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var p = new GridPoint(x, y);
                if (!taken.Contains(p))
                {
                    free.Add(p);
                }
            }
        }

        if (free.Count == 0)
        {
            return false;
        }

        _foods.Add(free[_random.Next(free.Count)]);
        return true;
    }
}
