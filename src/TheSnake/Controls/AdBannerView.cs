namespace TheSnake.Controls;

/// <summary>A native AdMob banner. The Android handler creates the AdView.</summary>
public sealed class AdBannerView : View
{
    public event EventHandler<bool>? LoadedChanged;

    internal void RaiseLoaded(bool loaded) => LoadedChanged?.Invoke(this, loaded);
}
