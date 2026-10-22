using Android.Views;
using TheSnake.Services;

namespace TheSnake.Platforms.Android;

/// <summary>
/// Frame loop on Android's Choreographer, which fires once per display refresh (60, 90 or 120 Hz).
/// Unlike a timer it never drifts against the screen, so motion and input stay as quick as the display allows.
/// </summary>
public sealed class ChoreographerTicker : Java.Lang.Object, IFrameTicker, Choreographer.IFrameCallback
{
    private Action<double>? _onFrame;

    public bool IsRunning { get; private set; }

    public void Start(Action<double> onFrame)
    {
        _onFrame = onFrame;
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        Choreographer.Instance?.PostFrameCallback(this);
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        Choreographer.Instance?.RemoveFrameCallback(this);
    }

    public void DoFrame(long frameTimeNanos)
    {
        if (!IsRunning)
        {
            return;
        }

        _onFrame?.Invoke(frameTimeNanos / 1_000_000.0);
        if (IsRunning)
        {
            Choreographer.Instance?.PostFrameCallback(this);
        }
    }
}
