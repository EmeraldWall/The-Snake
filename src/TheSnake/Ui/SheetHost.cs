namespace TheSnake.Ui;

/// <summary>Pages that can show in-app sheets.</summary>
public interface IHasSheetHost
{
    SheetHost Sheets { get; }
}

/// <summary>
/// A modern bottom sheet that replaces the system alert, action sheet and prompt dialogs.
/// It sits on top of a page, dims what is behind it, slides up, and follows the app theme.
/// </summary>
public sealed class SheetHost : Grid
{
    private readonly BoxView _backdrop;
    private readonly Border _card;
    private readonly VerticalStackLayout _body;
    private TaskCompletionSource<object?>? _pending;

    public SheetHost()
    {
        IsVisible = false;
        _backdrop = new BoxView { Color = Colors.Black, Opacity = 0 };
        var dismiss = new TapGestureRecognizer();
        dismiss.Tapped += (_, _) => Close(null);
        _backdrop.GestureRecognizers.Add(dismiss);

        _body = new VerticalStackLayout { Spacing = 14 };
        var grabber = new Border
        {
            WidthRequest = 40,
            HeightRequest = 5,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 3 },
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 4),
        };
        grabber.SetDynamicResource(BackgroundColorProperty, "Stroke");

        _card = new Border
        {
            VerticalOptions = LayoutOptions.End,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
            Padding = new Thickness(24, 12, 24, 28),
            Content = new ScrollView { Content = new VerticalStackLayout { Spacing = 0, Children = { grabber, _body } } },
        };
        _card.SetDynamicResource(BackgroundColorProperty, "SurfaceRaised");

        // Swallow taps on the card so they do not reach the backdrop.
        _card.GestureRecognizers.Add(new TapGestureRecognizer());

        Children.Add(_backdrop);
        Children.Add(_card);

        // Full width on phones, a centred 560 dp sheet on tablets.
        SizeChanged += (_, _) => _card.Margin = new Thickness(Math.Max(0, (Width - 560) / 2), 0);
    }

    public bool IsOpen => _pending is not null;

    /// <summary>Shows a sheet built by <paramref name="build"/>. Resolves with the value passed to <see cref="Close"/>, or null if dismissed.</summary>
    public async Task<T?> ShowAsync<T>(string title, string? message, Func<SheetHost, IEnumerable<View>> build)
    {
        if (_pending is not null)
        {
            Close(null);
        }

        _body.Children.Clear();
        var heading = new Label { Text = title, FontFamily = "OpenSansSemibold", FontSize = 22 };
        heading.SetDynamicResource(Label.TextColorProperty, "Text");
        _body.Children.Add(heading);
        if (!string.IsNullOrEmpty(message))
        {
            var text = new Label { Text = message, FontSize = 15, LineHeight = 1.2 };
            text.SetDynamicResource(Label.TextColorProperty, "Muted");
            _body.Children.Add(text);
        }

        foreach (var view in build(this))
        {
            _body.Children.Add(view);
        }

        _pending = new TaskCompletionSource<object?>();
        var task = _pending.Task;

        IsVisible = true;
        _card.TranslationY = 500;
        await Task.WhenAll(
            _backdrop.FadeToAsync(0.55, 160, Easing.CubicOut),
            _card.TranslateToAsync(0, 0, 220, Easing.CubicOut));

        var result = await task;
        return result is T value ? value : default;
    }

    /// <summary>Closes the open sheet and resolves its task with <paramref name="result"/>.</summary>
    public async void Close(object? result)
    {
        var pending = _pending;
        if (pending is null)
        {
            return;
        }

        _pending = null;
        await Task.WhenAll(
            _backdrop.FadeToAsync(0, 140, Easing.CubicIn),
            _card.TranslateToAsync(0, Math.Max(400, _card.Height), 160, Easing.CubicIn));
        if (_pending is null)
        {
            IsVisible = false;
        }

        pending.TrySetResult(result);
    }

    /// <summary>Call from OnBackButtonPressed. Returns true when a sheet was open and has been closed.</summary>
    public bool HandleBack()
    {
        if (!IsOpen)
        {
            return false;
        }

        Close(null);
        return true;
    }
}
