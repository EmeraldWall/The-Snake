using Android.App;
using Android.Runtime;

namespace TheSnake;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();

        // Play Games' own start up provider is removed in the manifest, because it would fail without a real
        // project ID. Start the SDK here instead, only once games.xml holds real IDs.
        Platforms.Android.PlayGamesService.StartIfConfigured(this);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
