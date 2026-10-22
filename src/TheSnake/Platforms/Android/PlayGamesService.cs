using System.Text.Json;
using Android.Gms.Extensions;
using Android.Gms.Games;
using Android.Gms.Games.Leaderboard;
using TheSnake.Services;

namespace TheSnake.Platforms.Android;

/// <summary>
/// Google Play Games Services v2 (spec 10). The SDK is only started when real IDs are configured, so a build
/// without a Play Console project behaves exactly like before. Scores that cannot be sent wait on the phone.
/// </summary>
public sealed class PlayGamesService : IGamesService
{
    private const string QueueKey = "games.pendingScores";
    private const int TimeSpanDaily = 0;
    private const int TimeSpanWeekly = 1;
    private const int TimeSpanAllTime = 2;
    private const int CollectionPublic = 0;

    private static bool s_sdkStarted;

    public bool IsConfigured => AppIdOk(Platform.AppContext);

    public bool IsSignedIn { get; private set; }

    public event EventHandler? SignInChanged;

    /// <summary>Called from MainApplication.OnCreate. Starts the SDK only when the project ID is real.</summary>
    public static void StartIfConfigured(global::Android.Content.Context context)
    {
        if (!s_sdkStarted && AppIdOk(context))
        {
            PlayGamesSdk.Initialize(context);
            s_sdkStarted = true;
        }
    }

    public async Task RefreshSignInAsync()
    {
        if (!Ready(out var activity))
        {
            return;
        }

        try
        {
            var result = await PlayGames.GetGamesSignInClient(activity).IsAuthenticated().AsAsync<AuthenticationResult>();
            SetSignedIn(result?.IsAuthenticated ?? false);
        }
        catch (Java.Lang.Exception)
        {
            SetSignedIn(false);
        }
    }

    public async Task<bool> SignInAsync()
    {
        if (!Ready(out var activity))
        {
            return false;
        }

        try
        {
            var result = await PlayGames.GetGamesSignInClient(activity).SignIn().AsAsync<AuthenticationResult>();
            SetSignedIn(result?.IsAuthenticated ?? false);
        }
        catch (Java.Lang.Exception)
        {
            SetSignedIn(false);
        }

        return IsSignedIn;
    }

    public void Submit(Board board, long score)
    {
        var queue = LoadQueue();
        queue[board.ToString()] = Math.Max(score, queue.GetValueOrDefault(board.ToString()));
        SaveQueue(queue);
        _ = FlushAsync();
    }

    public async Task<string?> GetRankTextAsync(Board board)
    {
        if (!IsSignedIn || !Ready(out var activity) || LeaderboardId(board) is not { } id)
        {
            return null;
        }

        try
        {
            int span = SpanFor(board);
            var data = await PlayGames.GetLeaderboardsClient(activity)
                .LoadCurrentPlayerLeaderboardScore(id, span, CollectionPublic)
                .AsAsync<AnnotatedData>();
            var (rank, display) = data?.Get() switch
            {
                LeaderboardScoreEntity e => (e.Rank, e.DisplayRank),
                LeaderboardScoreRef r => (r.Rank, r.DisplayRank),
                _ => (0L, null),
            };
            if (rank > 0)
            {
                string when = span switch { TimeSpanDaily => " today", TimeSpanWeekly => " this week", _ => "" };
                return $"Rank {display}{when}";
            }
        }
        catch (Java.Lang.Exception)
        {
        }

        return null;
    }

    public async Task ShowLeaderboardAsync(Board? board = null)
    {
        if (!Ready(out var activity))
        {
            return;
        }

        if (!IsSignedIn && !await SignInAsync())
        {
            return;
        }

        try
        {
            var client = PlayGames.GetLeaderboardsClient(activity);
            var intent = board is { } b && LeaderboardId(b) is { } id
                ? await client.GetLeaderboardIntent(id, SpanFor(b)).AsAsync<global::Android.Content.Intent>()
                : await client.GetAllLeaderboardsIntent().AsAsync<global::Android.Content.Intent>();
            activity.StartActivityForResult(intent, 9004);
        }
        catch (Java.Lang.Exception)
        {
        }
    }

    private async Task FlushAsync()
    {
        if (!IsSignedIn || !Ready(out var activity))
        {
            return;
        }

        var queue = LoadQueue();
        var client = PlayGames.GetLeaderboardsClient(activity);
        foreach (var (name, score) in queue.ToList())
        {
            if (Enum.TryParse<Board>(name, out var board) && LeaderboardId(board) is { } id)
            {
                try
                {
                    await client.SubmitScoreImmediate(id, score).AsAsync();
                    queue.Remove(name);
                }
                catch (Java.Lang.Exception)
                {
                    // Keep it and try again after the next sign in.
                }
            }
        }

        SaveQueue(queue);
    }

    private void SetSignedIn(bool value)
    {
        bool changed = value != IsSignedIn;
        IsSignedIn = value;
        if (value)
        {
            _ = FlushAsync();
        }

        if (changed)
        {
            MainThread.BeginInvokeOnMainThread(() => SignInChanged?.Invoke(this, EventArgs.Empty));
        }
    }

    private bool Ready(out global::Android.App.Activity activity)
    {
        activity = Platform.CurrentActivity!;
        return s_sdkStarted && activity is not null;
    }

    private static int SpanFor(Board board) => board switch
    {
        Board.DailyRun => TimeSpanDaily,
        Board.WeeklyArena => TimeSpanWeekly,
        _ => TimeSpanAllTime,
    };

    private static string? LeaderboardId(Board board)
    {
        string name = board switch
        {
            Board.Endless => "leaderboard_endless",
            Board.DailyRun => "leaderboard_daily_run",
            Board.WeeklyArena => "leaderboard_weekly_arena",
            _ => "leaderboard_retro_97",
        };
        string value = ReadString(Platform.AppContext, name);
        return value.Length == 0 || value == "REPLACE_ME" ? null : value;
    }

    private static bool AppIdOk(global::Android.Content.Context context)
    {
        string id = ReadString(context, "game_services_project_id");
        return id.Length > 0 && id != "0" && id.All(char.IsDigit);
    }

    private static string ReadString(global::Android.Content.Context context, string name)
    {
        int resId = context.Resources?.GetIdentifier(name, "string", context.PackageName) ?? 0;
        return resId == 0 ? "" : context.GetString(resId).Trim();
    }

    private static Dictionary<string, long> LoadQueue()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, long>>(Preferences.Default.Get(QueueKey, "{}")) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void SaveQueue(Dictionary<string, long> queue) =>
        Preferences.Default.Set(QueueKey, JsonSerializer.Serialize(queue));
}
