using Android.Views;

namespace TheSnake.Platforms.Android;

public enum GameKey
{
    Up,
    Down,
    Left,
    Right,
    Pause,
    Select,
}

/// <summary>Passes hardware keyboard and game controller keys from MainActivity to the game page.</summary>
public static class KeyInput
{
    /// <summary>Set by the game page while it is visible. Returns true when the key was used.</summary>
    public static Func<GameKey, bool>? Handler { get; set; }

    public static GameKey? Map(Keycode code) => code switch
    {
        Keycode.DpadUp or Keycode.W or Keycode.Num2 or Keycode.Numpad8 => GameKey.Up,
        Keycode.DpadDown or Keycode.S or Keycode.Num8 or Keycode.Numpad2 => GameKey.Down,
        Keycode.DpadLeft or Keycode.A or Keycode.Num4 or Keycode.Numpad4 => GameKey.Left,
        Keycode.DpadRight or Keycode.D or Keycode.Num6 or Keycode.Numpad6 => GameKey.Right,
        Keycode.Space or Keycode.P or Keycode.ButtonStart or Keycode.Num5 or Keycode.Numpad5 => GameKey.Pause,
        Keycode.Enter or Keycode.ButtonA or Keycode.DpadCenter => GameKey.Select,
        _ => null,
    };
}
