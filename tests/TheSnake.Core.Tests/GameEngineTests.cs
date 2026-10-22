using TheSnake.Core;

namespace TheSnake.Core.Tests;

/// <summary>Always returns 0, so food spawns on the first free cell in reading order (top left).</summary>
internal sealed class ZeroRandom : IRandomSource
{
    public int Next(int maxExclusive) => 0;
}

[TestClass]
public sealed class GameEngineTests
{
    private static readonly IReadOnlySet<GridPoint> None = new HashSet<GridPoint>();

    private static GameEngine Modern(bool wrap = true, int width = 24, int height = 36) =>
        new(new GameSetup(GameMode.Endless, RuleSet.Modern, width, height, wrap, None, 1), new ZeroRandom());

    [TestMethod]
    public void StartsWithFourSegmentsHeadingRight()
    {
        var engine = Modern();
        Assert.AreEqual(4, engine.Snake.Count);
        Assert.AreEqual(Direction.Right, engine.Heading);
        Assert.AreEqual(new GridPoint(12, 18), engine.Head);
    }

    [TestMethod]
    public void MovesOneCellPerTickInEachDirection()
    {
        var engine = Modern();
        engine.Tick();
        Assert.AreEqual(new GridPoint(13, 18), engine.Head);
        engine.QueueTurn(Direction.Down);
        engine.Tick();
        Assert.AreEqual(new GridPoint(13, 19), engine.Head);
        engine.QueueTurn(Direction.Left);
        engine.Tick();
        Assert.AreEqual(new GridPoint(12, 19), engine.Head);
        engine.QueueTurn(Direction.Up);
        engine.Tick();
        Assert.AreEqual(new GridPoint(12, 18), engine.Head);
    }

    [TestMethod]
    public void ReverseIsIgnored()
    {
        var engine = Modern();
        Assert.IsFalse(engine.QueueTurn(Direction.Left));
        engine.Tick();
        Assert.IsFalse(engine.IsDead);
        Assert.AreEqual(Direction.Right, engine.Heading);
    }

    [TestMethod]
    public void PredictsTheNextCellIncludingAQueuedTurn()
    {
        var engine = Modern();
        Assert.AreEqual(new GridPoint(13, 18), engine.PredictNextCell(out _));
        engine.QueueTurn(Direction.Up);
        Assert.AreEqual(Direction.Up, engine.NextHeading);
        Assert.AreEqual(new GridPoint(12, 17), engine.PredictNextCell(out _));
    }

    [TestMethod]
    public void PredictionIsNullAtAWall()
    {
        var engine = Modern(wrap: false, width: 10, height: 10);
        for (int i = 0; i < 4; i++) engine.Tick();
        Assert.AreEqual(9, engine.Head.X);
        Assert.IsNull(engine.PredictNextCell(out _));
    }

    [TestMethod]
    public void TwoQueuedTurnsApplyOverTwoTicks()
    {
        var engine = Modern();
        Assert.IsTrue(engine.QueueTurn(Direction.Up));
        Assert.IsTrue(engine.QueueTurn(Direction.Left));
        Assert.IsFalse(engine.QueueTurn(Direction.Down), "queue holds only 2 turns");
        engine.Tick();
        Assert.AreEqual(Direction.Up, engine.Heading);
        engine.Tick();
        Assert.AreEqual(Direction.Left, engine.Heading);
    }

    [TestMethod]
    public void WrapsAroundAllEdges()
    {
        var engine = Modern(width: 10, height: 10);
        for (int i = 0; i < 5; i++) engine.Tick();
        Assert.AreEqual(0, engine.Head.X, "wrapped right edge");
        Assert.IsTrue(engine.LastMoveWrapped);
        engine.QueueTurn(Direction.Down);
        for (int i = 0; i < 5; i++) engine.Tick();
        Assert.AreEqual(0, engine.Head.Y, "wrapped bottom edge");
        Assert.IsFalse(engine.IsDead);
    }

