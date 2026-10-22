using TheSnake.Core;

namespace TheSnake.Rendering;

/// <summary>
/// Paints the arena with Microsoft.Maui.Graphics.
/// Smooth themes draw the snake sliding into the cell it will enter next, so what you see is never
/// behind the game: a turn shows on the very next frame. Pixel (Retro 97) themes step cell by cell.
/// The engine always works in portrait cells. In landscape the arena is drawn rotated.
/// </summary>
public sealed class GameDrawable : IDrawable
{
    private const int MaxParticles = 24;

    private readonly Particle[] _particles = new Particle[MaxParticles];
    private float _originX;
    private float _originY;
    private int _cols;
    private int _rows;

    public GameEngine? Engine { get; set; }

    public Theme Theme { get; set; } = Theme.All[0];

    public Look Look { get; set; } = Look.Default;

    /// <summary>How far through the current tick we are, from 0 to 1.</summary>
    public float Interpolation { get; set; }

    public bool Landscape { get; set; }

    public bool ReduceMotion { get; set; }

    /// <summary>0 while alive, rising to 1 over the death effect.</summary>
    public float DeathProgress { get; set; }

    /// <summary>Milliseconds since the run started, for pulsing effects.</summary>
    public double ClockMs { get; set; }

    /// <summary>Size of one cell on screen, worked out on the last draw.</summary>
    public float CellSize { get; private set; }

    private Color SnakeColor => Look.SnakeColor ?? Theme.Snake;

    private Color HeadColor => Look.SnakeColor is { } c ? Theme.Mix(c, Theme.IsLight ? Colors.Black : Colors.White, 0.35f) : Theme.Head;

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

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var engine = Engine;
        canvas.FillColor = Theme.Background;
        canvas.FillRectangle(dirtyRect);
        if (engine is null)
        {
            return;
        }

        _cols = Landscape ? engine.Setup.Height : engine.Setup.Width;
        _rows = Landscape ? engine.Setup.Width : engine.Setup.Height;
        float cell = Math.Min(dirtyRect.Width / _cols, dirtyRect.Height / _rows);
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

        _originX = dirtyRect.X + (dirtyRect.Width - _cols * cell) / 2 + shake;
        _originY = dirtyRect.Y + (dirtyRect.Height - _rows * cell) / 2;

        canvas.Antialias = !Theme.Pixel;
        canvas.FillColor = Theme.Arena;
        canvas.FillRoundedRectangle(_originX, _originY, _cols * cell, _rows * cell, Theme.Pixel ? 0 : cell * 0.6f);
        if (Theme.Pixel)
        {
            canvas.StrokeColor = Theme.Snake;
            canvas.StrokeSize = MathF.Max(1, cell * 0.15f);
            canvas.DrawRectangle(_originX - cell * 0.3f, _originY - cell * 0.3f, _cols * cell + cell * 0.6f, _rows * cell + cell * 0.6f);
        }
        else
        {
            DrawGrid(canvas, cell);
        }

        foreach (var o in engine.Setup.Obstacles)
        {
            DrawBlock(canvas, o.X, o.Y, Theme.Obstacle, 0.08f, Theme.Pixel ? 0 : 0.25f);
        }

        DrawFood(canvas, engine);

        canvas.SaveState();
        canvas.ClipRectangle(_originX, _originY, _cols * cell, _rows * cell);
        if (Theme.Pixel)
        {
            DrawPixelSnake(canvas, engine);
        }
        else
        {
            DrawSmoothSnake(canvas, engine);
        }

