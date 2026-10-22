using System.Globalization;

namespace TheSnake.Core;

/// <summary>
/// Seeds for the shared global battles. Every player gets the same board on the same
/// UTC day (Daily Run) or ISO week (Weekly Arena), without any server.
/// </summary>
public static class Seeds
{
    public const int SeedBits = 26;
    public const uint SeedMask = (1u << SeedBits) - 1;

    private const string Salt = "vishvplus.thesnake.v1";

    public static uint Daily(DateOnly utcDate) =>
        Hashing.Fnv1a($"daily:{utcDate:yyyy-MM-dd}:{Salt}") & SeedMask;

    public static uint Weekly(DateOnly utcDate)
    {
        var (year, week) = IsoWeek(utcDate);
        return Hashing.Fnv1a($"weekly:{year}-W{week:00}:{Salt}") & SeedMask;
    }

    public static (int Year, int Week) IsoWeek(DateOnly utcDate)
    {
        var dateTime = utcDate.ToDateTime(TimeOnly.MinValue);
        return (ISOWeek.GetYear(dateTime), ISOWeek.GetWeekOfYear(dateTime));
    }

    public static string WeekKey(DateOnly utcDate)
    {
        var (year, week) = IsoWeek(utcDate);
        return $"{year}-W{week:00}";
    }

    public static DateOnly TodayUtc() => DateOnly.FromDateTime(DateTime.UtcNow);
}
