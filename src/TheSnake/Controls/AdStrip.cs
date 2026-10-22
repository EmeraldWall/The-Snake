using TheSnake.Services;

namespace TheSnake.Controls;

/// <summary>
/// The bottom ad strip (spec 9.1). Its height is reserved before an ad loads so the layout never jumps.
/// When no ad fills, a quiet house message shows. It is a Remove ads button in menus and does nothing in play.
/// The whole strip disappears once the player owns Remove ads.
/// </summary>
public sealed class AdStrip : ContentView
{
    private readonly IAdService _ads = ServiceHelper.Get<IAdService>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly Label _house;
    private readonly Grid _root;
    private AdBannerView? _banner;

    public AdStrip()
    {
        _house = new Label
        {
            Text = "THE SNAKE",
            TextColor = Color.FromArgb("#5F8F72"),
            FontSize = 14,
            CharacterSpacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        _root = new Grid { BackgroundColor = Color.FromArgb("#08100C") };
        _root.Add(_house);
        Content = _root;

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (!InGame && _banner is null && !_store.IsAdFree)
            {
                await RemoveAdsFlow.RunAsync();
            }
        };
        _house.GestureRecognizers.Add(tap);

        Loaded += (_, _) =>
        {
            _ads.StateChanged += OnStateChanged;
            _store.EntitlementChanged += OnStateChanged;
            Refresh();
        };
        Unloaded += (_, _) =>
        {
            _ads.StateChanged -= OnStateChanged;
            _store.EntitlementChanged -= OnStateChanged;
        };
    }

    /// <summary>True on the game page. The house message is never tappable during play.</summary>
    public bool InGame { get; set; }

    private void OnStateChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    private void Refresh()
    {
        if (_store.IsAdFree)
        {
            IsVisible = false;
            if (_banner is not null)
            {
                _root.Remove(_banner);
                _banner = null;
            }

            return;
        }

        IsVisible = true;
        double width = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        double height = _ads.BannerHeight(width);
        HeightRequest = height > 0 ? height : 50;
        _house.Text = InGame ? "THE SNAKE" : "THE SNAKE  .  Remove ads";

        if (_ads.CanRequestAds && _banner is null)
        {
            _banner = new AdBannerView();
            _banner.LoadedChanged += (_, loaded) => MainThread.BeginInvokeOnMainThread(() => _house.IsVisible = !loaded);
            _root.Add(_banner);
        }
    }
}
