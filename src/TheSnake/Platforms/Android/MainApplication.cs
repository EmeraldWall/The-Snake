using Android.App;
using Android.Runtime;
using TheSnake.Services;

namespace TheSnake;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    // Do not override OnCreate here. In 0.3.0 an OnCreate override crashed every launch of the Release build
    // with "UnsatisfiedLinkError: No implementation found for MainApplication.n_onCreate()", because the
    // native link for the override was not registered. MauiApplication.OnCreate calls CreateMauiApp during
    // start up anyway, so the early work happens there instead.
    protected override MauiApp CreateMauiApp()
    {
        // Catch crashes from both .NET and Java code so the next launch can show what happened.
        CrashLog.Init(FilesDir!.AbsolutePath);
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => CrashLog.Write("Android", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashLog.Write(".NET", e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) => CrashLog.Write("Task", e.Exception);
        Java.Lang.Thread.DefaultUncaughtExceptionHandler = new JavaCrashHandler(Java.Lang.Thread.DefaultUncaughtExceptionHandler);

        // Play Games' own start up provider is removed in the manifest, because it would fail without a real
        // project ID. Start the SDK here instead, only once games.xml holds real IDs.
        try
        {
            Platforms.Android.PlayGamesService.StartIfConfigured(this);
        }
        catch (Java.Lang.Exception ex)
        {
            CrashLog.Write("Play Games start", ex);
        }

        return MauiProgram.CreateMauiApp();
    }

    private sealed class JavaCrashHandler(Java.Lang.Thread.IUncaughtExceptionHandler? previous) : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        public void UncaughtException(Java.Lang.Thread t, Java.Lang.Throwable e)
        {
            CrashLog.Write($"Java thread {t.Name}", Android.Util.Log.GetStackTraceString(e));
            previous?.UncaughtException(t, e);
        }
    }
}
