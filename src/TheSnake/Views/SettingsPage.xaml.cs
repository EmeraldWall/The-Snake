using TheSnake.Core;
using TheSnake.Rendering;
using TheSnake.Services;

namespace TheSnake.Views;

public partial class SettingsPage : ContentPage
{
    private static readonly int[] BreakMinutes = [0, 30, 45, 60, 90];

    private readonly AppSettings _settings = ServiceHelper.Get<AppSettings>();
    private readonly IStoreService _store = ServiceHelper.Get<IStoreService>();
    private readonly IAdService _ads = ServiceHelper.Get<IAdService>();
    private readonly ProgressStore _progress = ServiceHelper.Get<ProgressStore>();
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();

        SoundSwitch.IsToggled = _settings.Sound;
        VibrationSwitch.IsToggled = _settings.Vibration;
        MotionSwitch.IsToggled = _settings.ReduceMotion;
        PuristSwitch.IsToggled = _settings.Purist;

        ControlsPicker.ItemsSource = new[] { "Swipe", "Thumb turns", "D pad" };
        ControlsPicker.SelectedIndex = (int)_settings.Controls;

        int level = Progression.LevelForXp(_progress.Progress.Xp);
        var unlocked = Progression.UnlockedThemes(level).ToList();
        var smooth = Theme.All.Where(t => !t.Pixel && unlocked.Contains(t.Name)).Select(t => t.Name).ToList();
        var pixel = Theme.All.Where(t => t.Pixel && unlocked.Contains(t.Name)).Select(t => t.Name).ToList();
        ThemePicker.ItemsSource = smooth;
        ThemePicker.SelectedIndex = Math.Max(0, smooth.IndexOf(_settings.Theme));
        RetroThemePicker.ItemsSource = pixel;
        RetroThemePicker.SelectedIndex = Math.Max(0, pixel.IndexOf(_settings.RetroTheme));
        var next = Progression.ThemeUnlocks.FirstOrDefault(t => t.Level > level);
        LockedLabel.Text = next.Theme is null ? "Every theme is unlocked." : $"Next theme: {next.Theme} at level {next.Level}. Themes are earned by playing, never sold.";

        BreakPicker.ItemsSource = BreakMinutes.Select(m => m == 0 ? "Off" : $"{m} minutes").ToList();
        BreakPicker.SelectedIndex = Math.Max(0, Array.IndexOf(BreakMinutes, _settings.BreakReminderMinutes));

        AboutLabel.Text = $"The Snake {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})\n{AppInfo.Current.PackageName}\nMade with .NET MAUI in Visual Studio.\nOne banner ad keeps the game free. One small purchase removes it for good.";
        _loading = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RemoveAdsButton.IsVisible = !_store.IsAdFree;
        PrivacyButton.IsVisible = _ads.PrivacyOptionsRequired;
    }

    private void OnToggled(object? sender, ToggledEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Sound = SoundSwitch.IsToggled;
        _settings.Vibration = VibrationSwitch.IsToggled;
        _settings.ReduceMotion = MotionSwitch.IsToggled;
        _settings.Purist = PuristSwitch.IsToggled;
    }

    private void OnPicked(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (ControlsPicker.SelectedIndex >= 0) _settings.Controls = (ControlScheme)ControlsPicker.SelectedIndex;
        if (ThemePicker.SelectedItem is string theme) _settings.Theme = theme;
        if (RetroThemePicker.SelectedItem is string retro) _settings.RetroTheme = retro;
        if (BreakPicker.SelectedIndex >= 0) _settings.BreakReminderMinutes = BreakMinutes[BreakPicker.SelectedIndex];
    }

    private async void OnRemoveAds(object? sender, EventArgs e)
    {
        await RemoveAdsFlow.RunAsync();
        RemoveAdsButton.IsVisible = !_store.IsAdFree;
    }

    private async void OnRestore(object? sender, EventArgs e)
    {
        await _store.RefreshAsync();
        RemoveAdsButton.IsVisible = !_store.IsAdFree;
        await DisplayAlertAsync("Restore purchases", _store.IsAdFree ? "Remove ads is active on this phone." : "No Remove ads purchase was found for this Google account.", "OK");
    }

    private async void OnPrivacy(object? sender, EventArgs e) => await _ads.ShowPrivacyOptionsAsync();

    private async void OnBack(object? sender, EventArgs e) => await Navigation.PopAsync();
}
