using Android.Gms.Ads;
using TheSnake.Services;
using Xamarin.Google.UserMesssagingPlatform;

namespace TheSnake.Platforms.Android;

/// <summary>
/// Google UMP consent first (the form only appears where the law needs it), then the Mobile Ads SDK.
/// No ad is requested before <see cref="CanRequestAds"/> is true.
/// </summary>
public sealed class AdMobService : IAdService
{
    private IConsentInformation? _consent;
    private bool _sdkStarted;
    private Task? _initTask;

    public bool CanRequestAds => _sdkStarted && (_consent?.CanRequestAds() ?? false);

    public bool PrivacyOptionsRequired =>
        _consent?.PrivacyOptionsRequirementStatus == ConsentInformationPrivacyOptionsRequirementStatus.Required;

    public event EventHandler? StateChanged;

    public Task InitializeAsync() => _initTask ??= RunAsync();

    public Task ShowPrivacyOptionsAsync()
    {
        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            return Task.CompletedTask;
        }

        var done = new TaskCompletionSource();
        UserMessagingPlatform.ShowPrivacyOptionsForm(activity, new FormDismissed(_ =>
        {
            done.TrySetResult();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }));
        return done.Task;
    }

    public double BannerHeight(double widthDp)
    {
        var context = Platform.AppContext;
        var size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSize(context, (int)widthDp);
        float density = context.Resources?.DisplayMetrics?.Density ?? 1f;
        return size.GetHeightInPixels(context) / density;
    }

    private async Task RunAsync()
    {
        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            _initTask = null; // Try again once there is an activity.
            return;
        }

        _consent = UserMessagingPlatform.GetConsentInformation(activity);

        // A choice from an earlier session may already allow ads. Start the SDK straight away then.
        if (_consent.CanRequestAds())
        {
            StartSdk();
        }

        var updated = new TaskCompletionSource<bool>();
        var parameters = new ConsentRequestParameters.Builder().SetTagForUnderAgeOfConsent(false).Build();
        _consent.RequestConsentInfoUpdate(activity, parameters,
            new ConsentUpdated(() => updated.TrySetResult(true)),
            new ConsentUpdateFailed(_ => updated.TrySetResult(false)));

        if (await updated.Task)
        {
            var dismissed = new TaskCompletionSource();
            MainThread.BeginInvokeOnMainThread(() =>
                UserMessagingPlatform.LoadAndShowConsentFormIfRequired(activity, new FormDismissed(_ => dismissed.TrySetResult())));
            await dismissed.Task;
        }

        if (_consent.CanRequestAds())
        {
            StartSdk();
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StartSdk()
    {
        if (_sdkStarted)
        {
            return;
        }

        _sdkStarted = true;
        var context = Platform.AppContext;
        // Google recommends starting the SDK off the main thread.
        Task.Run(() => MobileAds.Initialize(context));
        MainThread.BeginInvokeOnMainThread(() => StateChanged?.Invoke(this, EventArgs.Empty));
    }

    private sealed class ConsentUpdated(Action onDone) : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateSuccessListener
    {
        public void OnConsentInfoUpdateSuccess() => onDone();
    }

    private sealed class ConsentUpdateFailed(Action<FormError> onFail) : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateFailureListener
    {
        public void OnConsentInfoUpdateFailure(FormError p0) => onFail(p0);
    }

    private sealed class FormDismissed(Action<FormError?> onDone) : Java.Lang.Object, IConsentFormOnConsentFormDismissedListener
    {
        public void OnConsentFormDismissed(FormError? p0) => onDone(p0);
    }
}
