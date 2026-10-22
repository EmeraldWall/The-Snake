using TheSnake.Core;
using TheSnake.Core.Battle;

namespace TheSnake.Core.Tests;

[TestClass]
public sealed class BattleTests
{
    [TestMethod]
    public void StartsFourSnakesInSeparateCornersWithFood()
    {
        var battle = new BattleEngine(4, 1);
        Assert.AreEqual(4, battle.Snakes.Count);
        var cells = battle.Snakes.SelectMany(s => s.Body).ToList();
        Assert.AreEqual(cells.Count, cells.Distinct().Count(), "snakes must not overlap at the start");
        Assert.AreEqual(BattleEngine.FoodCount, battle.Foods.Count);
        Assert.IsFalse(battle.Foods.Any(cells.Contains));
    }

    [TestMethod]
    public void AllSnakesMoveEachTick()
    {
        var battle = new BattleEngine(2, 1);
        var heads = battle.Snakes.Select(s => s.Body[0]).ToList();
        battle.Tick();
        Assert.AreEqual(heads[0].Step(Direction.Right), battle.Snakes[0].Body[0]);
        Assert.AreEqual(heads[1].Step(Direction.Left), battle.Snakes[1].Body[0]);
        Assert.AreEqual(150, battle.ElapsedMs);
    }

    [TestMethod]
    public void HittingAnotherSnakeKillsYouAndCreditsThem()
    {
        var battle = new BattleEngine(2, 1);
        // Put player 1 right in front of player 0.
        var target = battle.Snakes[0].Body[0].Step(Direction.Right);
        battle.Snakes[1].Body.Clear();
        battle.Snakes[1].Body.AddRange([target, target.Step(Direction.Down), target.Step(Direction.Down).Step(Direction.Down)]);
        battle.Snakes[1].Heading = Direction.Up;
        battle.QueueTurn(1, Direction.Right);
        battle.Tick();
        Assert.IsFalse(battle.Snakes[0].Alive);
        Assert.AreEqual(1, battle.Snakes[1].Kills);
        Assert.AreEqual(BattleStatus.Over, battle.Status);
        Assert.AreEqual(1, battle.Winner);
    }

    [TestMethod]
    public void HeadOnCrashKillsBothAndIsADraw()
    {
        var battle = new BattleEngine(2, 1);
        var a = battle.Snakes[0];
        var b = battle.Snakes[1];
        // Line them up two cells apart, facing each other.
        a.Body.Clear();
        a.Body.AddRange([new GridPoint(10, 10), new GridPoint(9, 10), new GridPoint(8, 10)]);
        a.Heading = Direction.Right;
        b.Body.Clear();
        b.Body.AddRange([new GridPoint(12, 10), new GridPoint(13, 10), new GridPoint(14, 10)]);
        b.Heading = Direction.Left;
        battle.Tick();
        Assert.IsFalse(a.Alive);
        Assert.IsFalse(b.Alive);
        Assert.AreEqual(-1, battle.Winner);
        Assert.AreEqual(BattleStatus.Over, battle.Status);
    }

    [TestMethod]
    public void LongestSnakeWinsWhenTimeIsUp()
    {
        var battle = new BattleEngine(2, 3);
        battle.Snakes[1].Body.Add(battle.Snakes[1].Body[^1].Step(Direction.Right));
        // Keep both snakes circling safely in small squares until time runs out.
        Direction[] loop = [Direction.Down, Direction.Left, Direction.Up, Direction.Right];
        Direction[] loop1 = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];
        for (int i = 0; battle.Status == BattleStatus.Running && i < 2000; i++)
        {
            if (i % 2 == 0)
            {
                battle.QueueTurn(0, loop[(i / 2) % 4]);
                battle.QueueTurn(1, loop1[(i / 2) % 4]);
            }

            battle.Tick();
        }

        Assert.AreEqual(BattleStatus.Over, battle.Status);
        if (battle.Snakes.All(s => s.Alive))
        {
            Assert.IsTrue(battle.ElapsedMs >= BattleEngine.DurationMs);
            int expected = battle.Snakes[0].Length == battle.Snakes[1].Length ? -1 : (battle.Snakes[0].Length > battle.Snakes[1].Length ? 0 : 1);
            Assert.AreEqual(expected, battle.Winner);
        }
    }

    [TestMethod]
    public void DroppedPlayerFreezesAndOthersWin()
    {
        var battle = new BattleEngine(3, 1);
        battle.Drop(2);
        Assert.IsTrue(battle.Snakes[2].Dropped);
        Assert.AreEqual(4, battle.Snakes[2].Body.Count, "the frozen snake stays on the board");
        Assert.AreEqual(BattleStatus.Running, battle.Status);
        battle.Drop(1);
        Assert.AreEqual(BattleStatus.Over, battle.Status);
        Assert.AreEqual(0, battle.Winner);
    }

    [TestMethod]
    public void StateRoundTripsThroughTheWire()
    {
        var battle = new BattleEngine(4, 99);
        for (int i = 0; i < 5; i++) battle.Tick();
        var snapshot = BattleSnapshot.From(battle);
        var message = BattleWire.Read(BattleWire.State(snapshot));
        Assert.IsNotNull(message);
        var copy = message.State!;
        Assert.AreEqual(snapshot.Tick, copy.Tick);
        Assert.AreEqual(snapshot.RemainingMs, copy.RemainingMs);
        CollectionAssert.AreEqual(snapshot.Foods, copy.Foods);
        for (int i = 0; i < 4; i++)
        {
            CollectionAssert.AreEqual(snapshot.Snakes[i].Body, copy.Snakes[i].Body);
            Assert.AreEqual(snapshot.Snakes[i].Heading, copy.Snakes[i].Heading);
            Assert.AreEqual(snapshot.Snakes[i].Alive, copy.Snakes[i].Alive);
        }

        Assert.IsTrue(BattleWire.State(snapshot).Length < 400, "a 4 player state must stay small");
    }

    [TestMethod]
    public void LobbyStartHelloAndTurnRoundTrip()
    {
        var hello = BattleWire.Read(BattleWire.Hello("  Vishv\n  "))!;
        Assert.AreEqual("Vishv", hello.Name);
        Assert.AreEqual(BattleWire.ProtocolVersion, hello.Protocol);

        var start = BattleWire.Read(BattleWire.Start(1234, ["Host", "Ana", "Raj"], 2))!;
        Assert.AreEqual(1234u, start.Seed);
        Assert.AreEqual(2, start.YourIndex);
        CollectionAssert.AreEqual(new[] { "Host", "Ana", "Raj" }, start.Players!.ToArray());

        Assert.AreEqual(Direction.Left, BattleWire.Read(BattleWire.Turn(Direction.Left))!.Turn);
        Assert.AreEqual("Averyverylon", BattleWire.CleanName("Averyverylongname"));
        Assert.AreEqual("Player", BattleWire.CleanName("   "));
        Assert.IsNull(BattleWire.Read([99]));
        Assert.IsNull(BattleWire.Read([(byte)MessageType.State, 1]));
    }

    [TestMethod]
    public void ScoresAboveWhatTheRunAllowsAreRejected()
    {
        Assert.IsTrue(GameEngine.IsPlausibleScore(500, 10));
        Assert.IsFalse(GameEngine.IsPlausibleScore(100_000, 10));
        Assert.IsFalse(GameEngine.IsPlausibleScore(-1, 10));
    }
}
