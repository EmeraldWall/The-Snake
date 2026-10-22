using Android.Media;
using TheSnake.Services;

namespace TheSnake.Platforms.Android;

/// <summary>Short beeps through ToneGenerator on the media stream, so the volume buttons control them.</summary>
public sealed class AndroidSoundService : ISoundService
{
    private ToneGenerator? _tones;

    public void Play(Sfx sound)
    {
        try
        {
            _tones ??= new ToneGenerator(global::Android.Media.Stream.Music, 70);
            var (tone, ms) = sound switch
            {
                Sfx.Eat => (Tone.PropBeep, 40),
                Sfx.Golden => (Tone.PropAck, 120),
                Sfx.PowerUp => (Tone.CdmaConfirm, 160),
                Sfx.Critter => (Tone.PropBeep2, 90),
                Sfx.NearMiss => (Tone.CdmaPip, 30),
                Sfx.Death => (Tone.CdmaAbbrAlert, 400),
                Sfx.LevelUp => (Tone.CdmaAlertAutoredialLite, 300),
                _ => (Tone.PropBeep, 15),
            };
            _tones.StartTone(tone, ms);
        }
        catch (Java.Lang.RuntimeException)
        {
            // ToneGenerator can fail when the audio system is busy. Sound is optional, so ignore it.
            _tones = null;
        }
    }
}
