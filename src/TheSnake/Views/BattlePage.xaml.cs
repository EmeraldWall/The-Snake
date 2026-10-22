using TheSnake.Core;
using TheSnake.Core.Battle;
using TheSnake.Rendering;
using TheSnake.Services;
using TheSnake.Ui;
#if ANDROID
using TheSnake.Platforms.Android;
#endif

namespace TheSnake.Views;

/// <summary>
/// Nearby Battle (spec 5.2). One phone hosts and runs the only <see cref="BattleEngine"/>; up to three others
/// join over Nearby Connections, send their turns and draw the state the host sends every tick.
/// </summary>
public partial class BattlePage : ContentPage, IHasSheetHost
{
    private enum Mode
    {
        Choose,
        Hosting,
        Browsing,
        Joining,
        Waiting,
        Countdown,
        Playing,
        Results,
    }

    private sealed record Player(string Name, string? EndpointId);

    private const double SwipeDistance = 14;
    private const double EdgeDeadZone = 24;
    private const int CountdownMs = 3000;

    private readonly INearbyService _nearby = ServiceHelper.Get<INearbyService>();
    private readonly AppSettings _settings = ServiceHelper.Get<AppSettings>();
    private readonly Feedback _feedback = ServiceHelper.Get<Feedback>();
    private readonly IFrameTicker _ticker = ServiceHelper.Get<IFrameTicker>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly BattleDrawable _drawable = new();
    private readonly Dictionary<string, string> _foundHosts = [];

    // Host side.
    private readonly List<Player> _players = [];
    private BattleEngine? _engine;

    // Player side.
    private string? _hostEndpoint;
    private List<string> _names = [];

    private Mode _mode = Mode.Choose;
    private bool _isHost;
    private int _myIndex;
    private double _accumulatorMs;
    private double _sinceStateMs;
    private double _countdownLeftMs;
    private double _lastFrameMs = -1;
    private PointF? _swipeAnchor;

    public BattlePage()
    {
        InitializeComponent();
        Arena.Drawable = _drawable;
        NameEntry.Text = Preferences.Default.Get("battle.name", "");
        SizeChanged += (_, _) =>
        {
            LobbyBody.WidthRequest = Math.Min(560, Width);
            ResultCard.WidthRequest = Math.Min(400, Width - 56);
        };
        Arena.SizeChanged += (_, _) =>
        {
            _drawable.Landscape = Arena.Width > Arena.Height;
            Arena.Invalidate();
        };
        SetMode(Mode.Choose);
    }

    public SheetHost Sheets => SheetLayer;

    private string MyName => BattleWire.CleanName(NameEntry.Text);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _nearby.HostFound += OnHostFound;
        _nearby.HostLost += OnHostLost;
        _nearby.Connected += OnConnected;
        _nearby.Disconnected += OnDisconnected;
        _nearby.Received += OnReceived;
#if ANDROID
        KeyInput.Handler = OnKey;
#endif
        _drawable.Theme = Theme.Get(_settings.Theme);
        AdGap.IsVisible = !_store.IsAdFree;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _nearby.HostFound -= OnHostFound;
        _nearby.HostLost -= OnHostLost;
        _nearby.Connected -= OnConnected;
        _nearby.Disconnected -= OnDisconnected;
        _nearby.Received -= OnReceived;
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

