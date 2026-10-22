using Android.App;
using Android.Content.PM;
using Android.Views;
using TheSnake.Platforms.Android;

namespace TheSnake;

// No ScreenOrientation lock: Android 17 large screen rules, and the game redraws at any size.
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public class MainActivity : MauiAppCompatActivity
{
    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e is { Action: KeyEventActions.Down, RepeatCount: 0 } && KeyInput.Handler is { } handler
            && KeyInput.Map(e.KeyCode) is { } key && handler(key))
        {
            return true;
        }

        return base.DispatchKeyEvent(e);
    }
}
