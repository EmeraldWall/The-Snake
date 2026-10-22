namespace TheSnake.Services;

public enum Sfx
{
    Eat,
    Golden,
    PowerUp,
    Critter,
    NearMiss,
    Death,
    Click,
    LevelUp,
}

public interface ISoundService
{
    void Play(Sfx sound);
}

public sealed class SilentSoundService : ISoundService
{
    public void Play(Sfx sound)
    {
    }
}

/// <summary>Sound and vibration, each respecting its own setting.</summary>
public sealed class Feedback(ISoundService sound, AppSettings settings)
{
    public void Play(Sfx sfx)
    {
        if (settings.Sound)
        {
            sound.Play(sfx);
        }
    }

    public void Tick()
    {
        if (!settings.Vibration)
        {
            return;
        }

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
        }
    }

    public void Buzz(int milliseconds)
    {
        if (!settings.Vibration)
        {
            return;
        }

        try
        {
            Microsoft.Maui.Devices.Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(milliseconds));
        }
        catch (FeatureNotSupportedException)
        {
        }
    }
}
