namespace TheSnake.Core;

public interface IRandomSource
{
    /// <summary>Returns a value from 0 up to, but not including, <paramref name="maxExclusive"/>.</summary>
    int Next(int maxExclusive);
}

/// <summary>
/// SplitMix64 generator. Unlike System.Random it is guaranteed to give the same sequence
/// on every device and .NET version, which seeded battles and challenge codes rely on.
/// </summary>
public sealed class SeedRandom : IRandomSource
{
    private ulong _state;

    public SeedRandom(ulong seed) => _state = seed;

    public ulong NextULong()
    {
        ulong z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        return (int)(NextULong() % (ulong)maxExclusive);
    }
}

public static class Hashing
{
    /// <summary>32 bit FNV-1a, stable across platforms.</summary>
    public static uint Fnv1a(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 16777619;
        }

        return hash;
    }
}