    [TestMethod]
    public void DiesAtWallWhenWrapIsOff()
    {
        var engine = Modern(wrap: false, width: 10, height: 10);
        TickEvents events = TickEvents.None;
        for (int i = 0; i < 5 && !engine.IsDead; i++) events = engine.Tick();
        Assert.IsTrue(engine.IsDead);
        Assert.IsTrue(events.HasFlag(TickEvents.Died));
    }

    [TestMethod]
    public void DiesOnObstacle()
    {
        var obstacles = new HashSet<GridPoint> { new(14, 18) };
        var engine = new GameEngine(new GameSetup(GameMode.Endless, RuleSet.Modern, 24, 36, true, obstacles, 1), new ZeroRandom());
        engine.Tick();
        Assert.IsFalse(engine.IsDead);
        engine.Tick();
        Assert.IsTrue(engine.IsDead);
    }

    [TestMethod]
    public void MovingIntoTheLeavingTailIsSafe()
    {
        // A 4 long snake turning in a tight square chases its own tail without dying.
        var engine = Modern();
        engine.QueueTurn(Direction.Down);
        engine.Tick();
        engine.QueueTurn(Direction.Left);
        engine.Tick();
        engine.QueueTurn(Direction.Up);
        engine.Tick();
        Assert.IsFalse(engine.IsDead);
    }

    [TestMethod]
    public void DiesOnSelfCollision()
    {
        var engine = Modern();
        EatFoodTimes(engine, 3);
        Assert.IsTrue(engine.Snake.Count >= 7);
        engine.QueueTurn(engine.Heading.TurnRight());
        engine.Tick();
        engine.QueueTurn(engine.Heading.TurnRight());
        engine.Tick();
        engine.QueueTurn(engine.Heading.TurnRight());
        engine.Tick();
        Assert.IsTrue(engine.IsDead);
    }

    [TestMethod]
    public void EatingGrowsByOneAndScoresTen()
    {
        var engine = Modern();
        EatFoodTimes(engine, 1);
        Assert.AreEqual(5, engine.Snake.Count);
        Assert.AreEqual(10, engine.Score);
        Assert.AreEqual(1, engine.FoodsEaten);
    }

    [TestMethod]
    public void FoodNeverSpawnsOnSnakeOrObstacle()
    {
        var obstacles = Arenas.WeeklyLayout(42, 24, 36);
        var engine = new GameEngine(new GameSetup(GameMode.WeeklyArena, RuleSet.Modern, 24, 36, true, obstacles, 42));
        for (int i = 0; i < 2000 && !engine.IsOver; i++)
        {
            Assert.IsFalse(engine.IsOccupiedBySnake(engine.Food));
            Assert.IsFalse(obstacles.Contains(engine.Food));
            SteerTowardFood(engine);
            engine.Tick();
        }
    }

    [TestMethod]
    public void SpeedRampsDownToFloor()
    {
        var engine = Modern();
        Assert.AreEqual(220, engine.TickLengthMs);
        EatFoodTimes(engine, 10);
        Assert.AreEqual(190, engine.TickLengthMs);
    }

    [TestMethod]
    public void ComboRisesWhenEatingQuickly()
    {
        var engine = Modern();
        EatFoodTimes(engine, 2);
        Assert.IsTrue(engine.Combo >= 2, $"combo was {engine.Combo}");
        Assert.IsTrue(engine.Score > 20);
    }

    [TestMethod]
    public void ComboResetsAfterWindow()
    {
        var engine = Modern(width: 60, height: 60);
        EatFoodTimes(engine, 1);
        Assert.AreEqual(3000, engine.ComboWindowLeftMs);
        // Ride away from food for more than 3 seconds.
        engine.QueueTurn(Direction.Down);
        for (int i = 0; i < 20; i++) engine.Tick();
        Assert.AreEqual(1, engine.Combo);
        Assert.AreEqual(0, engine.ComboWindowLeftMs);
    }

    [TestMethod]
    public void PowerUpAppearsAfterTwelveFoods()
    {
        var engine = new GameEngine(Arenas.Endless(7));
        bool spawned = false;
        for (int i = 0; i < 20000 && !engine.IsOver && engine.FoodsEaten < 12; i++)
        {
            SteerTowardFood(engine);
            spawned |= engine.Tick().HasFlag(TickEvents.PowerUpSpawned);
        }

        Assert.AreEqual(12, engine.FoodsEaten);
        Assert.IsTrue(spawned);
    }

