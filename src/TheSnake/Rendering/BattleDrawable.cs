using TheSnake.Core;
using TheSnake.Core.Battle;

namespace TheSnake.Rendering;

/// <summary>
/// Draws a Nearby Battle snapshot. Every player has a colour and a body pattern (solid, stripes, dots,
/// outline) so snakes can be told apart without relying on colour. Your own snake glows.
/// </summary>
public sealed class BattleDrawable : IDrawable
{
    private static readonly string[] DarkColors = ["#39FF88", "#FF7AD9", "#FFD23F", "#4FD1FF"];
    private static readonly string[] LightColors = ["#1E7F4F", "#C2185B", "#8A6400", "#1F6FB2"];

    private float _cell;
    private float _originX;
    private float _originY;

    public BattleSnapshot? Snapshot { get; set; }

    public Theme Theme { get; set; } = Theme.All[0];

    public int LocalPlayer { get; set; }

    /// <summary>Your own next direction, drawn at once while the host catches up.</summary>
    public Direction? LocalHeading { get; set; }

    /// <summary>How far through the current tick we are, from 0 to 1.</summary>
    public float Interpolation { get; set; }

    public bool Landscape { get; set; }

    public Color PlayerColor(int index) =>
        Color.FromArgb((Theme.IsLight ? LightColors : DarkColors)[index % 4]);

    public static string PatternName(int index) => (index % 4) switch
    {
        0 => "solid",
        1 => "stripes",
        2 => "dots",
        _ => "outline",
    };

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Theme.Background;
        canvas.FillRectangle(dirtyRect);
        var s = Snapshot;
        if (s is null)
        {
            return;
        }

        int cols = Landscape ? s.Height : s.Width;
        int rows = Landscape ? s.Width : s.Height;
        _cell = Math.Min(dirtyRect.Width / cols, dirtyRect.Height / rows);
        _originX = dirtyRect.X + (dirtyRect.Width - cols * _cell) / 2;
        _originY = dirtyRect.Y + (dirtyRect.Height - rows * _cell) / 2;

        canvas.Antialias = true;
        canvas.FillColor = Theme.Arena;
        canvas.FillRoundedRectangle(_originX, _originY, cols * _cell, rows * _cell, _cell * 0.6f);
        canvas.FillColor = Theme.Grid;
        float dot = MathF.Max(1.5f, _cell * 0.09f);
        for (int x = 1; x < cols; x++)
        {
            for (int y = 1; y < rows; y++)
            {
                canvas.FillRectangle(_originX + x * _cell - dot / 2, _originY + y * _cell - dot / 2, dot, dot);
            }
        }

        foreach (var f in s.Foods)
        {
            var c = Center(s, f.X, f.Y);
            canvas.FillColor = Theme.Food.WithAlpha(0.2f);
            canvas.FillCircle(c.X, c.Y, _cell * 0.6f);
            canvas.FillColor = Theme.Food;
            canvas.FillCircle(c.X, c.Y, _cell * 0.36f);
        }

        canvas.SaveState();
        canvas.ClipRectangle(_originX, _originY, cols * _cell, rows * _cell);
        for (int i = 0; i < s.Snakes.Count; i++)
        {
            if (i != LocalPlayer)
            {
                DrawSnake(canvas, s, i);
            }
        }

        if (LocalPlayer >= 0 && LocalPlayer < s.Snakes.Count)
        {
            DrawSnake(canvas, s, LocalPlayer);
        }