        OnBack(this, EventArgs.Empty);
        return true;
    }

    // Lobby -------------------------------------------------------------------------------------------

    private async Task<bool> EnsureReadyAsync()
    {
        if (!_nearby.IsSupported)
        {
            await Dialogs.AlertAsync("Not available", "Nearby Battle needs Google Play services on this phone.");
            return false;
        }

        if (!await _nearby.EnsurePermissionsAsync())
        {
            await Dialogs.AlertAsync("Permission needed", "Nearby Battle uses Bluetooth and Wi Fi to find phones in the same room. Allow nearby devices (and location on older Android versions) in the permission prompt, then try again.");
            return false;
        }

        Preferences.Default.Set("battle.name", MyName);
        return true;
    }

    private async void OnHost(object? sender, EventArgs e)
    {
        if (!await EnsureReadyAsync())
        {
            return;
        }

        _isHost = true;
        _players.Clear();
        _players.Add(new Player(MyName, null));
        if (!await _nearby.StartHostingAsync(MyName))
        {
            await Dialogs.AlertAsync("Could not host", "Turn on Bluetooth, Wi Fi and location, then try again.");
            return;
        }

        SetMode(Mode.Hosting);
    }

    private async void OnJoin(object? sender, EventArgs e)
    {
        if (!await EnsureReadyAsync())
        {
            return;
        }

        _isHost = false;
        _foundHosts.Clear();
        if (!await _nearby.StartBrowsingAsync())
        {
            await Dialogs.AlertAsync("Could not search", "Turn on Bluetooth, Wi Fi and location, then try again.");
            return;
        }

        SetMode(Mode.Browsing);
    }

    private void OnCancel(object? sender, EventArgs e) => Reset();

    private void OnHostFound(object? sender, (string EndpointId, string Name) host)
    {
        _foundHosts[host.EndpointId] = host.Name;
        if (_mode == Mode.Browsing)
        {
            RefreshLobby();
        }
    }

    private void OnHostLost(object? sender, string endpointId)
    {
        _foundHosts.Remove(endpointId);
        if (_mode == Mode.Browsing)
        {
            RefreshLobby();
        }
    }

    private async void JoinHost(string endpointId)
    {
        SetMode(Mode.Joining);
        if (!await _nearby.JoinAsync(endpointId, MyName))
        {
            await Dialogs.AlertAsync("Could not join", "That battle is no longer available, or it is full.");
            SetMode(Mode.Browsing);
        }
    }

    private void OnConnected(object? sender, (string EndpointId, string Name) link)
    {
        if (_isHost)
        {
            if (_mode != Mode.Hosting || _players.Count >= BattleEngine.MaxPlayers)
            {
                _nearby.Disconnect(link.EndpointId);
                return;
            }

            _players.Add(new Player(BattleWire.CleanName(link.Name), link.EndpointId));
            _feedback.Play(Sfx.Click);
            BroadcastLobby();
            RefreshLobby();
        }
        else if (_mode == Mode.Joining)
        {
            _hostEndpoint = link.EndpointId;
            _nearby.StopLooking();
            _nearby.Send(link.EndpointId, BattleWire.Hello(MyName));
            SetMode(Mode.Waiting);
        }
    }

    private void OnDisconnected(object? sender, string endpointId)
    {
        if (_isHost)
        {
            int index = _players.FindIndex(p => p.EndpointId == endpointId);
            if (index < 0)
            {
                return;
            }

            if (_mode is Mode.Countdown or Mode.Playing && _engine is not null)
            {
                // Their snake turns grey and stops; the match goes on.
                _engine.Drop(index);
                _players[index] = _players[index] with { EndpointId = null, Name = _players[index].Name + " (left)" };
                RefreshStrip();
            }
            else
            {
                _players.RemoveAt(index);
                BroadcastLobby();
                RefreshLobby();
            }
        }
        else if (endpointId == _hostEndpoint)
        {
            HostLeft();
        }
    }

    private void OnReceived(object? sender, (string EndpointId, byte[] Data) packet)
    {
        if (BattleWire.Read(packet.Data) is not { } message)
        {
            return;
        }

        if (_isHost)
        {
            int index = _players.FindIndex(p => p.EndpointId == packet.EndpointId);
            if (index < 0)
            {
                return;
            }

            switch (message.Type)
            {
                case MessageType.Hello when _mode == Mode.Hosting:
                    _players[index] = _players[index] with { Name = message.Name };
                    BroadcastLobby();
                    RefreshLobby();
                    break;
                case MessageType.Turn when _mode == Mode.Playing:
                    _engine?.QueueTurn(index, message.Turn);
                    break;
                case MessageType.Bye:
                    OnDisconnected(this, packet.EndpointId);
                    _nearby.Disconnect(packet.EndpointId);
                    break;
            }

            return;
        }

        if (packet.EndpointId != _hostEndpoint)
        {
            return;
        }

        switch (message.Type)
        {
            case MessageType.Lobby:
                _names = [.. message.Players ?? []];
                _myIndex = message.YourIndex;
                RefreshLobby();
                break;
            case MessageType.Start:
                _names = [.. message.Players ?? []];
                _myIndex = message.YourIndex;
                _drawable.Snapshot = null;
                BeginCountdown();
                break;
            case MessageType.State when message.State is { } state:
                _drawable.Snapshot = state;
                _drawable.LocalHeading = null;
                _sinceStateMs = 0;
                if (state.Status == BattleStatus.Over && _mode != Mode.Results)
                {
                    ShowResults(state);
                }

                RefreshStrip();
                break;
            case MessageType.Bye:
                HostLeft();
                break;
        }
    }

    private void BroadcastLobby()
    {
        var names = _players.Select(p => p.Name).ToList();
        for (int i = 1; i < _players.Count; i++)
        {
            if (_players[i].EndpointId is { } ep)
            {
                _nearby.Send(ep, BattleWire.Lobby(names, i));
            }
        }
    }

    private async void HostLeft()
    {
        _ticker.Stop();
        _nearby.Stop();
        _hostEndpoint = null;
        if (_mode is Mode.Countdown or Mode.Playing)
        {
            await Dialogs.AlertAsync("Host left", "The host closed the battle, so the match has ended.");
        }

        Reset();
    }

    // Match -------------------------------------------------------------------------------------------

    private void OnStart(object? sender, EventArgs e) => StartMatch();

    private void OnRematch(object? sender, EventArgs e) => StartMatch();

    private void StartMatch()
    {
        if (!_isHost)
        {
            return;
        }

        // Players who left during the last match are not in the rematch.
        var stillHere = _players.Where((p, i) => i == 0 || p.EndpointId is not null).ToList();
        _players.Clear();
        _players.AddRange(stillHere);
        if (_players.Count < 2)
        {
            _ = Dialogs.AlertAsync("Need another player", "Wait for at least one more phone to join.");
            SetMode(Mode.Hosting);
            return;
        }

        _nearby.StopLooking();
        uint seed = (uint)Random.Shared.Next() & Seeds.SeedMask;
        _engine = new BattleEngine(_players.Count, seed);
        _names = _players.Select(p => p.Name).ToList();
        _myIndex = 0;
        var state = BattleWire.State(BattleSnapshot.From(_engine));
        for (int i = 1; i < _players.Count; i++)
        {
            if (_players[i].EndpointId is { } ep)
            {
                _nearby.Send(ep, BattleWire.Start(seed, _names, i));
                _nearby.Send(ep, state);
            }
        }

        _drawable.Snapshot = BattleSnapshot.From(_engine);
        BeginCountdown();
    }

    private void BeginCountdown()
    {
        _drawable.LocalPlayer = _myIndex;
        _drawable.LocalHeading = null;
        _accumulatorMs = 0;
        _sinceStateMs = 0;
        _countdownLeftMs = CountdownMs;
        ResultPanel.IsVisible = false;
        CountdownLabel.Text = "3";
        CountdownLabel.IsVisible = true;
        SetMode(Mode.Countdown);
        _lastFrameMs = -1;
        _ticker.Start(OnFrame);
    }

    private void OnFrame(double nowMs)
    {
        double dt = _lastFrameMs < 0 ? 0 : Math.Clamp(nowMs - _lastFrameMs, 0, 100);
        _lastFrameMs = nowMs;

        if (_mode == Mode.Countdown)
        {
            _countdownLeftMs -= dt;
            string text = Math.Max(1, (int)Math.Ceiling(_countdownLeftMs / 1000)).ToString();
            if (text != CountdownLabel.Text)
            {
                CountdownLabel.Text = text;
                _feedback.Play(Sfx.Click);
            }

            if (_countdownLeftMs <= 0)
            {
                CountdownLabel.IsVisible = false;
                SetMode(Mode.Playing);
            }
        }
        else if (_mode == Mode.Playing && _isHost && _engine is not null)
        {
            _accumulatorMs += dt;
            while (_accumulatorMs >= BattleEngine.TickMs && _engine.Status == BattleStatus.Running)
            {
                _accumulatorMs -= BattleEngine.TickMs;
                int lengthBefore = _engine.Snakes[0].Length;
                _engine.Tick();
                if (_engine.Snakes[0].Length > lengthBefore)
                {
                    _feedback.Play(Sfx.Eat);
                }

                var snapshot = BattleSnapshot.From(_engine);
                _drawable.Snapshot = snapshot;
                _nearby.SendToAll(_players.Skip(1).Where(p => p.EndpointId is not null).Select(p => p.EndpointId!).ToList(), BattleWire.State(snapshot));
                RefreshStrip();
            }

            _drawable.LocalHeading = _engine.Snakes[0].Alive ? _engine.Snakes[0].NextHeading : null;
            _drawable.Interpolation = (float)(_accumulatorMs / BattleEngine.TickMs);
            if (_engine.Status == BattleStatus.Over)
            {
                ShowResults(BattleSnapshot.From(_engine));
            }
        }
        else if (_mode == Mode.Playing)
        {
            _sinceStateMs += dt;
            _drawable.Interpolation = (float)(_sinceStateMs / BattleEngine.TickMs);
        }

        if (_drawable.Snapshot is { } s)
        {
            TimerLabel.Text = TimeSpan.FromMilliseconds(s.RemainingMs).ToString(@"m\:ss");
        }

        Arena.Invalidate();
    }

    private void ShowResults(BattleSnapshot state)
    {
        SetMode(Mode.Results);
        _ticker.Stop();
        Arena.Invalidate();

        bool won = state.Winner == _myIndex;
        ResultTitle.Text = state.Winner < 0 ? "Draw!" : won ? "You win!" : $"{NameOf(state.Winner)} wins";
        _feedback.Play(won ? Sfx.LevelUp : Sfx.Death);
        _feedback.Buzz(won ? 40 : 200);

        ResultList.Children.Clear();
        var order = state.Snakes.Select((s, i) => (s, i))
            .OrderByDescending(x => x.i == state.Winner)
            .ThenByDescending(x => x.s.Alive)
            .ThenByDescending(x => x.s.Body.Count);
        foreach (var (snake, i) in order)
        {
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 10 };
            row.Add(new BoxView { Color = _drawable.PlayerColor(i), WidthRequest = 14, HeightRequest = 14, CornerRadius = 7, VerticalOptions = LayoutOptions.Center });
            var name = new Label { Text = NameOf(i) + (i == _myIndex ? " (you)" : ""), FontSize = 16, FontFamily = i == state.Winner ? "OpenSansSemibold" : "OpenSansRegular" };
            name.SetDynamicResource(Label.TextColorProperty, "Text");
            row.Add(name, 1);
            var stats = new Label { Text = snake.Alive ? $"length {snake.Body.Count}, {snake.Kills} KO" : snake.Dropped ? "left" : $"out, {snake.Kills} KO", FontSize = 13, VerticalOptions = LayoutOptions.Center };
            stats.SetDynamicResource(Label.TextColorProperty, "Muted");
            row.Add(stats, 2);
            ResultList.Children.Add(row);
        }

        RematchButton.IsVisible = _isHost;
        WaitingLabel.IsVisible = !_isHost;
        ResultPanel.IsVisible = true;
    }

    private string NameOf(int index) => index >= 0 && index < _names.Count ? _names[index] : $"Player {index + 1}";

    // Input -------------------------------------------------------------------------------------------

    private void Steer(Direction screenDirection)
    {
        if (_mode != Mode.Playing || _drawable.Snapshot is not { } s || _myIndex >= s.Snakes.Count || !s.Snakes[_myIndex].Alive)
        {
            return;
        }

        var direction = _drawable.Landscape ? screenDirection.FromLandscapeScreen() : screenDirection;
        if (_isHost)
        {
            _engine?.QueueTurn(0, direction);
            return;
        }

        var current = _drawable.LocalHeading ?? s.Snakes[_myIndex].Heading;
        if (direction == current || direction == current.Opposite())
        {
            return;
        }

        _drawable.LocalHeading = direction;
        if (_hostEndpoint is { } host)
        {
            _nearby.Send(host, BattleWire.Turn(direction));
        }

        Arena.Invalidate();
    }

    private void OnTouchStart(object? sender, TouchEventArgs e)
    {
        var p = e.Touches.FirstOrDefault();
        bool blocked = p.X < EdgeDeadZone || p.X > Arena.Width - EdgeDeadZone || p.Y > Arena.Height - EdgeDeadZone;
        _swipeAnchor = blocked ? null : p;
        if (!blocked && _settings.Controls == ControlScheme.ThumbTurns && _drawable.Snapshot is { } s && _myIndex < s.Snakes.Count)
        {
            var basis = _drawable.LocalHeading ?? s.Snakes[_myIndex].Heading;
            bool right = p.X > Arena.Width / 2;
            var turn = right ? basis.TurnRight() : basis.TurnLeft();
            Steer(_drawable.Landscape ? turn.TurnLeft() : turn);
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

#if ANDROID
    private bool OnKey(GameKey key)
    {
        switch (key)
        {
            case GameKey.Up: Steer(Direction.Up); return true;
            case GameKey.Down: Steer(Direction.Down); return true;
            case GameKey.Left: Steer(Direction.Left); return true;
            case GameKey.Right: Steer(Direction.Right); return true;
            default: return false;
        }
    }
#endif

    // Screen state -----------------------------------------------------------------------------------

    private void SetMode(Mode mode)
    {
        _mode = mode;
        bool lobby = mode is Mode.Choose or Mode.Hosting or Mode.Browsing or Mode.Joining or Mode.Waiting;
        LobbyPanel.IsVisible = lobby;
        PlayPanel.IsVisible = !lobby;
        PlayerStrip.IsVisible = !lobby;
        TimerLabel.IsVisible = !lobby;
        ChooseRow.IsVisible = mode == Mode.Choose;
        NameEntry.IsEnabled = mode == Mode.Choose;
        CancelButton.IsVisible = mode is Mode.Hosting or Mode.Browsing or Mode.Joining or Mode.Waiting;
        StatusLabel.Text = mode switch
        {
            Mode.Choose => "Host on one phone, join from the others",
            Mode.Hosting => "Waiting for players to join",
            Mode.Browsing => "Looking for battles nearby",
            Mode.Joining => "Connecting",
            Mode.Waiting => "Joined. Waiting for the host to start",
            Mode.Countdown => "Get ready",
            Mode.Playing => "Last snake alive wins",
            _ => "Match over",
        };

        if (lobby)
        {
            RefreshLobby();
        }
        else
        {
            RefreshStrip();
        }
    }

    private void RefreshLobby()
    {
        ListBody.Children.Clear();
        ListTitle.IsVisible = _mode != Mode.Choose;
        ListHint.IsVisible = false;
        StartButton.IsVisible = false;

        switch (_mode)
        {
            case Mode.Hosting:
                ListTitle.Text = $"Players ({_players.Count} of {BattleEngine.MaxPlayers})";
                for (int i = 0; i < _players.Count; i++)
                {
                    ListBody.Children.Add(PlayerRow(i, _players[i].Name, i == 0 ? "host, you" : "joined"));
                }

                ListHint.Text = "Other phones open Nearby Battle and tap Join a battle.";
                ListHint.IsVisible = true;
                StartButton.IsVisible = true;
                StartButton.IsEnabled = _players.Count >= 2;
                break;

            case Mode.Browsing:
                ListTitle.Text = "Battles nearby";
                foreach (var (endpoint, name) in _foundHosts)
                {
                    var join = Kit.Secondary($"Join {name}'s battle", (_, _) => JoinHost(endpoint));
                    ListBody.Children.Add(join);
                }

                ListHint.Text = _foundHosts.Count == 0 ? "Searching. Make sure the host has tapped Host a battle." : "Tap a battle to join.";
                ListHint.IsVisible = true;
                break;

            case Mode.Joining:
                ListTitle.Text = "Connecting";
                ListHint.Text = "Hold on, linking the phones.";
                ListHint.IsVisible = true;
                break;

            case Mode.Waiting:
                ListTitle.Text = _names.Count > 0 ? $"Players ({_names.Count} of {BattleEngine.MaxPlayers})" : "Joined";
                for (int i = 0; i < _names.Count; i++)
                {
                    ListBody.Children.Add(PlayerRow(i, _names[i], i == 0 ? "host" : i == _myIndex ? "you" : "joined"));
                }

                ListHint.Text = "The host starts the battle.";
                ListHint.IsVisible = true;
                break;
        }
    }

    private View PlayerRow(int index, string name, string note)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
        row.Add(new BoxView { Color = _drawable.PlayerColor(index), WidthRequest = 18, HeightRequest = 18, CornerRadius = 9, VerticalOptions = LayoutOptions.Center });
        var label = new Label { Text = name, FontSize = 17, FontFamily = "OpenSansSemibold", VerticalOptions = LayoutOptions.Center };
        label.SetDynamicResource(Label.TextColorProperty, "Text");
        row.Add(label, 1);
        var hint = new Label { Text = $"{note}, {BattleDrawable.PatternName(index)}", FontSize = 13, VerticalOptions = LayoutOptions.Center };
        hint.SetDynamicResource(Label.TextColorProperty, "Muted");
        row.Add(hint, 2);
        var card = new Border { Content = row, Padding = new Thickness(16, 12), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 } };
        card.SetDynamicResource(BackgroundColorProperty, "Surface");
        card.SetDynamicResource(Border.StrokeProperty, "Stroke");
        return card;
    }

    private void RefreshStrip()
    {
        PlayerStrip.Children.Clear();
        var snapshot = _drawable.Snapshot;
        for (int i = 0; i < _names.Count; i++)
        {
            var state = snapshot is not null && i < snapshot.Snakes.Count ? snapshot.Snakes[i] : null;
            string status = state is null ? "" : state.Alive ? $" {state.Body.Count}" : state.Dropped ? " left" : " out";
            var chip = new Chip(NameOf(i) + (i == _myIndex ? " (you)" : "") + status,
                new BoxView { Color = _drawable.PlayerColor(i), WidthRequest = 12, HeightRequest = 12, CornerRadius = 6, VerticalOptions = LayoutOptions.Center })
            {
                IsSelected = i == _myIndex,
                Opacity = state is { Alive: false } ? 0.5 : 1,
            };
            PlayerStrip.Children.Add(chip);
        }
    }

    private void Reset()
    {
        _ticker.Stop();
        _nearby.Stop();
        _players.Clear();
        _foundHosts.Clear();
        _names.Clear();
        _engine = null;
        _hostEndpoint = null;
        _isHost = false;
        _drawable.Snapshot = null;
        ResultPanel.IsVisible = false;
        CountdownLabel.IsVisible = false;
        SetMode(Mode.Choose);
    }

    private async void OnBack(object? sender, EventArgs e)
    {
        if (_mode is Mode.Countdown or Mode.Playing &&
            !await Dialogs.ConfirmAsync("Leave the battle?", _isHost ? "You are the host, so the match ends for everyone." : "Your snake will stop and stay on the board.", "Leave"))
        {
            return;
        }

        var bye = BattleWire.Bye();
        if (_isHost)
        {
            _nearby.SendToAll(_players.Skip(1).Where(p => p.EndpointId is not null).Select(p => p.EndpointId!).ToList(), bye);
        }
        else if (_hostEndpoint is { } host)
        {
            _nearby.Send(host, bye);
        }

        // Give the goodbye a moment to leave before the links close.
        await Task.Delay(150);
        Reset();
        await Navigation.PopAsync(animated: false);
    }
}
