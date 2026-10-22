using System.Diagnostics;
using TheSnake.Core;
using TheSnake.Rendering;
using TheSnake.Services;
#if ANDROID
using TheSnake.Platforms.Android;
#endif

namespace TheSnake.Views;

public partial class GamePage : ContentPage
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

    private const double SwipeDistance = 24;
    private const double EdgeDeadZone = 24;
    private const int DeathEffectMs = 600;

    private static bool s_offeredRemoveAdsThisSession;
    private static double s_sessionPlayMs;
    private static bool s_breakShown;

    private readonly GameSetup? _fixedSetup;
    private readonly string _bestKey;
    private readonly AppSettings _settings = ServiceHelper.Get<AppSettings>();
    private readonly ProgressStore _progress = ServiceHelper.Get<ProgressStore>();
    private readonly Feedback _feedback = ServiceHelper.Get<Feedback>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly GameDrawable _drawable = new();
    private readonly Stopwatch _clock = new();
    private IDispatcherTimer? _frameTimer;
    private GameEngine? _engine;
    private State _state = State.WaitingForSize;
    private double _accumulatorMs;
    private double _lastFrameMs;
    private double _stateMs;
    private int _countdown;
    private int _toastMs;
    private PointF? _swipeAnchor;
    private bool _swipeBlocked;
    private bool _swipeMoved;

    public GamePage(GameSetup? setup, string bestKey)
    {
        InitializeComponent();
        _fixedSetup = setup;
        _bestKey = bestKey;
        Arena.Drawable = _drawable;
        SizeChanged += (_, _) => OnSizeChanged();
    }

    private bool IsRetro => _engine?.Setup.Rules == RuleSet.Retro || _fixedSetup?.Rules == RuleSet.Retro;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        App.WentToBackground += OnBackground;
#if ANDROID
        KeyInput.Handler = OnKey;
#endif
        _frameTimer ??= CreateTimer();
        ApplyTheme();
        UpdateRemoveAdsButtons();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        App.WentToBackground -= OnBackground;
#if ANDROID
        KeyInput.Handler = null;
