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
