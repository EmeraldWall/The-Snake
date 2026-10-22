using System.Text;

namespace TheSnake.Core.Battle;

/// <summary>What every phone draws: the host builds it from its engine, the others receive it.</summary>
public sealed class BattleSnapshot
{
    public int Tick { get; set; }

    public int RemainingMs { get; set; }

    public BattleStatus Status { get; set; }

    public int Winner { get; set; } = -1;

    public int Width { get; set; } = Arenas.StandardWidth;

    public int Height { get; set; } = Arenas.StandardHeight;

    public List<GridPoint> Foods { get; set; } = [];

    public List<SnakeState> Snakes { get; set; } = [];

    public static BattleSnapshot From(BattleEngine engine) => new()
    {
        Tick = engine.Ticks,
        RemainingMs = engine.RemainingMs,
        Status = engine.Status,
        Winner = engine.Winner,
        Width = engine.Width,
        Height = engine.Height,
        Foods = [.. engine.Foods],
        Snakes = engine.Snakes.Select(s => new SnakeState(s.Alive, s.Dropped, s.Heading, s.Kills, [.. s.Body])).ToList(),
    };
}

public sealed record SnakeState(bool Alive, bool Dropped, Direction Heading, int Kills, List<GridPoint> Body);

public enum MessageType : byte
{
    Hello = 1,
    Lobby = 2,
    Start = 3,
    Turn = 4,
    State = 5,
    Bye = 6,
}

/// <summary>A decoded network message. Only the fields for its type are set.</summary>
public sealed record BattleMessage(
    MessageType Type,
    string Name = "",
    int Protocol = 0,
    IReadOnlyList<string>? Players = null,
    int YourIndex = -1,
    uint Seed = 0,
    Direction Turn = Direction.Up,
    BattleSnapshot? State = null);

/// <summary>
/// Compact binary messages sent over Nearby Connections. A full state for 4 snakes is a few hundred bytes,
/// small enough to send every 150 ms tick. Cells fit in one byte per axis because arenas are under 256 cells.
/// </summary>
public static class BattleWire
{
    public const int ProtocolVersion = 1;
    public const int MaxNameLength = 12;

    public static byte[] Hello(string name) => Write(MessageType.Hello, w =>
    {
        w.Write(ProtocolVersion);
        w.Write(CleanName(name));
    });

    public static byte[] Lobby(IReadOnlyList<string> players, int yourIndex) => Write(MessageType.Lobby, w =>
    {
        w.Write((byte)yourIndex);
        WriteNames(w, players);
    });

    public static byte[] Start(uint seed, IReadOnlyList<string> players, int yourIndex) => Write(MessageType.Start, w =>
    {
        w.Write(seed);
        w.Write((byte)yourIndex);
        WriteNames(w, players);
    });

    public static byte[] Turn(Direction direction) => Write(MessageType.Turn, w => w.Write((byte)direction));

    public static byte[] Bye() => Write(MessageType.Bye, _ => { });

    public static byte[] State(BattleSnapshot s) => Write(MessageType.State, w =>
    {
        w.Write(s.Tick);
        w.Write(s.RemainingMs);
        w.Write((byte)s.Status);
        w.Write((sbyte)s.Winner);
        w.Write((byte)s.Width);
        w.Write((byte)s.Height);
        w.Write((byte)s.Foods.Count);
        foreach (var f in s.Foods)
        {
            WriteCell(w, f);
        }

        w.Write((byte)s.Snakes.Count);
        foreach (var snake in s.Snakes)
        {
            byte flags = (byte)((snake.Alive ? 1 : 0) | (snake.Dropped ? 2 : 0));
            w.Write(flags);
            w.Write((byte)snake.Heading);
            w.Write((byte)snake.Kills);
            w.Write((ushort)snake.Body.Count);
            foreach (var c in snake.Body)
            {
                WriteCell(w, c);
            }
        }
    });

    public static BattleMessage? Read(byte[] data)
    {
        try
        {
            using var r = new BinaryReader(new MemoryStream(data), Encoding.UTF8);
            var type = (MessageType)r.ReadByte();
            switch (type)
            {
                case MessageType.Hello:
                    int protocol = r.ReadInt32();
                    return new BattleMessage(type, Name: CleanName(r.ReadString()), Protocol: protocol);
                case MessageType.Lobby:
                    int you = r.ReadByte();
                    return new BattleMessage(type, Players: ReadNames(r), YourIndex: you);
                case MessageType.Start:
                    uint seed = r.ReadUInt32();
                    int index = r.ReadByte();
                    return new BattleMessage(type, Seed: seed, YourIndex: index, Players: ReadNames(r));
                case MessageType.Turn:
                    return new BattleMessage(type, Turn: (Direction)(r.ReadByte() & 3));
                case MessageType.Bye:
                    return new BattleMessage(type);
                case MessageType.State:
                    var s = new BattleSnapshot
                    {
                        Tick = r.ReadInt32(),
                        RemainingMs = r.ReadInt32(),
                        Status = (BattleStatus)r.ReadByte(),
                        Winner = r.ReadSByte(),
                        Width = r.ReadByte(),
                        Height = r.ReadByte(),
                    };
                    int foods = r.ReadByte();
                    for (int i = 0; i < foods; i++)
                    {
                        s.Foods.Add(ReadCell(r));
                    }

                    int snakes = r.ReadByte();
                    for (int i = 0; i < snakes; i++)
                    {
                        byte flags = r.ReadByte();
                        var heading = (Direction)(r.ReadByte() & 3);
                        int kills = r.ReadByte();
                        int length = r.ReadUInt16();
                        var body = new List<GridPoint>(length);
                        for (int j = 0; j < length; j++)
                        {
                            body.Add(ReadCell(r));
                        }

                        s.Snakes.Add(new SnakeState((flags & 1) != 0, (flags & 2) != 0, heading, kills, body));
                    }

                    return new BattleMessage(type, State: s);
                default:
                    return null;
            }
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }

    public static string CleanName(string? name)
    {
        var clean = new string((name ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (clean.Length > MaxNameLength)
        {
            clean = clean[..MaxNameLength];
        }

        return clean.Length == 0 ? "Player" : clean;
    }

    private static byte[] Write(MessageType type, Action<BinaryWriter> body)
    {
        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            w.Write((byte)type);
            body(w);
        }

        return ms.ToArray();
    }

    private static void WriteNames(BinaryWriter w, IReadOnlyList<string> names)
    {
        w.Write((byte)names.Count);
        foreach (var n in names)
        {
            w.Write(CleanName(n));
        }
    }

    private static List<string> ReadNames(BinaryReader r)
    {
        int count = r.ReadByte();
        var names = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            names.Add(CleanName(r.ReadString()));
        }

        return names;
    }

    private static void WriteCell(BinaryWriter w, GridPoint p)
    {
        w.Write((byte)p.X);
        w.Write((byte)p.Y);
    }

    private static GridPoint ReadCell(BinaryReader r) => new(r.ReadByte(), r.ReadByte());
}
