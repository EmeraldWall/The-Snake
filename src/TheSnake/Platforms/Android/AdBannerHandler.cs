using Android.Gms.Ads;
using Android.Widget;
using Microsoft.Maui.Handlers;
using TheSnake.Controls;
using TheSnake.Services;

namespace TheSnake.Platforms.Android;

public sealed class AdBannerHandler() : ViewHandler<AdBannerView, FrameLayout>(PropertyMapper)
{
    public static readonly IPropertyMapper<AdBannerView, AdBannerHandler> PropertyMapper =
        new PropertyMapper<AdBannerView, AdBannerHandler>(ViewMapper);

    private AdView? _adView;

    protected override FrameLayout CreatePlatformView() => new(Context);

    protected override void ConnectHandler(FrameLayout platformView)
    {
        base.ConnectHandler(platformView);
        var metrics = Context.Resources?.DisplayMetrics;
        int widthDp = metrics is null ? 360 : (int)(metrics.WidthPixels / metrics.Density);

        _adView = new AdView(Context)
        {
            AdUnitId = AdConfig.BannerUnitId,
            AdSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSize(Context, widthDp),
            AdListener = new Listener(this),
        };
        platformView.AddView(_adView, new FrameLayout.LayoutParams(
            global::Android.Views.ViewGroup.LayoutParams.MatchParent,
            global::Android.Views.ViewGroup.LayoutParams.WrapContent,
            global::Android.Views.GravityFlags.Center));
        _adView.LoadAd(new AdRequest.Builder().Build());
    }

    protected override void DisconnectHandler(FrameLayout platformView)
    {
        _adView?.Destroy();
        _adView = null;
        platformView.RemoveAllViews();
        base.DisconnectHandler(platformView);
    }

    private sealed class Listener(AdBannerHandler handler) : AdListener
    {
        public override void OnAdLoaded() => handler.VirtualView?.RaiseLoaded(true);

        public override void OnAdFailedToLoad(LoadAdError p0) => handler.VirtualView?.RaiseLoaded(false);
    }
}
