using TheSnake.Core;
using TheSnake.Services;
using TheSnake.Ui;

namespace TheSnake.Views;

public partial class HomePage : ContentPage, IHasSheetHost
{
    private readonly ProgressStore _progress = ServiceHelper.Get<ProgressStore>();
    private readonly AppSettings _settings = ServiceHelper.Get<AppSettings>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly IAdService _ads = ServiceHelper.Get<IAdService>();
    private readonly IGamesService _games = ServiceHelper.Get<IGamesService>();
    private bool _started;

    public HomePage()
    {
        InitializeComponent();

        // Full width on phones, a centred column on tablets and foldables.
        SizeChanged += (_, _) => Body.WidthRequest = Math.Min(560, Width);
        _store.EntitlementChanged += (_, _) => Refresh();
    }

    public SheetHost Sheets => SheetLayer;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeManager.ApplySaved();
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

            // Play Games v2 signs in on its own. This only reads the result.
            await _games.RefreshSignInAsync();
        }
    }

    protected override bool OnBackButtonPressed() => Sheets.HandleBack() || base.OnBackButtonPressed();

    private void Refresh()
    {
        var p = _progress.Progress;
        var today = Seeds.TodayUtc();
        Progression.EnsureMissions(p, today);

        int level = Progression.LevelForXp(p.Xp);
        LevelLabel.Text = level >= Progression.MaxLevel ? $"Level {level} (max)" : $"Level {level}";
        XpBar.Progress = Progression.LevelFraction(p.Xp);
        StreakLabel.Text = p.StreakDays > 0 ? $"{p.StreakDays} day streak" : "Start a streak today";

        DailySub.Text = Progression.HasPlayedDailyToday(p, today) ? $"Done today: {p.DailyScoreToday:N0}" : "Ranked try ready";
        p.Best.TryGetValue("weekly:" + Seeds.WeekKey(today), out int weeklyBest);
        WeeklySub.Text = weeklyBest > 0 ? $"Best this week {weeklyBest:N0}" : "New board every week";
        RetroSub.Text = $"{Arenas.RetroName((RetroVariant)_settings.RetroVariant)}, level {_settings.RetroLevel}";

        MissionsList.Children.Clear();
        foreach (var m in p.Missions)
        {
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
            var text = new Label { Text = m.Text, FontSize = 15 };
            text.SetDynamicResource(Label.TextColorProperty, m.Done ? "Accent" : "Text");
            var state = new Label { Text = m.Done ? "Done" : $"{m.Progress}/{m.Target}  +{m.RewardXp} XP", FontSize = 13, VerticalOptions = LayoutOptions.Center };
            state.SetDynamicResource(Label.TextColorProperty, m.Done ? "Accent" : "Muted");
            row.Add(text);
            row.Add(state, 1);
            MissionsList.Children.Add(row);
        }

        p.Best.TryGetValue("endless", out int endless);
        p.Best.TryGetValue("daily", out int daily);
        BestLabel.Text = $"Endless {endless:N0}   .   Daily {daily:N0}   .   {p.TotalRuns:N0} runs";

        RemoveAdsButton.IsVisible = !_store.IsAdFree;
        RemoveAdsButton.Text = _store.RemoveAdsPrice is { } price ? $"Remove ads {price}" : "Remove ads";
    }

    private Task Start(GameSetup? setup, string bestKey) => Navigation.PushAsync(new GamePage(setup, bestKey), animated: false);

    private void OnPlay(object? sender, EventArgs e) => _ = Start(null, "endless");

    private void OnDaily(object? sender, TappedEventArgs e) => _ = Start(Arenas.DailyRun(Seeds.TodayUtc()), "daily");

    private void OnWeekly(object? sender, TappedEventArgs e)
    {
        var today = Seeds.TodayUtc();
        _ = Start(Arenas.WeeklyArena(today), "weekly:" + Seeds.WeekKey(today));
    }

    private async void OnRetro(object? sender, TappedEventArgs e)
    {
        int variant = _settings.RetroVariant;
        int level = _settings.RetroLevel;
        bool purist = _settings.Purist;
        var names = Enum.GetValues<RetroVariant>().Select(Arenas.RetroName).ToList();

        bool play = await Sheets.ShowAsync<bool>("Retro 97", "The 1997 phone rules: fixed speed, critters and mazes.", h =>
        [
            Kit.Section("Board"),
            Kit.ChipGroup(names, variant, i => variant = i),
            Kit.Section("Speed"),
            Kit.ChipGroup(Enumerable.Range(1, 9).Select(l => l.ToString()).ToList(), level - 1, i => level = i + 1),
            Kit.Section("Style"),
            Kit.ChipGroup(["Classic", "Purist"], purist ? 1 : 0, i => purist = i == 1),
            Kit.Primary("Play", (_, _) => h.Close(true)),
        ]);

        if (!play)
        {
            return;
        }

        _settings.RetroVariant = variant;
        _settings.RetroLevel = level;
        _settings.Purist = purist;
        var v = (RetroVariant)variant;
        var seed = (uint)Random.Shared.Next() & Seeds.SeedMask;
        await Start(Arenas.Retro(seed, level, v, purist), $"retro:{v}:{level}:{(purist ? "purist" : "classic")}");
    }

    private async void OnChallenge(object? sender, TappedEventArgs e)
    {
        string? code = await Dialogs.PromptAsync("Challenge code", "Type the 8 character code your friend shared.", "K7Q2M9XA", "Play", maxLength: 10);
        if (code is null)
        {
            return;
        }

        if (!ChallengeCode.TryDecode(code, out var setup))
        {
            await Dialogs.AlertAsync("Code not found", "That code does not look right. Check the letters and try again.");
            return;
        }

        await Start(setup, "challenge:" + ChallengeCode.Encode(setup));
    }

    private void OnBattle(object? sender, TappedEventArgs e) => _ = Navigation.PushAsync(new BattlePage(), animated: false);

    private async void OnLeaderboards(object? sender, EventArgs e)
    {
        if (!_games.IsConfigured)
        {
            await Dialogs.AlertAsync("Leaderboards", "Google Play leaderboards switch on once the game is published with its Play Games ID. Until then your best scores are kept on this phone.");
            return;
        }

        await _games.ShowLeaderboardAsync();
    }

    private void OnSettings(object? sender, EventArgs e) => _ = Navigation.PushAsync(new SettingsPage(), animated: false);

    private async void OnRemoveAds(object? sender, EventArgs e)
    {
        await RemoveAdsFlow.RunAsync();
        Refresh();
    }
}
