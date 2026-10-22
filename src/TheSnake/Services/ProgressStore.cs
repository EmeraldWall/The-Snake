using System.Text.Json;
using TheSnake.Core;

namespace TheSnake.Services;

/// <summary>Loads and saves <see cref="PlayerProgress"/> as JSON in Preferences (covered by Android Auto Backup).</summary>
public sealed class ProgressStore
{
    private const string Key = "progress";

    public PlayerProgress Progress { get; private set; } = Load();

    public event EventHandler? Changed;

    public void Save()
    {
        Preferences.Default.Set(Key, JsonSerializer.Serialize(Progress));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static PlayerProgress Load()
    {
        try
        {
            string json = Preferences.Default.Get(Key, "");
            return string.IsNullOrEmpty(json) ? new PlayerProgress() : JsonSerializer.Deserialize<PlayerProgress>(json) ?? new PlayerProgress();
        }
        catch (JsonException)
        {
            return new PlayerProgress();
        }
    }
}
