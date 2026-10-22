namespace TheSnake.Services;

/// <summary>
/// AdMob IDs. These are Google's public test IDs, which always show test ads and are safe to use
/// in development. Replace both with the real IDs from the AdMob console before a Play Store release.
/// </summary>
public static class AdConfig
{
    public const string AppId = "ca-app-pub-3940256099942544~3347511713";
    public const string BannerUnitId = "ca-app-pub-3940256099942544/9214589741";
}

public interface IAdService
{
    /// <summary>True once consent is settled and the Ads SDK may request ads.</summary>
    bool CanRequestAds { get; }

    /// <summary>True when the player must be offered a Privacy options entry (UMP decides this).</summary>
    bool PrivacyOptionsRequired { get; }

    event EventHandler? StateChanged;

    /// <summary>Runs the consent flow (only shown where the law needs it) and starts the Ads SDK.</summary>
    Task InitializeAsync();

    Task ShowPrivacyOptionsAsync();

    /// <summary>Height in device independent units of an anchored adaptive banner for the given width.</summary>
    double BannerHeight(double widthDp);
}

public enum PurchaseOutcome
{
    Purchased,
    Pending,
    Cancelled,
    NotAvailable,
    Failed,
}

public interface IStoreService
{
    /// <summary>Ad free entitlement. Google Play is the source of truth, this is the cached value.</summary>
    bool IsAdFree { get; }

    string? RemoveAdsPrice { get; }

    event EventHandler? EntitlementChanged;

    /// <summary>Re-reads purchases from Google Play and acknowledges any that need it.</summary>
    Task RefreshAsync();

    Task<PurchaseOutcome> BuyRemoveAdsAsync();
}

/// <summary>Used where Google Play services are missing: no ads, no purchases, the game still works.</summary>
public sealed class NoAdService : IAdService
{
    public bool CanRequestAds => false;

    public bool PrivacyOptionsRequired => false;

    public event EventHandler? StateChanged { add { } remove { } }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task ShowPrivacyOptionsAsync() => Task.CompletedTask;

    public double BannerHeight(double widthDp) => 0;
}
