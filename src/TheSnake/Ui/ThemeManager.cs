using TheSnake.Rendering;
using TheSnake.Services;

namespace TheSnake.Ui;

/// <summary>Pushes the chosen theme's colours into the app resources so every screen and sheet follows it.</summary>
public static class ThemeManager
{
    public static Theme Current { get; private set; } = Theme.All[0];

    public static event EventHandler? Changed;

    public static void ApplySaved() => Apply(Theme.Get(ServiceHelper.Get<AppSettings>().Theme));

    public static void Apply(Theme theme)
    {
        Current = theme;
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        resources["Bg"] = theme.Background;
        resources["Surface"] = theme.Surface;
        resources["SurfaceRaised"] = theme.SurfaceRaised;
        resources["Stroke"] = theme.Stroke;
        resources["Accent"] = theme.Accent;
        resources["OnAccent"] = theme.OnAccent;
        resources["Text"] = theme.Text;
        resources["Muted"] = theme.MutedText;
        resources["Highlight"] = theme.Golden;

        if (Application.Current is { } app)
        {
            app.UserAppTheme = theme.IsLight ? AppTheme.Light : AppTheme.Dark;
        }

#if ANDROID
        // Dark status bar icons on light themes, light icons on dark ones.
        if (Platform.CurrentActivity?.Window is { } window)
        {
            if (AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView) is { } controller)
            {
                controller.AppearanceLightStatusBars = theme.IsLight;
                controller.AppearanceLightNavigationBars = theme.IsLight;
            }
        }
#endif

        Changed?.Invoke(null, EventArgs.Empty);
    }
}
