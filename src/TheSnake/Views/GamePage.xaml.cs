using TheSnake.Core;
using TheSnake.Rendering;
using TheSnake.Services;
using TheSnake.Ui;
#if ANDROID
using TheSnake.Platforms.Android;
#endif

namespace TheSnake.Views;

public partial class GamePage : ContentPage, IHasSheetHost
{
    private enum State
    {
        WaitingForSize,
        Ready,
        Running,
        Paused,
        Countdown,
        Dying,
        Over,
    }

    /// <summary>A swipe counts after this many dp. Small, so turns register early in the gesture.</summary>
    private const double SwipeDistance = 14;
    private const double EdgeDeadZone = 24;
    private const int DeathEffectMs = 600;

    /// <summary>
    /// If a turn arrives once the head is at least this far into its next cell, that move is finished
    /// at once and the turn starts straight away, instead of waiting for the next tick.
    /// </summary>
    private const double CommitMoveAt = 0.45;

    private static bool s_offeredRemoveAdsThisSession;
    private static double s_sessionPlayMs;
    private static bool s_breakShown;

    private readonly GameSetup? _fixedSetup;
    private readonly string _bestKey;
    private readonly AppSettings _settings = ServiceHelper.Get<AppSettings>();
    private readonly ProgressStore _progress = ServiceHelper.Get<ProgressStore>();
    private readonly Feedback _feedback = ServiceHelper.Get<Feedback>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly IFrameTicker _ticker = ServiceHelper.Get<IFrameTicker>();
    private readonly IGamesService _games = ServiceHelper.Get<IGamesService>();
    private readonly GameDrawable _drawable = new();
    private GameEngine? _engine;
    private State _state = State.WaitingForSize;
    private double _accumulatorMs;
    private double _lastFrameMs = -1;
    private double _stateMs;
    private int _countdown;
    private double _toastMs;
    private PointF? _swipeAnchor;
    private bool _swipeBlocked;

    public GamePage(GameSetup? setup, string bestKey)
    {
        InitializeComponent();
        _fixedSetup = setup;
        _bestKey = bestKey;
        Arena.Drawable = _drawable;

        // Listen to the arena itself, not the page. The page gets its size before its children are
        // arranged, so at that moment the arena still has no size and the run would never start.
        Arena.SizeChanged += (_, _) => OnSizeChanged();
        SizeChanged += (_, _) => OnSizeChanged();
    }

    public SheetHost Sheets => SheetLayer;

    private bool IsRetro => _engine?.Setup.Rules == RuleSet.Retro || _fixedSetup?.Rules == RuleSet.Retro;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        App.WentToBackground += OnBackground;
#if ANDROID
        KeyInput.Handler = OnKey;
#endif
        ApplyLook();
        UpdateRemoveAdsButtons();

