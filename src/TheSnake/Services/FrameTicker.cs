namespace TheSnake.Services;

/// <summary>Calls back once per screen frame while running.</summary>
public interface IFrameTicker
{
    bool IsRunning { get; }

    /// <summary>Starts calling <paramref name="onFrame"/> with the frame time in milliseconds.</summary>
    void Start(Action<double> onFrame);

    void Stop();
}

/// <summary>Fallback for platforms without a vsync callback.</summary>
public sealed class TimerFrameTicker : IFrameTicker
{
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private IDispatcherTimer? _timer;
    private Action<double>? _onFrame;

    public bool IsRunning => _timer?.IsRunning ?? false;

    public void Start(Action<double> onFrame)
    {
        _onFrame = onFrame;
        if (_timer is null)
        {
            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(8);
            _timer.Tick += (_, _) => _onFrame?.Invoke(_clock.Elapsed.TotalMilliseconds);
        }

        _timer.Start();
    }

    public void Stop() => _timer?.Stop();
}
