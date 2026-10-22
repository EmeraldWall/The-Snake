using TheSnake.Core;

namespace TheSnake.Services;

public enum Board
{
    Endless,
    DailyRun,
    WeeklyArena,
    Retro97,
}

/// <summary>Google Play Games Services v2: sign in and leaderboards.</summary>
public interface IGamesService
{
    /// <summary>False until real Play Console IDs are filled in (Platforms/Android/Resources/values/games.xml).</summary>
    bool IsConfigured { get; }

    bool IsSignedIn { get; }

    event EventHandler? SignInChanged;

    /// <summary>Checks the automatic sign in. Never shows UI.</summary>
    Task RefreshSignInAsync();

    /// <summary>Asks the player to sign in. Returns true when signed in.</summary>
    Task<bool> SignInAsync();

    /// <summary>Submits a score now, or keeps it on the phone and sends it after the next sign in.</summary>
    void Submit(Board board, long score);

    /// <summary>The player's rank text for the board, such as "#12 today", or null if unknown.</summary>
    Task<string?> GetRankTextAsync(Board board);

    Task ShowLeaderboardAsync(Board? board = null);

    public static Board? BoardFor(GameMode mode) => mode switch
    {
        GameMode.Endless => Board.Endless,
        GameMode.DailyRun => Board.DailyRun,
        GameMode.WeeklyArena => Board.WeeklyArena,
        GameMode.Retro97 => Board.Retro97,
        _ => null,
    };
}

public sealed class NoGamesService : IGamesService
{
    public bool IsConfigured => false;

    public bool IsSignedIn => false;

    public event EventHandler? SignInChanged { add { } remove { } }

    public Task RefreshSignInAsync() => Task.CompletedTask;

    public Task<bool> SignInAsync() => Task.FromResult(false);

    public void Submit(Board board, long score)
    {
    }

    public Task<string?> GetRankTextAsync(Board board) => Task.FromResult<string?>(null);

    public Task ShowLeaderboardAsync(Board? board = null) => Task.CompletedTask;
}
