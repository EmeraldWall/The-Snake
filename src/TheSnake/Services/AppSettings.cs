namespace TheSnake.Services;

public enum ControlScheme
{
    Swipe,
    ThumbTurns,
    DPad,
}

/// <summary>Player settings, stored with MAUI Preferences.</summary>
public sealed class AppSettings
{
    private readonly IPreferences _prefs = Preferences.Default;

    public bool Sound
    {
        get => _prefs.Get("settings.sound", true);
        set => _prefs.Set("settings.sound", value);
    }

    public bool Vibration
    {
        get => _prefs.Get("settings.vibration", true);
        set => _prefs.Set("settings.vibration", value);
    }

    public ControlScheme Controls
    {
        get => (ControlScheme)_prefs.Get("settings.controls", (int)ControlScheme.Swipe);
        set => _prefs.Set("settings.controls", (int)value);
    }

    public string Theme
    {
        get => _prefs.Get("settings.theme", "Neon LCD");
        set => _prefs.Set("settings.theme", value);
    }

    public string RetroTheme
    {
        get => _prefs.Get("settings.retroTheme", "Classic LCD");
        set => _prefs.Set("settings.retroTheme", value);
    }

    public string SnakeColor
    {
        get => _prefs.Get("look.snakeColor", "");
        set => _prefs.Set("look.snakeColor", value);
    }

    public int Skin
    {
        get => _prefs.Get("look.skin", 0);
        set => _prefs.Set("look.skin", value);
    }

    public int Shape
    {
        get => _prefs.Get("look.shape", 0);
        set => _prefs.Set("look.shape", value);
    }

    public int Grid
    {
        get => _prefs.Get("look.grid", 0);
        set => _prefs.Set("look.grid", value);
    }

    public int Food
    {
        get => _prefs.Get("look.food", 0);
        set => _prefs.Set("look.food", value);
    }

    public bool Glow
    {
        get => _prefs.Get("look.glow", true);
        set => _prefs.Set("look.glow", value);
    }

    public Rendering.Look Look => new(
        string.IsNullOrEmpty(SnakeColor) ? null : Color.FromArgb(SnakeColor),
        (Rendering.SkinPattern)Skin,
        (Rendering.BodyShape)Shape,
        (Rendering.GridStyle)Grid,
        (Rendering.FoodStyle)Food,
        Glow);

    public bool ReduceMotion
    {
        get => _prefs.Get("settings.reduceMotion", false);
        set => _prefs.Set("settings.reduceMotion", value);
    }

    public bool Purist
    {
        get => _prefs.Get("settings.purist", false);
        set => _prefs.Set("settings.purist", value);
    }

    public int RetroLevel
    {
        get => _prefs.Get("game.retroLevel", 5);
        set => _prefs.Set("game.retroLevel", value);
    }

    public int RetroVariant
    {
        get => _prefs.Get("game.retroVariant", 0);
        set => _prefs.Set("game.retroVariant", value);
    }

    public int BreakReminderMinutes
    {
        get => _prefs.Get("settings.breakMinutes", 0);
        set => _prefs.Set("settings.breakMinutes", value);
    }

    public bool SeenFirstRunHint
    {
        get => _prefs.Get("help.firstRun", false);
        set => _prefs.Set("help.firstRun", value);
    }

    public bool SeenEdgeSwipeTip
    {
        get => _prefs.Get("help.edgeSwipe", false);
        set => _prefs.Set("help.edgeSwipe", value);
    }
}
