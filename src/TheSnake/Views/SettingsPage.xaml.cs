using TheSnake.Core;
using TheSnake.Rendering;
using TheSnake.Services;
using TheSnake.Ui;

namespace TheSnake.Views;

/// <summary>Themes, snake look, controls and everything else. Built in code so the galleries can show live previews.</summary>
public partial class SettingsPage : ContentPage, IHasSheetHost
{
    private static readonly int[] BreakMinutes = [0, 30, 45, 60, 90];

    private readonly AppSettings _settings = ServiceHelper.Get<AppSettings>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly IAdService _ads = ServiceHelper.Get<IAdService>();
    private readonly ProgressStore _progress = ServiceHelper.Get<ProgressStore>();
    private readonly List<(GraphicsView View, GameDrawable Drawable)> _previews = [];
    private readonly List<(Border Tile, string Theme)> _themeTiles = [];
    private readonly List<(Border Dot, string Color)> _colorDots = [];
    private GameDrawable? _lookPreview;
    private GraphicsView? _lookPreviewView;
    private Button? _removeAds;
    private Button? _privacy;

    public SettingsPage()
    {
        InitializeComponent();

        // Full width on phones, a centred column on tablets and foldables.
        SizeChanged += (_, _) => Body.WidthRequest = Math.Min(620, Width);
        Build();
    }

