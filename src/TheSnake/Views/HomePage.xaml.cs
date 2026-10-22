using TheSnake.Core;
using TheSnake.Services;

namespace TheSnake.Views;

public partial class HomePage : ContentPage
{
    private readonly ProgressStore _progress = ServiceHelper.Get<ProgressStore>();
    private readonly AppSettings _settings = ServiceHelper.Get<AppSettings>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly IAdService _ads = ServiceHelper.Get<IAdService>();
    private bool _started;

    public HomePage()
    {
        InitializeComponent();
        _store.EntitlementChanged += (_, _) => Refresh();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
        if (!_started)
        {
            _started = true;
            // Restore Remove ads first, so a paying player never sees an ad request.
            await _store.RefreshAsync();
            Refresh();
            if (!_store.IsAdFree)
            {
                await _ads.InitializeAsync();
            }
        }
    }

    private void Refresh()
    {
        var p = _progress.Progress;
        var today = Seeds.TodayUtc();
        Progression.EnsureMissions(p, today);

        int level = Progression.LevelForXp(p.Xp);
        LevelLabel.Text = level >= Progression.MaxLevel ? $"Level {level} (max)" : $"Level {level}";
        XpBar.Progress = Progression.LevelFraction(p.Xp);
        StreakLabel.Text = p.StreakDays > 0 ? $"Streak {p.StreakDays} day{(p.StreakDays == 1 ? "" : "s")}" : "No streak yet";

        DailyButton.Text = Progression.HasPlayedDailyToday(p, today)
            ? $"Daily Run\nDone: {p.DailyScoreToday:N0}"
            : "Daily Run\nRanked try ready";
        p.Best.TryGetValue("weekly:" + Seeds.WeekKey(today), out int weeklyBest);
        WeeklyButton.Text = $"Weekly Arena\n{Seeds.WeekKey(today)}  Best {weeklyBest:N0}";
        var variant = (RetroVariant)_settings.RetroVariant;
        RetroButton.Text = $"Retro 97\n{Arenas.RetroName(variant)}, level {_settings.RetroLevel}";

        MissionsList.Children.Clear();
        foreach (var m in p.Missions)
        {
            MissionsList.Children.Add(new Label
            {
                Text = $"{(m.Done ? "Done" : $"{m.Progress}/{m.Target}")}   {m.Text}   +{m.RewardXp} XP",
                TextColor = m.Done ? Color.FromArgb("#39FF88") : Color.FromArgb("#C9E8D6"),
                FontSize = 14,
            });
        }

        p.Best.TryGetValue("endless", out int endless);
        p.Best.TryGetValue("daily", out int daily);
        BestLabel.Text = $"Endless {endless:N0}    Daily {daily:N0}    Runs played {p.TotalRuns:N0}";

        RemoveAdsButton.IsVisible = !_store.IsAdFree;
        RemoveAdsButton.Text = _store.RemoveAdsPrice is { } price ? $"Remove ads {price}" : "Remove ads";
    }

    private Task Start(GameSetup? setup, string bestKey) =>
        Navigation.PushAsync(new GamePage(setup, bestKey));

    private void OnPlay(object? sender, EventArgs e) => _ = Start(null, "endless");

    private void OnDaily(object? sender, EventArgs e) => _ = Start(Arenas.DailyRun(Seeds.TodayUtc()), "daily");

    private void OnWeekly(object? sender, EventArgs e)
    {
        var today = Seeds.TodayUtc();
        _ = Start(Arenas.WeeklyArena(today), "weekly:" + Seeds.WeekKey(today));
    }

    private async void OnRetro(object? sender, EventArgs e)
    {
        var names = Enum.GetValues<RetroVariant>().Select(Arenas.RetroName).ToArray();
        string? choice = await DisplayActionSheetAsync("Retro 97 mode", "Cancel", null, names);
        int index = Array.IndexOf(names, choice);
        if (index < 0)
        {
            return;
        }

        var levels = Enumerable.Range(1, 9).Select(l => $"Level {l}").ToArray();
        string? levelChoice = await DisplayActionSheetAsync("Speed level", "Cancel", null, levels);
        int level = Array.IndexOf(levels, levelChoice) + 1;
        if (level < 1)
        {
            return;
        }

        _settings.RetroVariant = index;
        _settings.RetroLevel = level;
        var variant = (RetroVariant)index;
        var seed = (uint)Random.Shared.Next() & Seeds.SeedMask;
        await Start(Arenas.Retro(seed, level, variant, _settings.Purist), $"retro:{variant}:{level}:{(_settings.Purist ? "purist" : "classic")}");
    }

    private async void OnChallenge(object? sender, EventArgs e)
    {
        string? code = await DisplayPromptAsync("Challenge code", "Type the 8 character code your friend shared.",
            "Play", "Cancel", "K7Q2M9XA", maxLength: 10, keyboard: Keyboard.Text);
        if (code is null)
        {
            return;
        }

        if (!ChallengeCode.TryDecode(code, out var setup))
        {
            await DisplayAlertAsync("Challenge code", "That code does not look right. Check the letters and try again.", "OK");
            return;
        }

        await Start(setup, "challenge:" + ChallengeCode.Encode(setup));
    }

    private void OnSettings(object? sender, EventArgs e) => _ = Navigation.PushAsync(new SettingsPage());

    private async void OnRemoveAds(object? sender, EventArgs e)
    {
        await RemoveAdsFlow.RunAsync();
        Refresh();
    }
}