        // Safety net in case no size event reaches us after layout.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), OnSizeChanged);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        App.WentToBackground -= OnBackground;
#if ANDROID
        KeyInput.Handler = null;
#endif
        _ticker.Stop();
    }

    protected override bool OnBackButtonPressed()
    {
        if (Sheets.HandleBack())
        {
            return true;
        }

        if (_state is State.Running or State.Countdown)
        {
            Pause();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void ApplyLook()
    {
        _drawable.Theme = Theme.Get(IsRetro ? _settings.RetroTheme : _settings.Theme);
        _drawable.Look = _settings.Look;
        _drawable.ReduceMotion = _settings.ReduceMotion;
        DPad.IsVisible = _settings.Controls == ControlScheme.DPad;
        ComboRow.IsVisible = !IsRetro;
        ComboBar.IsVisible = !IsRetro;
        Arena.Invalidate();
    }

    private void OnSizeChanged()
    {
        if (Arena.Width <= 0 || Arena.Height <= 0)
        {
            return;
        }

        // Overlay cards: never wider than 400 and always at least 20 dp from the arena edges, centred by margin.
        var cardMargin = new Thickness(Math.Max(20, (Arena.Width - 400) / 2), 0);
        PauseCard.Margin = cardMargin;
        OverCard.Margin = cardMargin;

        bool landscape = Arena.Width > Arena.Height;
        if (_state == State.WaitingForSize)
        {
            NewGame(landscape);
            return;
        }

        if (landscape != _drawable.Landscape)
        {
            _drawable.Landscape = landscape;
            Pause();
        }

        CheckDPadFits();
        Arena.Invalidate();
    }

    private void NewGame(bool? landscape = null)
    {
        _drawable.Landscape = landscape ?? _drawable.Landscape;
        var setup = _fixedSetup ?? FitToScreenEndless();
        _engine = new GameEngine(setup);
        _drawable.Engine = _engine;
        _drawable.DeathProgress = 0;
        _drawable.Interpolation = 0;
        _accumulatorMs = 0;
        PausePanel.IsVisible = false;
        OverPanel.IsVisible = false;
        CountdownLabel.IsVisible = false;
        OverTitle.Text = "Game over";
        _progress.Progress.Best.TryGetValue(_bestKey, out int best);
        BestLabel.Text = $"Best {best:N0}";
        CheckDPadFits();
        ShowReadyHint();
        SetState(State.Ready);
        UpdateHud();
        Arena.Invalidate();
    }

    /// <summary>Endless uses all of a big screen: about 16 dp per cell, at least 18 by 26 cells (spec 4.1).</summary>
    private GameSetup FitToScreenEndless()
    {
        double w = Arena.Width;
        double h = Arena.Height;
        if (_drawable.Landscape)
        {
            (w, h) = (h, w);
        }

        int cols = Math.Max(18, (int)(w / 16));
        int rows = Math.Max(26, (int)(h / 16));
        return Arenas.Endless((uint)Random.Shared.Next() & Seeds.SeedMask, cols, rows);
    }

    private void CheckDPadFits()
    {
        if (_settings.Controls != ControlScheme.DPad || _engine is null)
        {
            return;
        }

        int cols = _drawable.Landscape ? _engine.Setup.Height : _engine.Setup.Width;
        int rows = _drawable.Landscape ? _engine.Setup.Width : _engine.Setup.Height;
        double cell = Math.Min(Arena.Width / cols, Arena.Height / rows);
        if (cell < 10 && DPad.IsVisible)
        {
            DPad.IsVisible = false;
            ShowToast("Not enough room for the D pad. Swipe instead", 2500);
        }
    }

    private void ShowReadyHint()
    {
        string how = _settings.Controls switch
        {
            ControlScheme.ThumbTurns => "Tap left or right to turn",
            ControlScheme.DPad => "Press an arrow to start",
            _ => "Swipe to start",
        };

        if (!_settings.SeenFirstRunHint)
        {
            how += "\nEat, chain combos, dodge your tail.\nPause any time with II or Back.";
        }

        if (_settings.Controls == ControlScheme.Swipe && !_settings.SeenEdgeSwipeTip)
        {
            how += "\nTip: swipe from the middle of the screen.";
            _settings.SeenEdgeSwipeTip = true;
        }

        HintLabel.Text = how;
        HintCard.IsVisible = true;
    }

    private void SetState(State state)
    {
        _state = state;
        _stateMs = 0;
        if (state is State.Running or State.Countdown or State.Dying || _toastMs > 0)
        {
            StartTicker();
        }
        else
        {
            // Nothing moves while paused or in menus, so stop drawing to save battery.
            _ticker.Stop();
        }
    }

    private void StartTicker()
    {
        _lastFrameMs = -1;
        _ticker.Start(OnFrame);
    }

    private void OnFrame(double nowMs)
    {
        if (_engine is null)
        {
            return;
        }

        double dt = _lastFrameMs < 0 ? 0 : Math.Clamp(nowMs - _lastFrameMs, 0, 100);
        _lastFrameMs = nowMs;
        _stateMs += dt;
        _drawable.ClockMs += dt;
        _drawable.Advance((float)dt);

        if (_toastMs > 0)
        {
            _toastMs -= dt;
            if (_toastMs <= 0)
            {
                Toast.IsVisible = false;
            }
        }

        switch (_state)
        {
            case State.Running:
                s_sessionPlayMs += dt;
                _accumulatorMs += dt;
                while (_accumulatorMs >= _engine.TickLengthMs && _state == State.Running)
                {
                    _accumulatorMs -= _engine.TickLengthMs;
                    HandleEvents(_engine.Tick());
                }

                _drawable.Interpolation = (float)(_accumulatorMs / _engine.TickLengthMs);
                UpdateHud();
                break;

            case State.Countdown:
                int left = 3 - (int)(_stateMs / 1000);
                if (left <= 0)
                {
                    CountdownLabel.IsVisible = false;
                    SetState(State.Running);
                }
                else if (left != _countdown)
                {
                    _countdown = left;
                    CountdownLabel.Text = left.ToString();
                    _feedback.Play(Sfx.Click);
                }

                break;

            case State.Dying:
                _drawable.DeathProgress = (float)Math.Min(1, _stateMs / DeathEffectMs);
                if (_stateMs >= DeathEffectMs)
                {
                    ShowGameOver();
                }

                break;

            default:
                if (_toastMs <= 0)
                {
                    _ticker.Stop();
                }

                break;
        }

        Arena.Invalidate();
    }

    private void HandleEvents(TickEvents events)
    {
        if (_engine is null || events == TickEvents.None)
        {
            return;
        }

        var theme = _drawable.Theme;
        if (events.HasFlag(TickEvents.Ate))
        {
            _feedback.Play(Sfx.Eat);
            _feedback.Tick();
            _drawable.Burst(_engine.Head, theme.Food);
        }

        if (events.HasFlag(TickEvents.AteGolden))
        {
            _feedback.Play(Sfx.Golden);
            _feedback.Buzz(20);
            _drawable.Burst(_engine.Head, theme.Golden);
            ShowToast("Golden!", 700);
        }

        if (events.HasFlag(TickEvents.ComboUp))
        {
            ShowToast($"Combo x{_engine.Combo}", 600);
        }

        if (events.HasFlag(TickEvents.NearMiss))
        {
            _feedback.Play(Sfx.NearMiss);
            _feedback.Tick();
            ShowToast("Close!  +5", 600);
        }

        if (events.HasFlag(TickEvents.PowerUpTaken))
        {
            _feedback.Play(Sfx.PowerUp);
            _feedback.Buzz(20);
            ShowToast(_engine.ActivePower is { } p ? PowerName(p.Kind) : "Trim", 800);
        }

        if (events.HasFlag(TickEvents.CritterSpawned))
        {
            _feedback.Play(Sfx.Critter);
        }

        if (events.HasFlag(TickEvents.CritterEaten))
        {
            _feedback.Play(Sfx.Golden);
            _feedback.Buzz(20);
        }

        if (events.HasFlag(TickEvents.Died))
        {
            _feedback.Play(Sfx.Death);
            _feedback.Buzz(300);
            SetState(State.Dying);
        }
        else if (events.HasFlag(TickEvents.Won))
        {
            _feedback.Play(Sfx.LevelUp);
            ShowGameOver();
            OverTitle.Text = "Well done!";
        }
    }

    private static string PowerName(PowerUpKind kind) => kind switch
    {
        PowerUpKind.SlowTime => "Slow time",
        PowerUpKind.Phase => "Phase",
        PowerUpKind.Magnet => "Magnet",
        PowerUpKind.Double => "Double points",
        _ => "Trim",
    };

    private void UpdateHud()
    {
        if (_engine is null)
        {
            return;
        }

        ScoreLabel.Text = _engine.Score.ToString("N0");
        ComboLabel.Text = $"x{_engine.Combo}";
        ComboBar.Progress = (double)_engine.ComboWindowLeftMs / GameEngine.ComboWindowMs;

        if (_engine.ActivePower is { } power)
        {
            PowerRow.IsVisible = true;
            PowerLabel.Text = PowerName(power.Kind);
            PowerBar.Progress = (double)power.RemainingMs / power.TotalMs;
        }
        else if (_engine.Critter is { } critter)
        {
            PowerRow.IsVisible = true;
            PowerLabel.Text = $"Critter {critter.TicksLeft}";
            PowerBar.Progress = (double)critter.TicksLeft / GameEngine.CritterLifeTicks;
        }
        else
        {
            PowerRow.IsVisible = false;
        }
    }

    private void ShowToast(string text, int ms)
    {
        ToastLabel.Text = text;
        Toast.IsVisible = true;
        _toastMs = ms;
        if (!_ticker.IsRunning)
        {
            StartTicker();
        }
    }

    /// <summary>
    /// Applies a turn as fast as possible. The renderer already shows the snake moving into its next
    /// cell, so a queued turn is visible on the next frame. If the head is already most of the way
    /// into that cell, the move is finished now and the turn begins immediately.
    /// </summary>
    private void ApplyTurn(Direction direction)
    {
        if (_engine is null)
        {
            return;
        }

        if (_state == State.Ready)
        {
            StartRunning();
        }

        if (_state == State.Paused && IsRetro && _settings.Purist)
        {
            // Purist resumes on the first direction, with no countdown.
            PausePanel.IsVisible = false;
            SetState(State.Running);
        }

        if (_state is not (State.Running or State.Countdown))
        {
            return;
        }

        bool isRealTurn = direction != _engine.Heading && direction != _engine.Heading.Opposite();
        if (_state == State.Running && _engine.TurnQueueEmpty && isRealTurn
            && _accumulatorMs >= _engine.TickLengthMs * CommitMoveAt)
        {
            _accumulatorMs = 0;
            HandleEvents(_engine.Tick());
            if (_state != State.Running)
            {
                Arena.Invalidate();
                return;
            }
        }

        if (_engine.QueueTurn(direction))
        {
            _drawable.Interpolation = (float)(_accumulatorMs / _engine.TickLengthMs);
            Arena.Invalidate();
        }
    }

    private void Steer(Direction screenDirection) =>
        ApplyTurn(_drawable.Landscape ? screenDirection.FromLandscapeScreen() : screenDirection);

    private void TurnRelative(bool right)
    {
        if (_engine is null)
        {
            return;
        }

        var basis = _engine.TurnQueueEmpty ? _engine.Heading : _engine.NextHeading;
        ApplyTurn(right ? basis.TurnRight() : basis.TurnLeft());
    }

    private void StartRunning()
    {
        HintCard.IsVisible = false;
        _settings.SeenFirstRunHint = true;
        _accumulatorMs = 0;
        SetState(State.Running);
    }

    private void Pause()
    {
        if (_state is not (State.Running or State.Countdown))
        {
            return;
        }

        CountdownLabel.IsVisible = false;
        PausePanel.IsVisible = true;
        UpdateRemoveAdsButtons();
        SetState(State.Paused);
        Arena.Invalidate();
    }

    private void OnBackground(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Pause);

    private void OnPauseClicked(object? sender, EventArgs e)
    {
        if (_state == State.Paused)
        {
            OnContinue(sender, e);
        }
        else
        {
            Pause();
        }
    }

    private void OnContinue(object? sender, EventArgs e)
    {
        if (_state != State.Paused)
        {
            return;
        }

        PausePanel.IsVisible = false;
        if (IsRetro && _settings.Purist)
        {
            SetState(State.Running);
            return;
        }

        _countdown = 0;
        CountdownLabel.Text = "3";
        CountdownLabel.IsVisible = true;
        SetState(State.Countdown);
    }

    private void OnRestart(object? sender, EventArgs e) => NewGame();

    private async void OnHome(object? sender, EventArgs e)
    {
        _ticker.Stop();
        await Navigation.PopAsync(animated: false);
    }

    private async void OnRemoveAds(object? sender, EventArgs e)
    {
        await RemoveAdsFlow.RunAsync();
        UpdateRemoveAdsButtons();
    }

    private void UpdateRemoveAdsButtons()
    {
        PauseRemoveAds.IsVisible = !_store.IsAdFree;
        AdGap.IsVisible = !_store.IsAdFree;
    }

    private async void OnShare(object? sender, EventArgs e)
    {
        if (_engine is null)
        {
            return;
        }

        string mode = ModeName(_engine.Setup);
        string text = CanShareCode(_engine.Setup)
            ? $"Beat my {_engine.Score:N0} on The Snake ({mode}). Code {ChallengeCode.Encode(_engine.Setup)}"
            : $"I scored {_engine.Score:N0} on The Snake ({mode}).";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Share your score", Text = text });
    }

    private void ShowGameOver()
    {
        if (_engine is null)
        {
            return;
        }

        var today = Seeds.TodayUtc();
        var run = new RunSummary(_engine.Setup.Mode, _bestKey, _engine.Score, _engine.FoodsEaten,
            _engine.MaxComboReached, _engine.PowerUpsTaken, _engine.NearMisses);
        var reward = Progression.ApplyRun(_progress.Progress, run, today);
        _progress.Save();

        OverScore.Text = _engine.Score.ToString("N0");
        var lines = new List<string>
        {
            reward.NewBest ? "New best!" : $"Best {Math.Max(reward.PreviousBest, _engine.Score):N0}",
            $"+{reward.XpGained} XP",
        };

        if (reward.NewLevel > reward.OldLevel)
        {
            lines.Add($"Level up! You are now level {reward.NewLevel}");
            _feedback.Play(Sfx.LevelUp);
        }

        lines.AddRange(reward.Unlocked.Select(t => $"New theme unlocked: {t}"));
        lines.AddRange(reward.MissionsCompleted.Select(m => $"Mission done: {m.Text}"));
        if (_engine.Setup.Mode == GameMode.DailyRun)
        {
            lines.Add(reward.RankedDaily ? $"Today's ranked Daily Run. Streak {_progress.Progress.StreakDays}" : "Practice run (today's ranked try is used)");
        }

        OverDetails.Text = string.Join("\n", lines);
        bool code = CanShareCode(_engine.Setup);
        CodeCard.IsVisible = code;
        OverCode.Text = code ? $"CODE  {ChallengeCode.Encode(_engine.Setup)}" : "";

        // Google Play Games leaderboards. Only plausible scores are sent, and only today's first Daily Run.
        var board = IGamesService.BoardFor(_engine.Setup.Mode);
        bool ranked = board is not null && (_engine.Setup.Mode != GameMode.DailyRun || reward.RankedDaily);
        if (board is { } b && ranked && _games.IsConfigured && GameEngine.IsPlausibleScore(_engine.Score, _engine.Ticks))
        {
            _games.Submit(b, _engine.Score);
            _ = ShowRankAsync(b);
        }

        OverRank.IsVisible = false;
        LeaderboardButton.IsVisible = board is not null && _games.IsConfigured;

        bool offer = !_store.IsAdFree && !s_offeredRemoveAdsThisSession;
        OverRemoveAds.IsVisible = offer;
        s_offeredRemoveAdsThisSession |= offer;

        OverPanel.IsVisible = true;
        SetState(State.Over);
        BestLabel.Text = $"Best {Math.Max(reward.PreviousBest, _engine.Score):N0}";

        if (_settings.BreakReminderMinutes > 0 && !s_breakShown && s_sessionPlayMs > _settings.BreakReminderMinutes * 60_000)
        {
            s_breakShown = true;
            _ = Dialogs.AlertAsync("Time for a break?", $"You have played for over {_settings.BreakReminderMinutes} minutes. Your progress is saved.", "Got it");
        }
    }

    private async Task ShowRankAsync(Board board)
    {
        // Give Play Games a moment to take the new score before asking for the rank.
        await Task.Delay(1500);
        if (await _games.GetRankTextAsync(board) is { } rank && _state == State.Over)
        {
            OverRank.Text = rank;
            OverRank.IsVisible = true;
        }
    }

    private async void OnLeaderboard(object? sender, EventArgs e)
    {
        if (_engine is not null && IGamesService.BoardFor(_engine.Setup.Mode) is { } board)
        {
            await _games.ShowLeaderboardAsync(board);
        }
    }

    private static bool CanShareCode(GameSetup setup) =>
        setup.Rules == RuleSet.Retro || (setup.Width == Arenas.StandardWidth && setup.Height == Arenas.StandardHeight);

    private static string ModeName(GameSetup setup) => setup.Mode switch
    {
        GameMode.DailyRun => "Daily Run",
        GameMode.WeeklyArena => "Weekly Arena",
        GameMode.Retro97 => $"Retro 97, {Arenas.RetroName(setup.Variant)}, level {setup.RetroLevel}",
        GameMode.Challenge => "Challenge",
        _ => "Endless",
    };

    // Touch input. Swipes and thumb taps start only inside the arena and away from the side edges and
    // the bottom 24 dp, so they never clash with the back gesture or the ad strip (spec 6.1 and 9.1).
    // Everything reacts on touch down or during the drag, never on release.
    private void OnTouchStart(object? sender, TouchEventArgs e)
    {
        var p = e.Touches.FirstOrDefault();
        _swipeBlocked = p.X < EdgeDeadZone || p.X > Arena.Width - EdgeDeadZone || p.Y > Arena.Height - EdgeDeadZone;
        _swipeAnchor = _swipeBlocked ? null : p;

        if (!_swipeBlocked && _settings.Controls == ControlScheme.ThumbTurns)
        {
            TurnRelative(p.X > Arena.Width / 2);
        }
    }

    private void OnTouchMove(object? sender, TouchEventArgs e)
    {
        if (_swipeAnchor is not { } anchor || _settings.Controls == ControlScheme.ThumbTurns)
        {
            return;
        }

        var p = e.Touches.FirstOrDefault();
        double dx = p.X - anchor.X;
        double dy = p.Y - anchor.Y;
        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) < SwipeDistance)
        {
            return;
        }

        Steer(Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? Direction.Right : Direction.Left) : (dy > 0 ? Direction.Down : Direction.Up));
        _swipeAnchor = p;
    }

    private void OnTouchEnd(object? sender, TouchEventArgs e) => _swipeAnchor = null;

    private void OnPadUp(object? sender, EventArgs e) => PadPress(Direction.Up);

    private void OnPadDown(object? sender, EventArgs e) => PadPress(Direction.Down);

    private void OnPadLeft(object? sender, EventArgs e) => PadPress(Direction.Left);

    private void OnPadRight(object? sender, EventArgs e) => PadPress(Direction.Right);

    private void PadPress(Direction direction)
    {
        _feedback.Tick();
        Steer(direction);
    }

#if ANDROID
    private bool OnKey(GameKey key)
    {
        if (Sheets.IsOpen)
        {
            return false;
        }

        switch (key)
        {
            case GameKey.Up: Steer(Direction.Up); return true;
            case GameKey.Down: Steer(Direction.Down); return true;
            case GameKey.Left: Steer(Direction.Left); return true;
            case GameKey.Right: Steer(Direction.Right); return true;
            case GameKey.Pause:
                OnPauseClicked(null, EventArgs.Empty);
                return true;
            case GameKey.Select when _state == State.Over:
                OnRestart(null, EventArgs.Empty);
                return true;
            case GameKey.Select when _state == State.Paused:
                OnContinue(null, EventArgs.Empty);
                return true;
            default:
                return false;
        }
    }
#endif
}