    [TestMethod]
    public void SameSeedGivesSameGame()
    {
        static string Play(uint seed)
        {
            var engine = new GameEngine(Arenas.DailyRun(new DateOnly(2026, 10, 22)) with { Seed = seed });
            var log = new System.Text.StringBuilder();
            for (int i = 0; i < 3000 && !engine.IsOver; i++)
            {
                SteerTowardFood(engine);
                engine.Tick();
                log.Append(engine.Food).Append(engine.Score);
            }

            return log.ToString();
        }

        Assert.AreEqual(Play(123), Play(123));
        Assert.AreNotEqual(Play(123), Play(124));
    }

    [TestMethod]
    public void RetroUsesFixedLevelSpeedAndLevelPoints()
    {
        var engine = new GameEngine(Arenas.Retro(1, 9, RetroVariant.WallsOff), new ZeroRandom());
        Assert.AreEqual(95, engine.TickLengthMs);
        EatFoodTimes(engine, 1);
        Assert.AreEqual(9, engine.Score);
        Assert.AreEqual(95, engine.TickLengthMs);
    }

    [TestMethod]
    public void RetroCritterAppearsAfterFiveFoods()
    {
        var engine = new GameEngine(Arenas.Retro(3, 5, RetroVariant.WallsOff));
        bool spawned = false;
        for (int i = 0; i < 20000 && !engine.IsOver && engine.FoodsEaten < 5; i++)
        {
            SteerTowardFood(engine);
            spawned |= engine.Tick().HasFlag(TickEvents.CritterSpawned);
        }

        Assert.IsTrue(spawned);
        Assert.IsNotNull(engine.Critter);
    }

    [TestMethod]
    public void PuristQueueHoldsOneTurn()
    {
        var engine = new GameEngine(Arenas.Retro(1, 5, RetroVariant.WallsOff, purist: true));
        Assert.IsTrue(engine.QueueTurn(Direction.Up));
        Assert.IsFalse(engine.QueueTurn(Direction.Left));
    }

    [TestMethod]
    public void MazesKeepTheStartZoneClear()
    {
        foreach (var variant in Enum.GetValues<RetroVariant>())
        {
            var setup = Arenas.Retro(1, 5, variant);
            var engine = new GameEngine(setup);
            foreach (var cell in engine.Snake)
            {
                Assert.IsFalse(setup.Obstacles.Contains(cell), $"{variant} blocks the start");
            }

            engine.Tick();
            Assert.IsFalse(engine.IsDead, $"{variant} kills on the first move");
        }
    }

    private static void EatFoodTimes(GameEngine engine, int count)
    {
        int target = engine.FoodsEaten + count;
        for (int i = 0; i < 10000 && engine.FoodsEaten < target && !engine.IsOver; i++)
        {
            SteerTowardFood(engine);
            engine.Tick();
        }

        Assert.AreEqual(target, engine.FoodsEaten, "steering helper failed to reach the food");
    }

    /// <summary>Simple safe greedy steering used to drive the engine in tests.</summary>
    internal static void SteerTowardFood(GameEngine engine)
    {
        var head = engine.Head;
        var food = engine.Food;
        var options = new List<Direction>();
        if (food.X > head.X) options.Add(Direction.Right);
        if (food.X < head.X) options.Add(Direction.Left);
        if (food.Y > head.Y) options.Add(Direction.Down);
        if (food.Y < head.Y) options.Add(Direction.Up);
        options.AddRange(Enum.GetValues<Direction>());

        foreach (var d in options)
        {
            if (d == engine.Heading.Opposite())
            {
                continue;
            }

            var next = head.Step(d);
            next = new GridPoint((next.X + engine.Setup.Width) % engine.Setup.Width, (next.Y + engine.Setup.Height) % engine.Setup.Height);
            if (!engine.IsOccupiedBySnake(next) && !engine.IsObstacle(next))
            {
                if (d != engine.Heading)
                {
                    engine.QueueTurn(d);
                }

                return;
            }
        }
    }
}
