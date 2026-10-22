namespace TheSnake.Rendering;

/// <summary>
/// Colours for one theme. Pixel themes draw square LCD blocks (Retro 97), the others draw rounded,
/// smooth, glowing shapes. Snake, food and obstacles keep at least 3 to 1 contrast with the background.
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
    public static Theme Get(string name) => All.FirstOrDefault(t => t.Name == name) ?? All[0];

    private static Theme Smooth(string name, string bg, string arena, string grid, string snake, string head, string food, string golden, string obstacle, string power, string text) =>
        new(name, Color.FromArgb(bg), Color.FromArgb(arena), Color.FromArgb(grid), Color.FromArgb(snake), Color.FromArgb(head),
            Color.FromArgb(food), Color.FromArgb(golden), Color.FromArgb(obstacle), Color.FromArgb(power), Color.FromArgb(text), false);

    private static Theme Lcd(string name, string bg, string ink) =>
        new(name, Color.FromArgb(bg), Color.FromArgb(bg), Color.FromArgb(bg), Color.FromArgb(ink), Color.FromArgb(ink),
            Color.FromArgb(ink), Color.FromArgb(ink), Color.FromArgb(ink), Color.FromArgb(ink), Color.FromArgb(ink), true);

    public static readonly IReadOnlyList<Theme> All =
    [
        Smooth("Neon LCD", "#0B1410", "#0F1C16", "#16281F", "#39FF88", "#B6FFD5", "#FF4D6D", "#FFD23F", "#4E7A63", "#5CC8FF", "#E8FFF1"),
        Lcd("Classic LCD", "#C7D3A6", "#2B3A1F"),
        Lcd("Blue Backlight", "#9FC6D9", "#10283A"),
        Lcd("Night", "#101410", "#8FD18F"),
        Smooth("Sunset", "#1A0F1F", "#22142A", "#2E1C38", "#FF9E5E", "#FFD9BF", "#5CE1E6", "#FFE45C", "#8A6A9E", "#B69CFF", "#FFF1E8"),
        Smooth("Ocean", "#06141F", "#0A1D2C", "#10293D", "#4FD1FF", "#C9F1FF", "#FF7A59", "#FFD23F", "#4D7891", "#8CFFB0", "#E6F7FF"),
        Smooth("Candy", "#1C0E1A", "#261324", "#331A30", "#FF7AD9", "#FFD1F2", "#7CFF6B", "#FFE45C", "#9A6C92", "#6BD5FF", "#FFEFFA"),
        Smooth("Mono", "#101010", "#171717", "#222222", "#F2F2F2", "#FFFFFF", "#A8A8A8", "#FFFFFF", "#808080", "#D0D0D0", "#FFFFFF"),
        Smooth("Ember", "#170B07", "#21100A", "#2D170E", "#FF6A3D", "#FFC2A8", "#FFD23F", "#FFF27A", "#8F5A45", "#6BE3FF", "#FFEDE6"),
        Smooth("Aurora", "#07131A", "#0B1C26", "#122835", "#7CFFCB", "#D6FFF0", "#C792FF", "#FFE45C", "#4F7F86", "#FF9EC7", "#EAFFFA"),
    ];
}
