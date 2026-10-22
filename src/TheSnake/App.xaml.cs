using TheSnake.Services;

namespace TheSnake;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
    }

    /// <summary>Raised when the app goes to the background, so a running game can pause.</summary>
    public static event EventHandler? WentToBackground;

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Deactivated += (_, _) => WentToBackground?.Invoke(this, EventArgs.Empty);
        window.Stopped += (_, _) => WentToBackground?.Invoke(this, EventArgs.Empty);
        window.Resumed += (_, _) => _ = ServiceHelper.Get<IStoreService>().RefreshAsync();
        return window;
    }
}
