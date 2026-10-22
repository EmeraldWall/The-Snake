using Microsoft.Extensions.Logging;
using TheSnake.Controls;
using TheSnake.Services;
#if ANDROID
using TheSnake.Platforms.Android;
#endif

namespace TheSnake;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<AdBannerView, AdBannerHandler>();
#endif
            });

        builder.Services.AddSingleton<AppSettings>();
        builder.Services.AddSingleton<ProgressStore>();
        builder.Services.AddSingleton<Feedback>();
#if ANDROID
        builder.Services.AddSingleton<ISoundService, AndroidSoundService>();
        builder.Services.AddSingleton<IAdService, AdMobService>();
        builder.Services.AddSingleton<IStoreService, PlayBillingService>();
#else
        builder.Services.AddSingleton<ISoundService, SilentSoundService>();
        builder.Services.AddSingleton<IAdService, NoAdService>();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