#endif
        _frameTimer?.Stop();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_state is State.Running or State.Countdown)
        {
            Pause();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private IDispatcherTimer CreateTimer()
    {
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(16);
        timer.IsRepeating = true;
        timer.Tick += (_, _) => OnFrame();
        return timer;
    }

    private void ApplyTheme()
    {
        var theme = Theme.Get(IsRetro ? _settings.RetroTheme : _settings.Theme);
        _drawable.Theme = theme;
        _drawable.ReduceMotion = _settings.ReduceMotion;
        BackgroundColor = theme.Background;
        ScoreLabel.TextColor = theme.Text;
        DPad.IsVisible = _settings.Controls == ControlScheme.DPad;
        bool modern = !IsRetro;
        ComboLabel.IsVisible = modern;
        ComboBar.IsVisible = modern;
    }

    private void OnSizeChanged()
    {
        if (Arena.Width <= 0 || Arena.Height <= 0)
        {
            return;
        }

        bool landscape = Arena.Width > Arena.Height;
        if (_state == State.WaitingForSize)
        {
            NewGame(landscape);
            return;
        }

        if (landscape != _drawable.Landscape)
        {
            _drawable.Landscape = landscape;
            if (_state is State.Running or State.Countdown)
            {
                Pause();
            }
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
        _drawable.Interpolation = 1;
        _accumulatorMs = 0;
        PausePanel.IsVisible = false;
        OverPanel.IsVisible = false;
        CountdownLabel.IsVisible = false;
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
            ShowToast("Turn the phone sideways or use swipes, the D pad needs more room", 2500);
        }
    }

    private void ShowReadyHint()
    {
        string how = _settings.Controls switch
        {
            ControlScheme.ThumbTurns => "Tap the left or right half to turn",
            ControlScheme.DPad => "Press an arrow to start",
            _ => "Swipe to start",
        };

        if (!_settings.SeenFirstRunHint)
        {
            how += "\nEat food, chain combos, avoid your tail.\nPause any time with II or the back gesture.";
        }

        if (_settings.Controls == ControlScheme.Swipe && !_settings.SeenEdgeSwipeTip)
        {
            how += "\nTip: swipe from the middle of the screen.";
            _settings.SeenEdgeSwipeTip = true;
        }

        HintLabel.Text = how;
        HintLabel.IsVisible = true;
    }

    private void SetState(State state)
    {
        _state = state;
        _stateMs = 0;
        bool animate = state is State.Running or State.Countdown or State.Dying || _toastMs > 0;
        if (animate)
        {
            _clock.Restart();
            _lastFrameMs = 0;
            _frameTimer?.Start();
        }
        else
        {
            // Nothing moves while paused or in menus, so stop drawing to save battery.
            _frameTimer?.Stop();
        }
    }

    private void OnFrame()
    {
        if (_engine is null)
        {
            return;
        }

        double now = _clock.Elapsed.TotalMilliseconds;
        double dt = Math.Min(100, now - _lastFrameMs);
        _lastFrameMs = now;
        _stateMs += dt;
        _drawable.ClockMs += dt;
        _drawable.Advance((float)dt);

        if (_toastMs > 0)
        {
            _toastMs -= (int)dt;
            if (_toastMs <= 0)
            {
                ToastLabel.IsVisible = false;
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

                _drawable.Interpolation = _state == State.Running ? (float)(_accumulatorMs / _engine.TickLengthMs) : 1;
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
                    _frameTimer?.Stop();
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
            string name = _engine.ActivePower?.Kind switch
            {
                PowerUpKind.SlowTime => "Slow time",
                PowerUpKind.Phase => "Phase",
                PowerUpKind.Magnet => "Magnet",
                PowerUpKind.Double => "Double points",
                _ => "Trim",
            };
            ShowToast(name, 800);
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
            _drawable.Interpolation = 1;
            SetState(State.Dying);
        }
        else if (events.HasFlag(TickEvents.Won))
        {
            _feedback.Play(Sfx.LevelUp);
            OverTitle.Text = "Well done!";
            ShowGameOver();
        }
    }

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
            PowerLabel.Text = power.Kind switch
            {
                PowerUpKind.SlowTime => "Slow time",
                PowerUpKind.Phase => "Phase",
                PowerUpKind.Magnet => "Magnet",
                _ => "Double",
            };
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
        ToastLabel.IsVisible = true;
        _toastMs = ms;
        if (_frameTimer is { IsRunning: false })
        {
            _clock.Restart();
            _lastFrameMs = 0;
            _frameTimer.Start();
        }
    }

    private void Steer(Direction screenDirection)
    {
        if (_engine is null)
        {
            return;
        }

        var direction = _drawable.Landscape ? screenDirection.FromLandscapeScreen() : screenDirection;
        if (_state == State.Ready)
        {
            StartRunning();
        }

        if (_state is State.Running or State.Countdown)
        {
            _engine.QueueTurn(direction);
        }
        else if (_state == State.Paused && IsRetro && _settings.Purist)
        {
            // Purist resumes on the first direction, with no countdown.
            _engine.QueueTurn(direction);
            PausePanel.IsVisible = false;
            SetState(State.Running);
        }
    }

    private void TurnRelative(bool right)
    {
        if (_state == State.Ready)
        {
            StartRunning();
        }

        if (_state is State.Running or State.Countdown)
        {
            _engine?.QueueRelativeTurn(right);
        }
    }

    private void StartRunning()
    {
        HintLabel.IsVisible = false;
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
        _drawable.Interpolation = 1;
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

    private void OnRestart(object? sender, EventArgs e)
    {
        OverTitle.Text = "Game over";
        NewGame();
    }

    private async void OnHome(object? sender, EventArgs e)
    {
        _frameTimer?.Stop();
        await Navigation.PopAsync();
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

        lines.AddRange(reward.Unlocked.Select(t => $"Unlocked theme: {t}"));
        lines.AddRange(reward.MissionsCompleted.Select(m => $"Mission done: {m.Text}"));
        if (_engine.Setup.Mode == GameMode.DailyRun)
        {
            lines.Add(reward.RankedDaily ? $"Today's ranked Daily Run. Streak {_progress.Progress.StreakDays}" : "Practice run (today's ranked try is used)");
        }

        OverDetails.Text = string.Join("\n", lines);
        OverCode.Text = CanShareCode(_engine.Setup) ? $"Challenge code  {ChallengeCode.Encode(_engine.Setup)}" : "";
        OverCode.IsVisible = OverCode.Text.Length > 0;

        bool offer = !_store.IsAdFree && !s_offeredRemoveAdsThisSession;
        OverRemoveAds.IsVisible = offer;
        s_offeredRemoveAdsThisSession |= offer;

        OverPanel.IsVisible = true;
        PlayAgainButton.Focus();
        SetState(State.Over);
        BestLabel.Text = $"Best {Math.Max(reward.PreviousBest, _engine.Score):N0}";

        if (_settings.BreakReminderMinutes > 0 && !s_breakShown && s_sessionPlayMs > _settings.BreakReminderMinutes * 60_000)
        {
            s_breakShown = true;
            _ = DisplayAlertAsync("Take a break?", $"You have played for over {_settings.BreakReminderMinutes} minutes. Your progress is saved.", "OK");
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
    private void OnTouchStart(object? sender, TouchEventArgs e)
    {
        var p = e.Touches.FirstOrDefault();
        _swipeMoved = false;
        _swipeBlocked = p.X < EdgeDeadZone || p.X > Arena.Width - EdgeDeadZone || p.Y > Arena.Height - EdgeDeadZone;
        _swipeAnchor = _swipeBlocked ? null : p;
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

        _swipeMoved = true;
        Steer(Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? Direction.Right : Direction.Left) : (dy > 0 ? Direction.Down : Direction.Up));
        _swipeAnchor = p;
    }

    private void OnTouchEnd(object? sender, TouchEventArgs e)
    {
        if (_swipeBlocked || _swipeMoved)
        {
            return;
        }

        var p = e.Touches.FirstOrDefault();
        if (_settings.Controls == ControlScheme.ThumbTurns)
        {
            TurnRelative(p.X > Arena.Width / 2);
        }
    }

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
