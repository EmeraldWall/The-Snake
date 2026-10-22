using System.Text;

namespace TheSnake.Core;

/// <summary>
/// 8 character Crockford Base32 code (40 bits) that lets a friend replay the same board.
/// Layout: 26 bit seed, 1 bit rules, 4 bit level, 3 bit variant, 6 bit check.
/// </summary>
public static class ChallengeCode
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string Encode(GameSetup setup)
    {
        ulong rules = setup.Rules == RuleSet.Retro ? 1UL : 0UL;
        ulong level = setup.Rules == RuleSet.Retro ? (ulong)setup.RetroLevel : 0UL;
        ulong variant = setup.Rules == RuleSet.Retro ? (ulong)setup.Variant : (setup.WeeklyObstacles ? 1UL : 0UL);
        ulong payload = (setup.Seed & Seeds.SeedMask) | (rules << 26) | (level << 27) | (variant << 31);
        ulong value = payload | ((ulong)Check(payload) << 34);

        var sb = new StringBuilder(8);
        for (int i = 7; i >= 0; i--)
        {
            sb.Append(Alphabet[(int)((value >> (i * 5)) & 31)]);
        }

        return sb.ToString();
    }

    public static bool TryDecode(string? text, out GameSetup setup)
    {
        setup = null!;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        ulong value = 0;
        int digits = 0;
        foreach (char raw in text.ToUpperInvariant())
        {
            if (raw is ' ' or '-')
            {
                continue;
            }

            char c = raw switch { 'O' => '0', 'I' or 'L' => '1', _ => raw };
            int index = Alphabet.IndexOf(c);
            if (index < 0)
            {
                return false;
            }

            value = (value << 5) | (uint)index;
            digits++;
        }

        if (digits != 8)
        {
            return false;
        }

        ulong payload = value & ((1UL << 34) - 1);
        if ((value >> 34) != Check(payload))
        {
            return false;
        }

        uint seed = (uint)(payload & Seeds.SeedMask);
        bool retro = ((payload >> 26) & 1) == 1;
        int level = (int)((payload >> 27) & 15);
        int variant = (int)((payload >> 31) & 7);

        if (retro)
        {
            if (level is < 1 or > 9 || variant > (int)RetroVariant.Apartment)
            {
                return false;
            }

            setup = Arenas.Retro(seed, level, (RetroVariant)variant) with { Mode = GameMode.Challenge };
            return true;
        }

        if (level != 0 || variant > 1)
        {
            return false;
        }

        setup = variant == 1
            ? Arenas.WeeklyArena(seed, GameMode.Challenge)
            : Arenas.Endless(seed) with { Mode = GameMode.Challenge };
        return true;
    }

    private static uint Check(ulong payload) => Hashing.Fnv1a(payload.ToString("X9")) & 63;
}