    public SheetHost Sheets => SheetLayer;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_removeAds is not null) _removeAds.IsVisible = !_store.IsAdFree;
        if (_privacy is not null) _privacy.IsVisible = _ads.PrivacyOptionsRequired;
    }

    protected override bool OnBackButtonPressed() => Sheets.HandleBack() || base.OnBackButtonPressed();

    private int PlayerLevel => Progression.LevelForXp(_progress.Progress.Xp);

    private void Build()
    {
        int level = PlayerLevel;
        var unlockLevel = Progression.ThemeUnlocks.ToDictionary(t => t.Theme, t => t.Level);

        // Themes.
        Body.Children.Add(Kit.Section("Theme"));
        Body.Children.Add(Muted("Changes the whole app, not just the game. More themes unlock as you level up."));
        var themeGrid = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var theme in Theme.All.Where(t => !t.Pixel))
        {
            int needed = unlockLevel.GetValueOrDefault(theme.Name, 1);
            themeGrid.Children.Add(ThemeTile(theme, locked: needed > level, needed, selected: theme.Name == _settings.Theme, onPick: () =>
            {
                _settings.Theme = theme.Name;
                ThemeManager.Apply(theme);
                RefreshSelections();
            }));
        }

        Body.Children.Add(themeGrid);

        // Snake look.
        Body.Children.Add(Kit.Section("Snake"));
        Body.Children.Add(LookPreview());
        Body.Children.Add(Caption("Colour"));
        var dots = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var hex in Look.SnakeColors)
        {
            dots.Children.Add(ColorDot(hex));
        }

        Body.Children.Add(dots);
        Body.Children.Add(Caption("Skin"));
        Body.Children.Add(Kit.ChipGroup(Enum.GetNames<SkinPattern>(), _settings.Skin, i => { _settings.Skin = i; RefreshPreviews(); }));
        Body.Children.Add(Caption("Body"));
        Body.Children.Add(Kit.ChipGroup(Enum.GetNames<BodyShape>(), _settings.Shape, i => { _settings.Shape = i; RefreshPreviews(); }));
        Body.Children.Add(Caption("Food"));
        Body.Children.Add(Kit.ChipGroup(Enum.GetNames<FoodStyle>(), _settings.Food, i => { _settings.Food = i; RefreshPreviews(); }));
        Body.Children.Add(Caption("Board"));
        Body.Children.Add(Kit.ChipGroup(["Dot grid", "Line grid", "Plain"], _settings.Grid, i => { _settings.Grid = i; RefreshPreviews(); }));
        Body.Children.Add(SwitchRow("Glow effects", _settings.Glow, v => { _settings.Glow = v; RefreshPreviews(); }));

        // Retro screens.
        Body.Children.Add(Kit.Section("Retro 97 screen"));
        var pixelThemes = Theme.All.Where(t => t.Pixel).ToList();
        var retroGrid = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var theme in pixelThemes)
        {
            int needed = unlockLevel.GetValueOrDefault(theme.Name, 1);
            retroGrid.Children.Add(ThemeTile(theme, needed > level, needed, theme.Name == _settings.RetroTheme, () =>
            {
                _settings.RetroTheme = theme.Name;
                RefreshSelections();
            }));
        }

        Body.Children.Add(retroGrid);

        // Controls.
        Body.Children.Add(Kit.Section("Controls"));
        Body.Children.Add(Kit.ChipGroup(["Swipe", "Thumb taps", "D pad"], (int)_settings.Controls, i => _settings.Controls = (ControlScheme)i));
        Body.Children.Add(Muted("Swipe: flick anywhere on the board. Thumb taps: tap the left or right half to turn. Keyboards and game controllers always work."));

        // Sound and feel.
        Body.Children.Add(Kit.Section("Sound and feel"));
        Body.Children.Add(SwitchRow("Sound", _settings.Sound, v => _settings.Sound = v));
        Body.Children.Add(SwitchRow("Vibration", _settings.Vibration, v => _settings.Vibration = v));
        Body.Children.Add(SwitchRow("Reduce motion", _settings.ReduceMotion, v => _settings.ReduceMotion = v));

        // Healthy play.
        Body.Children.Add(Kit.Section("Break reminder"));
        Body.Children.Add(Kit.ChipGroup(BreakMinutes.Select(m => m == 0 ? "Off" : $"{m} min").ToList(),
            Math.Max(0, Array.IndexOf(BreakMinutes, _settings.BreakReminderMinutes)), i => _settings.BreakReminderMinutes = BreakMinutes[i]));

        // Ads and privacy.
        Body.Children.Add(Kit.Section("Ads and privacy"));
        _removeAds = Kit.Primary(_store.RemoveAdsPrice is { } price ? $"Remove ads {price}" : "Remove ads", async (_, _) =>
        {
            await RemoveAdsFlow.RunAsync();
            _removeAds!.IsVisible = !_store.IsAdFree;
        });
        Body.Children.Add(_removeAds);
        Body.Children.Add(Kit.Secondary("Restore purchases", async (_, _) =>
        {
            await _store.RefreshAsync();
            _removeAds.IsVisible = !_store.IsAdFree;
            await Dialogs.AlertAsync("Restore purchases", _store.IsAdFree ? "Remove ads is active on this phone." : "No Remove ads purchase was found for this Google account.");
        }));
        _privacy = Kit.Secondary("Privacy options", async (_, _) => await _ads.ShowPrivacyOptionsAsync());
        Body.Children.Add(_privacy);

        // About.
        Body.Children.Add(Kit.Section("About"));
        Body.Children.Add(Muted($"The Snake {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})\n{AppInfo.Current.PackageName}\nMade with .NET MAUI in Visual Studio.\nOne banner ad keeps the game free. One small purchase removes it for good."));
    }

    private View ThemeTile(Theme theme, bool locked, int needed, bool selected, Action onPick)
    {
        var drawable = new GameDrawable { Theme = theme, Look = Look.Default, Engine = PreviewEngine(), Interpolation = 0 };
        var view = new GraphicsView { Drawable = drawable, HeightRequest = 64, InputTransparent = true };
        if (!theme.Pixel)
        {
            _previews.Add((view, drawable));
        }

        var name = new Label { Text = theme.Name, FontSize = 13, FontFamily = "OpenSansSemibold", HorizontalTextAlignment = TextAlignment.Center };
        name.SetDynamicResource(Label.TextColorProperty, "Text");
        var stack = new VerticalStackLayout { Spacing = 6, Children = { view, name } };

        var content = new Grid { Children = { stack } };
        if (locked)
        {
            var veil = new Border
            {
                BackgroundColor = Colors.Black.WithAlpha(0.55f),
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                HeightRequest = 64,
                VerticalOptions = LayoutOptions.Start,
                Content = new Label { Text = $"Level {needed}", TextColor = Colors.White, FontFamily = "OpenSansSemibold", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
            };
            content.Children.Add(veil);
        }

        var tile = new Border
        {
            WidthRequest = 112,
            Padding = 6,
            Margin = new Thickness(0, 0, 10, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Content = content,
        };
        tile.SetDynamicResource(BackgroundColorProperty, "Surface");
        SemanticProperties.SetDescription(tile, locked ? $"{theme.Name}, unlocks at level {needed}" : theme.Name);

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (locked)
            {
                await Dialogs.AlertAsync(theme.Name, $"Reach level {needed} to unlock this theme. You are level {PlayerLevel}. Every run earns XP.");
                return;
            }

            onPick();
        };
        tile.GestureRecognizers.Add(tap);
        _themeTiles.Add((tile, theme.Name));
        StyleTile(tile, selected);
        return tile;
    }

    private static void StyleTile(Border tile, bool selected)
    {
        tile.StrokeThickness = selected ? 3 : 1;
        tile.SetDynamicResource(Border.StrokeProperty, selected ? "Accent" : "Stroke");
    }

    private View LookPreview()
    {
        _lookPreview = new GameDrawable { Theme = Theme.Get(_settings.Theme), Look = _settings.Look, Engine = PreviewEngine(length: 6), Interpolation = 0.5f };
        _lookPreviewView = new GraphicsView { Drawable = _lookPreview, HeightRequest = 110, InputTransparent = true };
        var card = new Border { Content = _lookPreviewView, Padding = 6, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 } };
        card.SetDynamicResource(BackgroundColorProperty, "Surface");
        card.SetDynamicResource(Border.StrokeProperty, "Stroke");
        return card;
    }

    private View ColorDot(string hex)
    {
        bool themeColour = hex.Length == 0;
        var swatch = new Border
        {
            WidthRequest = 40,
            HeightRequest = 40,
            Margin = new Thickness(0, 0, 10, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
            BackgroundColor = themeColour ? Colors.Transparent : Color.FromArgb(hex),
            Content = themeColour ? new Label { Text = "A", FontFamily = "OpenSansSemibold", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } : null,
        };
        if (themeColour && swatch.Content is Label auto)
        {
            auto.SetDynamicResource(Label.TextColorProperty, "Text");
        }

        SemanticProperties.SetDescription(swatch, themeColour ? "Theme colour" : $"Colour {hex}");
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            _settings.SnakeColor = hex;
            RefreshSelections();
            RefreshPreviews();
        };
        swatch.GestureRecognizers.Add(tap);
        _colorDots.Add((swatch, hex));
        StyleDot(swatch, hex == _settings.SnakeColor);
        return swatch;
    }

    private static void StyleDot(Border dot, bool selected)
    {
        dot.StrokeThickness = selected ? 4 : 1.5;
        dot.SetDynamicResource(Border.StrokeProperty, selected ? "Text" : "Stroke");
    }

    private void RefreshSelections()
    {
        foreach (var (tile, name) in _themeTiles)
        {
            StyleTile(tile, name == _settings.Theme || name == _settings.RetroTheme);
        }

        foreach (var (dot, hex) in _colorDots)
        {
            StyleDot(dot, hex == _settings.SnakeColor);
        }

        RefreshPreviews();
    }

    private void RefreshPreviews()
    {
        var look = _settings.Look;
        foreach (var (view, drawable) in _previews)
        {
            drawable.Look = look with { SnakeColor = null };
            view.Invalidate();
        }

        if (_lookPreview is not null)
        {
            _lookPreview.Theme = Theme.Get(_settings.Theme);
            _lookPreview.Look = look;
            _lookPreviewView?.Invalidate();
        }
    }

    /// <summary>A tiny board with a snake and food, to preview themes and looks.</summary>
    private static GameEngine PreviewEngine(int length = 4)
    {
        var engine = new GameEngine(new GameSetup(GameMode.Endless, RuleSet.Modern, 9, 4, true, new HashSet<GridPoint>(), 7));
        for (int i = 4; i < length; i++)
        {
            // Grow by steering into food is too slow here, so just move a little to show a bend.
            engine.QueueTurn(i % 2 == 0 ? Direction.Down : Direction.Right);
            engine.Tick();
        }

        return engine;
    }

    private static Label Caption(string text)
    {
        var l = new Label { Text = text, FontSize = 14, FontFamily = "OpenSansSemibold", Margin = new Thickness(4, 8, 0, 2) };
        l.SetDynamicResource(Microsoft.Maui.Controls.Label.TextColorProperty, "Text");
        return l;
    }

    private static Label Muted(string text)
    {
        var l = new Label { Text = text };
        l.SetDynamicResource(StyleProperty, "MutedText");
        return l;
    }

    private static View SwitchRow(string text, bool value, Action<bool> onChange)
    {
        var label = new Label { Text = text, FontSize = 16, VerticalOptions = LayoutOptions.Center };
        label.SetDynamicResource(Microsoft.Maui.Controls.Label.TextColorProperty, "Text");
        var toggle = new Switch { IsToggled = value };
        toggle.Toggled += (_, e) => onChange(e.Value);
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(4, 2) };
        row.Add(label);
        row.Add(toggle, 1);
        return row;
    }

    private async void OnBack(object? sender, EventArgs e) => await Navigation.PopAsync(animated: false);
}
