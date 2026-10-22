namespace TheSnake.Services;

/// <summary>The single place that runs the Remove ads purchase and tells the player what happened.</summary>
public static class RemoveAdsFlow
{
    public static async Task RunAsync()
    {
        var store = ServiceHelper.Get<IStoreService>();
        if (store.IsAdFree)
        {
            return;
        }

        var outcome = await store.BuyRemoveAdsAsync();
        string? message = outcome switch
        {
            PurchaseOutcome.Purchased => "Thanks for supporting The Snake. Ads are gone for good on every phone signed in to this Google account.",
            PurchaseOutcome.Pending => "Your payment is pending. Ads will disappear as soon as Google Play confirms it.",
            PurchaseOutcome.NotAvailable => "Remove ads is not available right now. It works when the game is installed from Google Play.",
            PurchaseOutcome.Failed => "The purchase did not go through. You have not been charged.",
            _ => null,
        };

        if (message is not null)
        {
            await Ui.Dialogs.AlertAsync(outcome == PurchaseOutcome.Purchased ? "Thank you!" : "Remove ads", message);
        }
    }
}
