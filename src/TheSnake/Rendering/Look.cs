namespace TheSnake.Rendering;

public enum SkinPattern
{
    Solid,
    Stripes,
    Gradient,
    Dots,
    Outline,
}

public enum BodyShape
{
    Rounded,
    Square,
    Beads,
}

public enum GridStyle
{
    Dots,
    Lines,
    Off,
}

public enum FoodStyle
{
    Orb,
    Apple,
    Gem,
}

/// <summary>Player chosen look on top of the theme. All free. Pixel (Retro 97) themes keep the classic look.</summary>
public sealed record Look(
    Color? SnakeColor,
    SkinPattern Skin,
    BodyShape Shape,
    GridStyle Grid,
    FoodStyle Food,
    bool Glow)
{
    public static readonly Look Default = new(null, SkinPattern.Solid, BodyShape.Rounded, GridStyle.Dots, FoodStyle.Orb, true);

    /// <summary>Snake colours a player can pick instead of the theme's own. Empty string means "theme colour".</summary>
    public static readonly IReadOnlyList<string> SnakeColors =
    [
        "", "#39FF88", "#4FD1FF", "#8C9EFF", "#C792FF", "#FF7AD9", "#FF4D6D",
        "#FF9E5E", "#FFD23F", "#9BE564", "#F2F2F2", "#00F0FF", "#1E7F4F",
    ];
}
