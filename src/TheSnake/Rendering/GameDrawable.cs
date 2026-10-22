using TheSnake.Core;

namespace TheSnake.Rendering;

/// <summary>
/// Paints the arena with Microsoft.Maui.Graphics. Smooth themes slide the snake between cells using
/// <see cref="Interpolation"/>; pixel (Retro 97) themes step cell by cell like the old LCD.
/// The engine always works in portrait cells. In landscape the arena is drawn rotated.
/// </summary>
public sealed class GameDrawable : IDrawable
{
    private const int MaxParticles = 24;

    private readonly Particle[] _particles = new Particle[MaxParticles];

    public GameEngine? Engine { get; set; }

    public Theme Theme { get; set; } = Theme.All[0];

    /// <summary>How far through the current tick we are, from 0 to 1.</summary>
    public float Interpolation { get; set; } = 1;

    public bool Landscape { get; set; }

    public bool ReduceMotion { get; set; }

    /// <summary>0 while alive, rising to 1 over the death effect.</summary>
    public float DeathProgress { get; set; }

    /// <summary>Milliseconds since the run started, for pulsing effects.</summary>
    public double ClockMs { get; set; }

    /// <summary>Size of one cell on screen, worked out on the last draw.</summary>
    public float CellSize { get; private set; }

    public void Burst(GridPoint cell, Color color)
    {
        if (ReduceMotion || Theme.Pixel)
        {
            return;
        }

        var center = CellCenter(cell.X, cell.Y);
        var random = Random.Shared;
        for (int i = 0; i < 12; i++)
        {
            int slot = Array.FindIndex(_particles, p => p.Life <= 0);
            if (slot < 0)
            {
                return;
            }

            double angle = random.NextDouble() * Math.PI * 2;
            float speed = (float)(0.05 + random.NextDouble() * 0.12);
            _particles[slot] = new Particle(center.X, center.Y, (float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed, 1f, color);
        }
    }

    public void Advance(float dtMs)
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            if (p.Life <= 0)
            {
                continue;
            }

            p.X += p.Vx * dtMs * CellSize / 16f;
            p.Y += p.Vy * dtMs * CellSize / 16f;
            p.Life -= dtMs / 450f;
        }
    }

    private float _originX;
    private float _originY;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var engine = Engine;
        canvas.FillColor = Theme.Background;
        canvas.FillRectangle(dirtyRect);
        if (engine is null)
        {
            return;
        }

        int cols = Landscape ? engine.Setup.Height : engine.Setup.Width;
        int rows = Landscape ? engine.Setup.Width : engine.Setup.Height;
        float cell = Math.Min(dirtyRect.Width / cols, dirtyRect.Height / rows);
        if (Theme.Pixel)
        {
            cell = MathF.Max(1, MathF.Floor(cell));
        }

        CellSize = cell;
        float shake = 0;
        if (DeathProgress > 0 && !ReduceMotion && !Theme.Pixel)
        {
            shake = MathF.Sin(DeathProgress * 40) * cell * 0.3f * (1 - DeathProgress);
        }

        _originX = dirtyRect.X + (dirtyRect.Width - cols * cell) / 2 + shake;
        _originY = dirtyRect.Y + (dirtyRect.Height - rows * cell) / 2;

        canvas.Antialias = !Theme.Pixel;
        canvas.FillColor = Theme.Arena;
        canvas.FillRoundedRectangle(_originX, _originY, cols * cell, rows * cell, Theme.Pixel ? 0 : cell * 0.6f);
        if (Theme.Pixel)
        {
            canvas.StrokeColor = Theme.Snake;
            canvas.StrokeSize = MathF.Max(1, cell * 0.15f);
            canvas.DrawRectangle(_originX - cell * 0.3f, _originY - cell * 0.3f, cols * cell + cell * 0.6f, rows * cell + cell * 0.6f);
        }
        else
        {
            DrawGrid(canvas, cols, rows, cell);
        }

        foreach (var o in engine.Setup.Obstacles)
        {
            DrawBlock(canvas, o.X, o.Y, Theme.Obstacle, 0.08f, 0.25f);
        }

        DrawFood(canvas, engine);
        DrawSnake(canvas, engine);
        DrawParticles(canvas, cell);
    }

    private void DrawGrid(ICanvas canvas, int cols, int rows, float cell)
    {
        canvas.FillColor = Theme.Grid;
        float dot = MathF.Max(1, cell * 0.08f);
        for (int x = 1; x < cols; x++)
        {
            for (int y = 1; y < rows; y++)
            {
                canvas.FillRectangle(_originX + x * cell - dot / 2, _originY + y * cell - dot / 2, dot, dot);
            }
        }
    }

    private void DrawFood(ICanvas canvas, GameEngine engine)
    {
        float cell = CellSize;
        double pulse = ReduceMotion ? 0 : Math.Sin(ClockMs / 180.0);

        if (Theme.Pixel)
        {
            var f = CellCenter(engine.Food.X, engine.Food.Y);
            canvas.FillColor = Theme.Food;
            float s = cell * 0.28f;
            canvas.FillRectangle(f.X - s / 2, f.Y - cell * 0.42f, s, s);
            canvas.FillRectangle(f.X - cell * 0.42f, f.Y - s / 2, s, s);
            canvas.FillRectangle(f.X + cell * 0.42f - s, f.Y - s / 2, s, s);
            canvas.FillRectangle(f.X - s / 2, f.Y + cell * 0.42f - s, s, s);
        }
        else
        {
            var f = CellCenter(engine.Food.X, engine.Food.Y);
            canvas.FillColor = Theme.Food.WithAlpha(0.22f);
            canvas.FillCircle(f.X, f.Y, cell * (0.62f + 0.06f * (float)pulse));
            canvas.FillColor = Theme.Food;
            canvas.FillCircle(f.X, f.Y, cell * 0.36f);
        }

        if (engine.Golden is { } golden)
        {
            var g = CellCenter(golden.X, golden.Y);
            canvas.FillColor = Theme.Golden.WithAlpha(0.25f);
            canvas.FillCircle(g.X, g.Y, cell * (0.75f + 0.1f * (float)pulse));
            canvas.FillColor = Theme.Golden;
            DrawStar(canvas, g.X, g.Y, cell * 0.48f);
        }

        if (engine.PowerUp is { } power)
        {
            var c = CellCenter(power.Position.X, power.Position.Y);
            bool fading = power.RemainingMs < 2500 && ((int)(ClockMs / 150) % 2 == 0);
            if (!fading || ReduceMotion)
            {
                DrawPowerIcon(canvas, power.Kind, c.X, c.Y, cell);
            }
        }

        if (engine.Critter is { } critter)
        {
            DrawCritter(canvas, critter);
        }
    }

    private void DrawSnake(ICanvas canvas, GameEngine engine)
    {
        var body = engine.Snake;
        float alpha = DeathProgress > 0 ? 1 - DeathProgress * 0.85f : 1;
        var bodyColor = Theme.Snake.WithAlpha(alpha);
        var headColor = Theme.Head.WithAlpha(alpha);

        if (engine.ActivePower?.Kind == PowerUpKind.Phase && !Theme.Pixel)
        {
            bodyColor = bodyColor.WithAlpha(alpha * 0.55f);
        }

        if (Theme.Pixel)
        {
            for (int i = body.Count - 1; i >= 0; i--)
            {
                DrawBlock(canvas, body[i].X, body[i].Y, i == 0 ? headColor : bodyColor, 0.06f, 0);
            }

            DrawEye(canvas, body[0].X, body[0].Y, engine.Heading, Theme.Background);
            return;
        }

        float t = DeathProgress > 0 ? 1 : Math.Clamp(Interpolation, 0, 1);
        const float inset = 0.12f;
        const float radius = 0.38f;

        // Body cells and the bridges that join them.
        for (int i = body.Count - 1; i >= 1; i--)
        {
            DrawBlock(canvas, body[i].X, body[i].Y, bodyColor, inset, radius);
            if (i < body.Count - 1 && body[i].ManhattanTo(body[i + 1]) == 1)
            {
                Bridge(canvas, body[i].X, body[i].Y, body[i + 1].X, body[i + 1].Y, bodyColor, inset);
            }
        }

        // Tail sliding out of the cell it just left.
        var tail = body[^1];
        if (engine.PreviousTail is { } prev && prev.ManhattanTo(tail) == 1 && body.Count > 1)
        {
            float tx = Lerp(prev.X, tail.X, t);
            float ty = Lerp(prev.Y, tail.Y, t);
            DrawBlock(canvas, tx, ty, bodyColor, inset, radius);
            Bridge(canvas, tx, ty, tail.X, tail.Y, bodyColor, inset);
        }

        // Head sliding into its new cell.
        float hx = body[0].X, hy = body[0].Y;
        if (body.Count > 1 && !engine.LastMoveWrapped && body[0].ManhattanTo(body[1]) == 1)
        {
            hx = Lerp(body[1].X, body[0].X, t);
            hy = Lerp(body[1].Y, body[0].Y, t);
            Bridge(canvas, body[1].X, body[1].Y, hx, hy, bodyColor, inset);
        }

        var hc = CellCenter(hx, hy);
        canvas.FillColor = Theme.Snake.WithAlpha(0.18f * alpha);
        canvas.FillCircle(hc.X, hc.Y, CellSize * 0.85f);
        DrawBlock(canvas, hx, hy, headColor, 0.04f, 0.45f);
        DrawEye(canvas, hx, hy, engine.Heading, Theme.Background);
    }

    private void DrawEye(ICanvas canvas, float gx, float gy, Direction heading, Color color)
    {
        var c = CellCenter(gx, gy);
        var screenHeading = Landscape ? heading.TurnLeft() : heading;
        var (dx, dy) = screenHeading.Delta();
        float cell = CellSize;
        canvas.FillColor = color;
        float s = Theme.Pixel ? MathF.Max(1, MathF.Floor(cell * 0.22f)) : cell * 0.11f;
        // Two eyes, set forward and to each side of the heading.
        float fx = c.X + dx * cell * 0.18f;
        float fy = c.Y + dy * cell * 0.18f;
        float px = -dy * cell * 0.2f;
        float py = dx * cell * 0.2f;
        if (Theme.Pixel)
        {
            canvas.FillRectangle(fx + px - s / 2, fy + py - s / 2, s, s);
        }
        else
        {
            canvas.FillCircle(fx + px, fy + py, s);
            canvas.FillCircle(fx - px, fy - py, s);
        }
    }

    private void DrawPowerIcon(ICanvas canvas, PowerUpKind kind, float x, float y, float cell)
    {
        canvas.FillColor = Theme.PowerUp.WithAlpha(Theme.Pixel ? 1 : 0.25f);
        if (!Theme.Pixel)
        {
            canvas.FillCircle(x, y, cell * 0.7f);
        }

        canvas.StrokeColor = Theme.PowerUp;
        canvas.FillColor = Theme.PowerUp;
        canvas.StrokeSize = MathF.Max(1.5f, cell * 0.1f);
        canvas.StrokeLineCap = LineCap.Round;
        float r = cell * 0.4f;
        switch (kind)
        {
            case PowerUpKind.SlowTime:
                var glass = new PathF();
                glass.MoveTo(x - r, y - r);
                glass.LineTo(x + r, y - r);
                glass.LineTo(x - r, y + r);
                glass.LineTo(x + r, y + r);
                glass.Close();
                canvas.FillPath(glass);
                break;
            case PowerUpKind.Phase:
                var ghost = new PathF();
                ghost.MoveTo(x - r, y + r);
                ghost.LineTo(x - r, y);
                ghost.AddArc(x - r, y - r, x + r, y + r, 180, 0, true);
                ghost.LineTo(x + r, y + r);
                ghost.LineTo(x + r / 3, y + r * 0.6f);
                ghost.LineTo(x - r / 3, y + r);
                ghost.Close();
                canvas.FillPath(ghost);
                break;
            case PowerUpKind.Magnet:
                canvas.DrawArc(x - r, y - r, r * 2, r * 2, 180, 0, false, false);
                canvas.DrawLine(x - r, y, x - r, y - r * 0.9f);
                canvas.DrawLine(x + r, y, x + r, y - r * 0.9f);
                break;
            case PowerUpKind.Double:
                canvas.FontColor = Theme.PowerUp;
                canvas.FontSize = cell * 0.75f;
                canvas.DrawString("x2", x - cell, y - cell / 2, cell * 2, cell, HorizontalAlignment.Center, VerticalAlignment.Center);
                break;
            case PowerUpKind.Trim:
                canvas.DrawLine(x - r, y - r, x + r * 0.6f, y + r * 0.6f);
                canvas.DrawLine(x + r, y - r, x - r * 0.6f, y + r * 0.6f);
                canvas.DrawCircle(x - r * 0.75f, y + r * 0.75f, r * 0.3f);
                canvas.DrawCircle(x + r * 0.75f, y + r * 0.75f, r * 0.3f);
                break;
        }
    }

    private void DrawCritter(ICanvas canvas, Critter critter)
    {
        // A small two cell pixel sprite. Shapes vary so the critter reads without colour.
        var a = CellCenter(critter.Left.X, critter.Left.Y);
        var b = CellCenter(critter.Right.X, critter.Right.Y);
        canvas.FillColor = Theme.Food;
        float s = CellSize * 0.3f;
        int shape = critter.Shape;
        foreach (var (c, flip) in new[] { (a, false), (b, true) })
        {
            canvas.FillRectangle(c.X - s, c.Y - s / 2, s * 2, s);
            float leg = flip ? s : -s;
            if (shape % 2 == 0)
            {
                canvas.FillRectangle(c.X + leg - s / 4, c.Y - s * 1.3f, s / 2, s / 2);
            }

            if (shape % 3 == 0)
            {
                canvas.FillRectangle(c.X - s / 4, c.Y + s * 0.8f, s / 2, s / 2);
            }
        }
    }

    private static void DrawStar(ICanvas canvas, float x, float y, float r)
    {
        var star = new PathF();
        for (int i = 0; i < 10; i++)
        {
            double angle = -Math.PI / 2 + i * Math.PI / 5;
            float rr = i % 2 == 0 ? r : r * 0.45f;
            float px = x + (float)Math.Cos(angle) * rr;
            float py = y + (float)Math.Sin(angle) * rr;
            if (i == 0) star.MoveTo(px, py); else star.LineTo(px, py);
        }

        star.Close();
        canvas.FillPath(star);
    }

    private void DrawParticles(ICanvas canvas, float cell)
    {
        foreach (var p in _particles)
        {
            if (p.Life <= 0)
            {
                continue;
            }

            canvas.FillColor = p.Color.WithAlpha(Math.Clamp(p.Life, 0, 1));
            canvas.FillCircle(p.X, p.Y, cell * 0.12f * (0.5f + p.Life));
        }
    }

    private void DrawBlock(ICanvas canvas, float gx, float gy, Color color, float inset, float radius)
    {
        var (sx, sy) = ToScreen(gx, gy);
        float cell = CellSize;
        float pad = Theme.Pixel ? MathF.Max(1, MathF.Round(cell * inset)) : cell * inset;
        canvas.FillColor = color;
        if (radius <= 0)
        {
            canvas.FillRectangle(sx + pad, sy + pad, cell - pad * 2, cell - pad * 2);
        }
        else
        {
            canvas.FillRoundedRectangle(sx + pad, sy + pad, cell - pad * 2, cell - pad * 2, cell * radius);
        }
    }

    private void Bridge(ICanvas canvas, float ax, float ay, float bx, float by, Color color, float inset)
    {
        var (x1, y1) = ToScreen(ax, ay);
        var (x2, y2) = ToScreen(bx, by);
        float cell = CellSize;
        float pad = cell * inset;
        float left = Math.Min(x1, x2) + pad;
        float top = Math.Min(y1, y2) + pad;
        float right = Math.Max(x1, x2) + cell - pad;
        float bottom = Math.Max(y1, y2) + cell - pad;
        canvas.FillColor = color;
        canvas.FillRectangle(left, top, right - left, bottom - top);
    }

    private (float X, float Y) ToScreen(float gx, float gy)
    {
        if (Landscape && Engine is { } engine)
        {
            return (_originX + gy * CellSize, _originY + (engine.Setup.Width - 1 - gx) * CellSize);
        }

        return (_originX + gx * CellSize, _originY + gy * CellSize);
    }

    private PointF CellCenter(float gx, float gy)
    {
        var (x, y) = ToScreen(gx, gy);
        return new PointF(x + CellSize / 2, y + CellSize / 2);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private struct Particle(float x, float y, float vx, float vy, float life, Color color)
    {
        public float X = x;
        public float Y = y;
        public float Vx = vx;
        public float Vy = vy;
        public float Life = life;
        public Color Color = color;
    }
}