        canvas.RestoreState();
    }

    private void DrawSnake(ICanvas canvas, BattleSnapshot s, int index)
    {
        var snake = s.Snakes[index];
        if (snake.Body.Count == 0)
        {
            return;
        }

        var color = snake.Dropped ? Theme.Obstacle : PlayerColor(index);
        var heading = index == LocalPlayer && LocalHeading is { } predicted && snake.Alive ? predicted : snake.Heading;
        float t = snake.Alive && s.Status == BattleStatus.Running ? Math.Clamp(Interpolation, 0, 1) : 0;

        for (int i = snake.Body.Count - 1; i >= 0; i--)
        {
            var p = snake.Body[i];
            DrawSegment(canvas, s, p.X, p.Y, color, index, i);
            if (i > 0 && snake.Body[i].ManhattanTo(snake.Body[i - 1]) == 1)
            {
                Bridge(canvas, s, p.X, p.Y, snake.Body[i - 1].X, snake.Body[i - 1].Y, color);
            }
        }

        // Head slides toward its next cell, unless that crosses an edge.
        var head = snake.Body[0];
        var next = head.Step(heading);
        float hx = head.X, hy = head.Y;
        if (t > 0 && next.X >= 0 && next.Y >= 0 && next.X < s.Width && next.Y < s.Height)
        {
            hx = head.X + (next.X - head.X) * t;
            hy = head.Y + (next.Y - head.Y) * t;
            Bridge(canvas, s, head.X, head.Y, hx, hy, color);
        }

        var hc = Center(s, hx, hy);
        if (index == LocalPlayer && snake.Alive)
        {
            canvas.FillColor = color.WithAlpha(0.25f);
            canvas.FillCircle(hc.X, hc.Y, _cell * 0.95f);
        }

        Block(canvas, s, hx, hy, Theme.Mix(color, Theme.IsLight ? Colors.Black : Colors.White, 0.3f), 0.04f, 0.45f);
        var screenHeading = Landscape ? heading.TurnLeft() : heading;
        var (dx, dy) = screenHeading.Delta();
        canvas.FillColor = Theme.Arena;
        float px = -dy * _cell * 0.2f, py = dx * _cell * 0.2f;
        float fx = hc.X + dx * _cell * 0.18f, fy = hc.Y + dy * _cell * 0.18f;
        canvas.FillCircle(fx + px, fy + py, _cell * 0.11f);
        canvas.FillCircle(fx - px, fy - py, _cell * 0.11f);
    }

    private void DrawSegment(ICanvas canvas, BattleSnapshot s, float gx, float gy, Color color, int player, int segment)
    {
        switch (player % 4)
        {
            case 1 when segment / 2 % 2 == 1:
                Block(canvas, s, gx, gy, Theme.Mix(color, Theme.Arena, 0.35f), 0.12f, 0.38f);
                break;
            case 2:
                Block(canvas, s, gx, gy, color, 0.12f, 0.38f);
                var c = Center(s, gx, gy);
                canvas.FillColor = Theme.Arena;
                canvas.FillCircle(c.X, c.Y, _cell * 0.13f);
                break;
            case 3:
                var (x, y) = ToScreen(s, gx, gy);
                float pad = _cell * 0.12f;
                canvas.FillColor = Theme.Mix(color, Theme.Arena, 0.7f);
                canvas.FillRoundedRectangle(x + pad, y + pad, _cell - pad * 2, _cell - pad * 2, _cell * 0.38f);
                canvas.StrokeColor = color;
                canvas.StrokeSize = MathF.Max(1.5f, _cell * 0.1f);
                canvas.DrawRoundedRectangle(x + pad, y + pad, _cell - pad * 2, _cell - pad * 2, _cell * 0.38f);
                break;
            default:
                Block(canvas, s, gx, gy, color, 0.12f, 0.38f);
                break;
        }
    }

    private void Block(ICanvas canvas, BattleSnapshot s, float gx, float gy, Color color, float inset, float radius)
    {
        var (x, y) = ToScreen(s, gx, gy);
        float pad = _cell * inset;
        canvas.FillColor = color;
        canvas.FillRoundedRectangle(x + pad, y + pad, _cell - pad * 2, _cell - pad * 2, _cell * radius);
    }

    private void Bridge(ICanvas canvas, BattleSnapshot s, float ax, float ay, float bx, float by, Color color)
    {
        var (x1, y1) = ToScreen(s, ax, ay);
        var (x2, y2) = ToScreen(s, bx, by);
        float side = _cell * 0.12f;
        bool horizontal = Math.Abs(y1 - y2) < 0.01f;
        float left = Math.Min(x1, x2) + (horizontal ? _cell / 2 : side);
        float right = Math.Max(x1, x2) + (horizontal ? _cell / 2 : _cell - side);
        float top = Math.Min(y1, y2) + (horizontal ? side : _cell / 2);
        float bottom = Math.Max(y1, y2) + (horizontal ? _cell - side : _cell / 2);
        canvas.FillColor = color;
        canvas.FillRectangle(left, top, right - left, bottom - top);
    }

    private (float X, float Y) ToScreen(BattleSnapshot s, float gx, float gy) => Landscape
        ? (_originX + gy * _cell, _originY + (s.Width - 1 - gx) * _cell)
        : (_originX + gx * _cell, _originY + gy * _cell);

    private PointF Center(BattleSnapshot s, float gx, float gy)
    {
        var (x, y) = ToScreen(s, gx, gy);
        return new PointF(x + _cell / 2, y + _cell / 2);
    }
}