        canvas.RestoreState();
        DrawParticles(canvas, cell);
    }

    private void DrawGrid(ICanvas canvas, float cell)
    {
        switch (Look.Grid)
        {
            case GridStyle.Dots:
                canvas.FillColor = Theme.Grid;
                float dot = MathF.Max(1.5f, cell * 0.09f);
                for (int x = 1; x < _cols; x++)
                {
                    for (int y = 1; y < _rows; y++)
                    {
                        canvas.FillRectangle(_originX + x * cell - dot / 2, _originY + y * cell - dot / 2, dot, dot);
                    }
                }

                break;
            case GridStyle.Lines:
                canvas.StrokeColor = Theme.Grid;
                canvas.StrokeSize = 1;
                for (int x = 1; x < _cols; x++)
                {
                    canvas.DrawLine(_originX + x * cell, _originY, _originX + x * cell, _originY + _rows * cell);
                }

                for (int y = 1; y < _rows; y++)
                {
                    canvas.DrawLine(_originX, _originY + y * cell, _originX + _cols * cell, _originY + y * cell);
                }

                break;
        }
    }

    private void DrawFood(ICanvas canvas, GameEngine engine)
    {
        float cell = CellSize;
        float pulse = ReduceMotion ? 0 : (float)Math.Sin(ClockMs / 180.0);
        var f = CellCenter(engine.Food.X, engine.Food.Y);

        if (Theme.Pixel)
        {
            canvas.FillColor = Theme.Food;
            float s = cell * 0.28f;
            canvas.FillRectangle(f.X - s / 2, f.Y - cell * 0.42f, s, s);
            canvas.FillRectangle(f.X - cell * 0.42f, f.Y - s / 2, s, s);
            canvas.FillRectangle(f.X + cell * 0.42f - s, f.Y - s / 2, s, s);
            canvas.FillRectangle(f.X - s / 2, f.Y + cell * 0.42f - s, s, s);
        }
        else
        {
            if (Look.Glow)
            {
                canvas.FillColor = Theme.Food.WithAlpha(0.2f);
                canvas.FillCircle(f.X, f.Y, cell * (0.62f + 0.06f * pulse));
            }

            DrawFoodShape(canvas, f.X, f.Y, cell, Theme.Food);
        }

        if (engine.Golden is { } golden)
        {
            var g = CellCenter(golden.X, golden.Y);
            if (Look.Glow || Theme.Pixel)
            {
                canvas.FillColor = Theme.Golden.WithAlpha(Theme.Pixel ? 0 : 0.25f);
                canvas.FillCircle(g.X, g.Y, cell * (0.75f + 0.1f * pulse));
            }

            canvas.FillColor = Theme.Golden;
            DrawStar(canvas, g.X, g.Y, cell * 0.5f);
        }

        if (engine.PowerUp is { } power)
        {
            var c = CellCenter(power.Position.X, power.Position.Y);
            bool blinkOff = !ReduceMotion && power.RemainingMs < 2500 && ((int)(ClockMs / 150) % 2 == 0);
            if (!blinkOff)
            {
                DrawPowerIcon(canvas, power.Kind, c.X, c.Y, cell);
            }
        }

        if (engine.Critter is { } critter)
        {
            DrawCritter(canvas, critter);
        }
    }

    private void DrawFoodShape(ICanvas canvas, float x, float y, float cell, Color color)
    {
        canvas.FillColor = color;
        switch (Look.Food)
        {
            case FoodStyle.Apple:
                canvas.FillCircle(x, y + cell * 0.04f, cell * 0.34f);
                canvas.StrokeColor = Theme.Mix(color, Theme.Text, 0.5f);
                canvas.StrokeSize = MathF.Max(1.5f, cell * 0.08f);
                canvas.DrawLine(x, y - cell * 0.22f, x + cell * 0.06f, y - cell * 0.42f);
                canvas.FillColor = Theme.Mix(SnakeColor, Theme.Arena, 0.15f);
                canvas.FillEllipse(x + cell * 0.06f, y - cell * 0.44f, cell * 0.26f, cell * 0.14f);
                break;
            case FoodStyle.Gem:
                var gem = new PathF();
                gem.MoveTo(x, y - cell * 0.4f);
                gem.LineTo(x + cell * 0.36f, y - cell * 0.06f);
                gem.LineTo(x, y + cell * 0.4f);
                gem.LineTo(x - cell * 0.36f, y - cell * 0.06f);
                gem.Close();
                canvas.FillPath(gem);
                canvas.FillColor = Colors.White.WithAlpha(0.45f);
                canvas.FillCircle(x - cell * 0.1f, y - cell * 0.12f, cell * 0.07f);
                break;
            default:
                canvas.FillCircle(x, y, cell * 0.36f);
                canvas.FillColor = Colors.White.WithAlpha(0.35f);
                canvas.FillCircle(x - cell * 0.11f, y - cell * 0.11f, cell * 0.09f);
                break;
        }
    }

    private void DrawPixelSnake(ICanvas canvas, GameEngine engine)
    {
        var body = engine.Snake;
        float alpha = DeathProgress > 0 ? 1 - DeathProgress * 0.85f : 1;
        for (int i = body.Count - 1; i >= 0; i--)
        {
            DrawBlock(canvas, body[i].X, body[i].Y, Theme.Snake.WithAlpha(alpha), 0.06f, 0);
        }

        DrawEyes(canvas, body[0].X, body[0].Y, engine.NextHeading, Theme.Background);
    }

    private void DrawSmoothSnake(ICanvas canvas, GameEngine engine)
    {
        var body = engine.Snake;
        int n = body.Count;
        float alpha = DeathProgress > 0 ? 1 - DeathProgress * 0.85f : 1;
        if (engine.ActivePower?.Kind == PowerUpKind.Phase)
        {
            alpha *= 0.55f;
        }

        float t = engine.IsOver ? 0 : Math.Clamp(Interpolation, 0, 1);
        var (inset, radius, bridgeWidth) = Look.Shape switch
        {
            BodyShape.Square => (0.08f, 0.12f, 1f),
            BodyShape.Beads => (0.1f, 0.5f, 0.55f),
            _ => (0.12f, 0.38f, 1f),
        };

        // Work out where the head is heading and whether the tail moves this tick.
        bool grows = false;
        GridPoint? next = null;
        if (!engine.IsOver)
        {
            next = engine.PredictNextCell(out grows);
        }

        (float X, float Y)? wrapEntry = null;
        if (next is null && !engine.IsOver && engine.Setup.Wrap)
        {
            wrapEntry = WrapTarget(engine, out grows);
        }
        else if (next is null)
        {
            grows = true; // Nothing moves, keep the tail still too.
        }

        bool tailSlides = (next is not null || wrapEntry is not null) && !grows && n > 1;

        // Tail first, so the head is drawn on top.
        for (int i = n - 1; i >= 1; i--)
        {
            var color = SegmentColor(i, n).WithAlpha(alpha);
            if (i == n - 1 && tailSlides)
            {
                float tx = Lerp(body[i].X, body[i - 1].X, t);
                float ty = Lerp(body[i].Y, body[i - 1].Y, t);
                if (body[i].ManhattanTo(body[i - 1]) == 1)
                {
                    DrawSegment(canvas, tx, ty, color, inset, radius, i);
                    Bridge(canvas, tx, ty, body[i - 1].X, body[i - 1].Y, color, inset, bridgeWidth);
                }

                continue;
            }

            DrawSegment(canvas, body[i].X, body[i].Y, color, inset, radius, i);
            if (body[i].ManhattanTo(body[i - 1]) == 1)
            {
                Bridge(canvas, body[i].X, body[i].Y, body[i - 1].X, body[i - 1].Y, color, inset, bridgeWidth);
            }
        }

        var headColor = HeadColor.WithAlpha(alpha);
        if (next is { } cellAhead)
        {
            // The head cell stays filled while the head slides out of it.
            DrawSegment(canvas, body[0].X, body[0].Y, SegmentColor(0, n).WithAlpha(alpha), inset, radius, 0);
            float hx = Lerp(body[0].X, cellAhead.X, t);
            float hy = Lerp(body[0].Y, cellAhead.Y, t);
            Bridge(canvas, body[0].X, body[0].Y, hx, hy, SegmentColor(0, n).WithAlpha(alpha), inset, bridgeWidth);
            DrawHead(canvas, hx, hy, headColor, engine.NextHeading, alpha);
        }
        else if (wrapEntry is { } entry)
        {
            // Slide out of one edge and in at the other. The clip hides the parts outside the arena.
            var (dx, dy) = engine.NextHeading.Delta();
            DrawSegment(canvas, body[0].X, body[0].Y, SegmentColor(0, n).WithAlpha(alpha), inset, radius, 0);
            float ox = body[0].X + dx * t, oy = body[0].Y + dy * t;
            Bridge(canvas, body[0].X, body[0].Y, ox, oy, SegmentColor(0, n).WithAlpha(alpha), inset, bridgeWidth);
            DrawHead(canvas, ox, oy, headColor, engine.NextHeading, alpha);
            DrawHead(canvas, entry.X - dx * (1 - t), entry.Y - dy * (1 - t), headColor, engine.NextHeading, alpha);
        }
        else
        {
            DrawHead(canvas, body[0].X, body[0].Y, headColor, engine.NextHeading, alpha);
        }
    }

    private (float X, float Y)? WrapTarget(GameEngine engine, out bool grows)
    {
        grows = false;
        var raw = engine.Head.Step(engine.NextHeading);
        var w = new GridPoint((raw.X + engine.Setup.Width) % engine.Setup.Width, (raw.Y + engine.Setup.Height) % engine.Setup.Height);
        if (w == raw || engine.IsObstacle(w))
        {
            return null;
        }

        grows = w == engine.Food || w == engine.Golden;
        bool tailLeaves = !grows && w == engine.Snake[^1];
        if (engine.IsOccupiedBySnake(w) && !tailLeaves && engine.ActivePower?.Kind != PowerUpKind.Phase)
        {
            return null;
        }

        return (w.X, w.Y);
    }

    private void DrawHead(ICanvas canvas, float gx, float gy, Color color, Direction heading, float alpha)
    {
        if (Look.Glow)
        {
            var hc = CellCenter(gx, gy);
            canvas.FillColor = SnakeColor.WithAlpha(0.2f * alpha);
            canvas.FillCircle(hc.X, hc.Y, CellSize * 0.85f);
        }

        float radius = Look.Shape == BodyShape.Square ? 0.18f : 0.45f;
        DrawBlock(canvas, gx, gy, color, 0.04f, radius);
        DrawEyes(canvas, gx, gy, heading, Theme.Arena);
    }

    private Color SegmentColor(int index, int count)
    {
        var baseColor = SnakeColor;
        return Look.Skin switch
        {
            SkinPattern.Stripes => (index / 2) % 2 == 1 ? Theme.Mix(baseColor, Theme.Arena, 0.3f) : baseColor,
            SkinPattern.Gradient => Theme.Mix(baseColor, Theme.Arena, 0.4f * index / Math.Max(1, count - 1)),
            _ => baseColor,
        };
    }

    private void DrawSegment(ICanvas canvas, float gx, float gy, Color color, float inset, float radius, int index)
    {
        if (Look.Skin == SkinPattern.Outline)
        {
            var (sx, sy) = ToScreen(gx, gy);
            float pad = CellSize * inset;
            canvas.FillColor = Theme.Mix(color, Theme.Arena, 0.7f);
            canvas.FillRoundedRectangle(sx + pad, sy + pad, CellSize - pad * 2, CellSize - pad * 2, CellSize * radius);
            canvas.StrokeColor = color;
            canvas.StrokeSize = MathF.Max(1.5f, CellSize * 0.1f);
            canvas.DrawRoundedRectangle(sx + pad, sy + pad, CellSize - pad * 2, CellSize - pad * 2, CellSize * radius);
            return;
        }

        DrawBlock(canvas, gx, gy, color, inset, radius);
        if (Look.Skin == SkinPattern.Dots && index % 2 == 0)
        {
            var c = CellCenter(gx, gy);
            canvas.FillColor = Theme.Mix(color, Theme.Arena, 0.6f);
            canvas.FillCircle(c.X, c.Y, CellSize * 0.12f);
        }
    }

    private void DrawEyes(ICanvas canvas, float gx, float gy, Direction heading, Color color)
    {
        var c = CellCenter(gx, gy);
        var screenHeading = Landscape ? heading.TurnLeft() : heading;
        var (dx, dy) = screenHeading.Delta();
        float cell = CellSize;
        canvas.FillColor = color;
        float fx = c.X + dx * cell * 0.18f;
        float fy = c.Y + dy * cell * 0.18f;
        float px = -dy * cell * 0.2f;
        float py = dx * cell * 0.2f;
        if (Theme.Pixel)
        {
            float s = MathF.Max(1, MathF.Floor(cell * 0.22f));
            canvas.FillRectangle(fx + px - s / 2, fy + py - s / 2, s, s);
        }
        else
        {
            float s = cell * 0.11f;
            canvas.FillCircle(fx + px, fy + py, s);
            canvas.FillCircle(fx - px, fy - py, s);
        }
    }

    private void DrawPowerIcon(ICanvas canvas, PowerUpKind kind, float x, float y, float cell)
    {
        if (!Theme.Pixel)
        {
            canvas.FillColor = Theme.PowerUp.WithAlpha(0.22f);
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

    /// <summary>Joins two neighbouring segments. <paramref name="width"/> below 1 gives a thinner neck (Beads shape).</summary>
    private void Bridge(ICanvas canvas, float ax, float ay, float bx, float by, Color color, float inset, float width)
    {
        var (x1, y1) = ToScreen(ax, ay);
        var (x2, y2) = ToScreen(bx, by);
        float cell = CellSize;
        float side = cell * (1 - width) / 2 + cell * inset * width;
        bool horizontal = Math.Abs(y1 - y2) < 0.01f;
        float left = Math.Min(x1, x2) + (horizontal ? cell / 2 : side);
        float right = Math.Max(x1, x2) + (horizontal ? cell / 2 : cell - side);
        float top = Math.Min(y1, y2) + (horizontal ? side : cell / 2);
        float bottom = Math.Max(y1, y2) + (horizontal ? cell - side : cell / 2);
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
