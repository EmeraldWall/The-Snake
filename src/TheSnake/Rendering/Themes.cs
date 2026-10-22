namespace TheSnake.Rendering;

/// <summary>
/// Colours for one theme. Pixel themes draw square LCD blocks (Retro 97), the others draw rounded,
/// smooth shapes. Snake, food, golden food, obstacles and power ups keep at least 3 to 1 contrast
/// with the arena. The whole app (menus, sheets, buttons) also takes its colours from the theme.
/// </summary>
public sealed record Theme(
    string Name,
    Color Background,
    Color Arena,
    Color Grid,
    Color Snake,
    Color Head,
    Color Food,
    Color Golden,
    Color Obstacle,
    Color PowerUp,
    Color Text,
    bool Pixel)
{
    public bool IsLight => Background.GetLuminosity() > 0.5f;

    /// <summary>Cards and sheets: the background nudged toward the text colour.</summary>
    public Color Surface => Mix(Background, Text, 0.07f);

    public Color SurfaceRaised => Mix(Background, Text, 0.12f);

    public Color Stroke => Mix(Background, Text, 0.18f);

    public Color MutedText => Mix(Text, Background, 0.38f);

    /// <summary>Main buttons use the snake colour.</summary>
    public Color Accent => Snake;

    /// <summary>Black or white, whichever reads better on <see cref="Accent"/>.</summary>
    public Color OnAccent => Accent.GetLuminosity() > 0.55f ? Color.FromArgb("#0B1410") : Colors.White;

    public static Theme Get(string name) => All.FirstOrDefault(t => t.Name == name) ?? All[0];

    public static Color Mix(Color a, Color b, float amount) => new(
        a.Red + (b.Red - a.Red) * amount,
        a.Green + (b.Green - a.Green) * amount,
        a.Blue + (b.Blue - a.Blue) * amount);

    private static Theme Smooth(string name, string bg, string arena, string grid, string snake, string head, string food, string golden, string obstacle, string power, string text) =>
        new(name, Color.FromArgb(bg), Color.FromArgb(arena), Color.FromArgb(grid), Color.FromArgb(snake), Color.FromArgb(head),
            Color.FromArgb(food), Color.FromArgb(golden), Color.FromArgb(obstacle), Color.FromArgb(power), Color.FromArgb(text), false);

    private static Theme Lcd(string name, string bg, string ink) =>
        new(name, Color.FromArgb(bg), Color.FromArgb(bg), Color.FromArgb(bg), Color.FromArgb(ink), Color.FromArgb(ink),
            Color.FromArgb(ink), Color.FromArgb(ink), Color.FromArgb(ink), Color.FromArgb(ink), Color.FromArgb(ink), true);

    public static readonly IReadOnlyList<Theme> All =
    [
        //      name          background arena      grid       snake      head       food       golden     obstacle   power up   text
        Smooth("Neon LCD", "#0B1410", "#0F1C16", "#1A3024", "#39FF88", "#B6FFD5", "#FF4D6D", "#FFD23F", "#5E8C74", "#5CC8FF", "#E8FFF1"),
        Smooth("Ocean",    "#06141F", "#0A1D2C", "#143045", "#4FD1FF", "#C9F1FF", "#FF7A59", "#FFD23F", "#5D8AA3", "#8CFFB0", "#E6F7FF"),
        Smooth("Sunset",   "#1A0F1F", "#22142A", "#35223F", "#FF9E5E", "#FFD9BF", "#5CE1E6", "#FFE45C", "#9A7BAE", "#B69CFF", "#FFF1E8"),
        Smooth("Paper",    "#F4F1E8", "#FBF9F3", "#E3DDCC", "#1E7F4F", "#0F5C36", "#D6334F", "#9A7400", "#7D7462", "#2F62C8", "#1D2420"),
        Smooth("Mono",     "#101010", "#171717", "#262626", "#F2F2F2", "#FFFFFF", "#A8A8A8", "#FFFFFF", "#8A8A8A", "#D0D0D0", "#FFFFFF"),
        Smooth("Forest",   "#0E1A10", "#132416", "#1F3923", "#9BE564", "#E1FFC9", "#FF6F59", "#FFD447", "#6D8F62", "#7CD4FF", "#EEFFE6"),
        Smooth("Candy",    "#1C0E1A", "#261324", "#3A1F37", "#FF7AD9", "#FFD1F2", "#7CFF6B", "#FFE45C", "#A77AA0", "#6BD5FF", "#FFEFFA"),
        Smooth("Midnight", "#0A0B1E", "#10122B", "#1C2045", "#8C9EFF", "#DDE2FF", "#FF7AA2", "#FFD966", "#6F78B3", "#6EF2C4", "#ECEEFF"),
        Smooth("Ice",      "#EAF4FA", "#F7FBFE", "#D4E5F0", "#1F6FB2", "#0E4C80", "#D9465F", "#8C6A00", "#6B8396", "#7B3FC4", "#122533"),
        Smooth("Ember",    "#170B07", "#21100A", "#36190F", "#FF6A3D", "#FFC2A8", "#FFD23F", "#FFF27A", "#A06A54", "#6BE3FF", "#FFEDE6"),
        Smooth("Cyber",    "#0A0612", "#120A1F", "#22143A", "#00F0FF", "#C2FCFF", "#FF2E97", "#F9F871", "#7B61A8", "#B6FF3B", "#F3EDFF"),
        Smooth("Aurora",   "#07131A", "#0B1C26", "#163343", "#7CFFCB", "#D6FFF0", "#C792FF", "#FFE45C", "#5D939B", "#FF9EC7", "#EAFFFA"),
        Smooth("Lava",     "#120707", "#1C0A0A", "#331313", "#FF3B30", "#FFB3AD", "#FFE45C", "#FFFFFF", "#9C5B55", "#4DD8FF", "#FFEDEB"),
        Lcd("Classic LCD", "#C7D3A6", "#2B3A1F"),
        Lcd("Blue Backlight", "#9FC6D9", "#10283A"),
        Lcd("Night", "#101410", "#8FD18F"),
        Lcd("Game Boy", "#9BBC0F", "#0F380F"),
        Lcd("Amber CRT", "#1A1000", "#FFB000"),
    ];
}
