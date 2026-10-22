using Android.BillingClient.Api;
using TheSnake.Services;

namespace TheSnake.Platforms.Android;

/// <summary>
/// One time "remove_ads" product through Google Play Billing (spec 9.2).
/// Google Play keeps the purchase on the player's account, so it survives updates, reinstalls and new phones.
/// The local cache only lets the app hide ads instantly and offline.
/// </summary>
public sealed class PlayBillingService : Java.Lang.Object, IStoreService, IPurchasesUpdatedListener
{
    public const string RemoveAdsProductId = "remove_ads";
    private const string InApp = "inapp";
    private const string CacheKey = "store.adFree";

    private BillingClient? _client;
    private TaskCompletionSource<PurchaseOutcome>? _pendingBuy;

    public bool IsAdFree { get; private set; } = Preferences.Default.Get(CacheKey, false);

    public string? RemoveAdsPrice { get; private set; }

    public event EventHandler? EntitlementChanged;

    public async Task RefreshAsync()
    {
        if (!await ConnectAsync())
        {
            return;
        }

        try
        {
            var result = await _client!.QueryPurchasesAsync(QueryPurchasesParams.NewBuilder().SetProductType(InApp).Build());
            if (result.Result.ResponseCode != BillingResponseCode.Ok)
            {
                return; // Keep the cached value when Google Play cannot answer.
            }

            bool owned = false;
            foreach (var purchase in result.Purchases ?? [])
            {
                owned |= await HandlePurchaseAsync(purchase);
            }

            // Only a successful answer without the product turns ads back on (for example after a refund).
            SetAdFree(owned);
            await LoadPriceAsync();
        }
        catch (Java.Lang.Exception)
        {
        }
    }

    public async Task<PurchaseOutcome> BuyRemoveAdsAsync()
    {
        var activity = Platform.CurrentActivity;
        if (activity is null || !await ConnectAsync())
        {
            return PurchaseOutcome.NotAvailable;
        }

        var details = await QueryProductAsync();
        if (details is null)
        {
            return PurchaseOutcome.NotAvailable;
        }

        var flow = BillingFlowParams.NewBuilder()
            .SetProductDetailsParamsList([BillingFlowParams.ProductDetailsParams.NewBuilder().SetProductDetails(details).Build()])
            .Build();

        _pendingBuy = new TaskCompletionSource<PurchaseOutcome>();
        var launch = _client!.LaunchBillingFlow(activity, flow);
        if (launch.ResponseCode != BillingResponseCode.Ok)
        {
            _pendingBuy = null;
            return launch.ResponseCode == BillingResponseCode.ItemAlreadyOwned ? await RefreshThenOwned() : PurchaseOutcome.Failed;
        }

        return await _pendingBuy.Task;
    }

    public void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases)
    {
        _ = OnPurchasesUpdatedAsync(result, purchases);
    }

    private async Task OnPurchasesUpdatedAsync(BillingResult result, IList<Purchase>? purchases)
    {
        var outcome = result.ResponseCode switch
        {
            BillingResponseCode.UserCancelled => PurchaseOutcome.Cancelled,
            BillingResponseCode.Ok => PurchaseOutcome.Failed,
            _ => PurchaseOutcome.Failed,
        };

        if (result.ResponseCode == BillingResponseCode.Ok && purchases is not null)
        {
            foreach (var purchase in purchases)
            {
                if (!purchase.Products.Contains(RemoveAdsProductId))
                {
                    continue;
                }

                if (purchase.PurchaseState == PurchaseState.Pending)
                {
                    outcome = PurchaseOutcome.Pending;
                }
                else if (await HandlePurchaseAsync(purchase))
                {
                    outcome = PurchaseOutcome.Purchased;
                    SetAdFree(true);
                }
            }
        }

        _pendingBuy?.TrySetResult(outcome);
        _pendingBuy = null;
    }

    /// <summary>Acknowledges a completed purchase. Unacknowledged purchases are refunded by Google after 3 days.</summary>
    private async Task<bool> HandlePurchaseAsync(Purchase purchase)
    {
        if (!purchase.Products.Contains(RemoveAdsProductId) || purchase.PurchaseState != PurchaseState.Purchased)
        {
            return false;
        }

        if (!purchase.IsAcknowledged)
        {
            var ack = AcknowledgePurchaseParams.NewBuilder().SetPurchaseToken(purchase.PurchaseToken).Build();
            await _client!.AcknowledgePurchaseAsync(ack);
        }

        return true;
    }

    private async Task<PurchaseOutcome> RefreshThenOwned()
    {
        await RefreshAsync();
        return IsAdFree ? PurchaseOutcome.Purchased : PurchaseOutcome.Failed;
    }

    private async Task<ProductDetails?> QueryProductAsync()
    {
        var product = QueryProductDetailsParams.Product.NewBuilder().SetProductId(RemoveAdsProductId).SetProductType(InApp).Build();
        var result = await _client!.QueryProductDetailsAsync(QueryProductDetailsParams.NewBuilder().SetProductList([product]).Build());
        return result.Result.ResponseCode == BillingResponseCode.Ok ? result.ProductDetailsList?.FirstOrDefault() : null;
    }

    private async Task LoadPriceAsync()
    {
        var details = await QueryProductAsync();
        RemoveAdsPrice = details?.GetOneTimePurchaseOfferDetails()?.FormattedPrice;
    }

    private async Task<bool> ConnectAsync()
    {
        try
        {
            _client ??= BillingClient.NewBuilder(Platform.AppContext)
                .SetListener(this)
                .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
                .EnableAutoServiceReconnection()
                .Build();

            if (_client.IsReady)
            {
                return true;
            }

            var result = await _client.StartConnectionAsync(() => { });
            return result.ResponseCode == BillingResponseCode.Ok;
        }
        catch (Java.Lang.Exception)
        {
            return false;
        }
    }

    private void SetAdFree(bool value)
    {
        Preferences.Default.Set(CacheKey, value);
        if (IsAdFree == value)
        {
            return;
        }

        IsAdFree = value;
        MainThread.BeginInvokeOnMainThread(() => EntitlementChanged?.Invoke(this, EventArgs.Empty));
    }
}
