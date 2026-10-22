namespace TheSnake.Ui;

/// <summary>A tappable pill used for single choices. Colours follow the theme.</summary>
public sealed class Chip : Border
{
    private readonly Label _label;
    private bool _selected;

    public Chip(string text, View? leading = null)
    {
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 };
        StrokeThickness = 1;
        Padding = new Thickness(16, 0);
        MinimumHeightRequest = 44;
        Margin = new Thickness(0, 0, 8, 8);
        _label = new Label { Text = text, FontSize = 15, FontFamily = "OpenSansSemibold", VerticalOptions = LayoutOptions.Center };
        Content = leading is null
            ? _label
            : new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center, Children = { leading, _label } };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);
        SemanticProperties.SetDescription(this, text);
        Apply();
    }

    public event EventHandler? Tapped;

    public bool IsSelected
    {
        get => _selected;
        set
        {
            _selected = value;
            Apply();
        }
    }

    private void Apply()
    {
        SetDynamicResource(BackgroundColorProperty, _selected ? "Accent" : "Surface");
        SetDynamicResource(StrokeProperty, _selected ? "Accent" : "Stroke");
        _label.SetDynamicResource(Label.TextColorProperty, _selected ? "OnAccent" : "Text");
    }
}

public static class Kit
{
    /// <summary>A wrapping row of chips where exactly one is selected.</summary>
    public static FlexLayout ChipGroup(IReadOnlyList<string> options, int selected, Action<int> onSelect, Func<int, View?>? leading = null)
    {
        var flex = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Start };
        var chips = new List<Chip>();
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var chip = new Chip(options[i], leading?.Invoke(i)) { IsSelected = i == selected };
            chip.Tapped += (_, _) =>
            {
                foreach (var c in chips) c.IsSelected = c == chip;
                onSelect(index);
            };
            chips.Add(chip);
            flex.Children.Add(chip);
        }

        return flex;
    }

    public static Button Primary(string text, EventHandler onClick)
    {
        var b = new Button { Text = text };
        b.SetDynamicResource(VisualElement.StyleProperty, "PrimaryButton");
        b.Clicked += onClick;
        return b;
    }

    public static Button Secondary(string text, EventHandler onClick)
    {
        var b = new Button { Text = text };
        b.SetDynamicResource(VisualElement.StyleProperty, "SecondaryButton");
        b.Clicked += onClick;
        return b;
    }

    public static Button Ghost(string text, EventHandler onClick)
    {
        var b = new Button { Text = text };
        b.SetDynamicResource(VisualElement.StyleProperty, "GhostButton");
        b.Clicked += onClick;
        return b;
    }

    public static Label Section(string text)
    {
        var l = new Label { Text = text.ToUpperInvariant() };
        l.SetDynamicResource(VisualElement.StyleProperty, "Section");
        return l;
    }
}

/// <summary>Ready made sheets for the common cases.</summary>
public static class Dialogs
{
    /// <summary>The sheet host of the page on screen.</summary>
    public static SheetHost? CurrentHost =>
        (Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page) is IHasSheetHost page ? page.Sheets : null;

    public static async Task AlertAsync(string title, string message, string ok = "OK")
    {
        if (CurrentHost is not { } host)
        {
            return;
        }

        await host.ShowAsync<object>(title, message, h => [Kit.Primary(ok, (_, _) => h.Close(null))]);
    }

    public static async Task<bool> ConfirmAsync(string title, string message, string confirm, string cancel = "Cancel")
    {
        if (CurrentHost is not { } host)
        {
            return false;
        }

        return await host.ShowAsync<bool>(title, message, h =>
        [
            Kit.Primary(confirm, (_, _) => h.Close(true)),
            Kit.Ghost(cancel, (_, _) => h.Close(false)),
        ]);
    }

    public static async Task<string?> PromptAsync(string title, string message, string placeholder, string ok, int maxLength = 20)
    {
        if (CurrentHost is not { } host)
        {
            return null;
        }

        return await host.ShowAsync<string>(title, message, h =>
        {
            var entry = new Entry
            {
                Placeholder = placeholder,
                MaxLength = maxLength,
                FontSize = 22,
                FontFamily = "OpenSansSemibold",
                CharacterSpacing = 3,
                HorizontalTextAlignment = TextAlignment.Center,
                Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeCharacter),
            };
            entry.SetDynamicResource(Entry.TextColorProperty, "Text");
            entry.SetDynamicResource(Entry.PlaceholderColorProperty, "Muted");
            var box = new Border { Content = entry, Padding = new Thickness(12, 4), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 } };
            box.SetDynamicResource(VisualElement.BackgroundColorProperty, "Surface");
            box.SetDynamicResource(Border.StrokeProperty, "Stroke");
            entry.Completed += (_, _) => h.Close(entry.Text);
            entry.Loaded += (_, _) => entry.Focus();
            return [box, Kit.Primary(ok, (_, _) => h.Close(entry.Text)), Kit.Ghost("Cancel", (_, _) => h.Close(null))];
        });
    }
}
