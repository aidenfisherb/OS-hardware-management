using OpenCvSharp;

namespace PipeDodger.Game;

public enum GameState { Ready, Playing, GameOver }

/// <summary>
/// The game: bird physics, scrolling pipes, collision, and score, all drawn with OpenCV.
/// Knows nothing about the camera; call Flap() when the player's input fires.
/// </summary>
public sealed class GameScene : IDisposable
{
    public const int Width = 640;
    public const int Height = 480;
    public const int GroundY = 420;
    public const int CharacterX = 150;

    // A little smaller than the image, so a near miss doesn't feel like an unfair hit.
    public const int CharacterRadius = 16;

    // Much gentler than the original game: holding height takes one blink about every 1.15 seconds.
    private const double GravityPixelsPerSecond2 = 450;
    private const double FlapSpeedPixelsPerSecond = 260;
    private const double MaxFallSpeedPixelsPerSecond = 380;

    // Limits one physics step, so a camera stall can't drop the bird through a pipe.
    private const double MaxStepSeconds = 0.1;

    private const int PipeWidth = 70;
    private const int PipeCapOverhang = 6;
    private const int PipeCapHeight = 24;
    private const int PipeGapHeight = 180;
    private const int PipeGapMargin = 40;
    private const int MaxGapChange = 90;
    private const int PipeSpacing = 260;
    private const int PipeCount = 4;
    private const int FirstPipeX = 500;
    private const int ScrollPixelsPerSecond = 90;
    private const int GroundStripeWidth = 32;
    private const int SpriteHeight = 36;
    private const int BackgroundTolerance = 40;

    // OpenCV colors are BGR, not RGB
    private static readonly Scalar Outline = new(40, 50, 40);
    private static readonly Scalar SkyTop = new(235, 180, 90);
    private static readonly Scalar SkyBottom = new(250, 235, 190);
    private static readonly Scalar Cloud = new(255, 255, 255);
    private static readonly Scalar FarHill = new(170, 200, 130);
    private static readonly Scalar NearHill = new(120, 185, 95);
    private static readonly Scalar Dirt = new(120, 190, 225);
    private static readonly Scalar Grass = new(70, 200, 120);
    private static readonly Scalar GrassDark = new(50, 165, 90);
    private static readonly Scalar SpriteBackgroundKey = new(255, 0, 255);

    private readonly record struct PipeColors(Scalar Base, Scalar Light, Scalar Dark);

    private static readonly PipeColors RedPipe = new(new(50, 50, 215), new(125, 125, 250), new(30, 30, 150));
    private static readonly PipeColors BluePipe = new(new(210, 100, 40), new(250, 175, 115), new(140, 60, 20));

    private sealed class Pipe
    {
        public double X;
        public int GapCenterY;
        public PipeColors Colors;
        public bool Scored;
    }

    private readonly Random _random = new();
    private readonly List<Pipe> _pipes = new();
    private readonly Mat _background;
    private readonly Mat _sprite;
    private readonly Mat _spriteMask;
    private readonly Mat _canvas = new();
    private double _scrollOffset;
    private double _velocityY;
    private double _readySeconds;

    public GameScene()
    {
        (_sprite, _spriteMask) = LoadCharacterSprite();
        _background = BuildBackground();
        Restart();
    }

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public double CharacterY { get; private set; }

    // ---------- Player actions ----------

    /// <summary>Back to the start screen with a new set of pipes.</summary>
    public void Restart()
    {
        State = GameState.Ready;
        Score = 0;
        CharacterY = GroundY / 2.0;
        _velocityY = 0;
        _readySeconds = 0;

        _pipes.Clear();
        int gapCenter = GroundY / 2;
        for (int i = 0; i < PipeCount; i++)
        {
            gapCenter = RandomGapCenter(gapCenter);
            _pipes.Add(new Pipe
            {
                X = FirstPipeX + i * PipeSpacing,
                GapCenterY = gapCenter,
                Colors = i % 2 == 0 ? RedPipe : BluePipe,
            });
        }
    }

    /// <summary>Sets the bird's speed straight upward, whatever it was, as in the original game. The first flap starts the game.</summary>
    public void Flap()
    {
        if (State == GameState.GameOver)
            return;

        State = GameState.Playing;
        _velocityY = -FlapSpeedPixelsPerSecond;
    }

    // ---------- Update ----------

    public void Update(double deltaSeconds)
    {
        double step = Math.Min(deltaSeconds, MaxStepSeconds);

        switch (State)
        {
            case GameState.Ready:
                // hover on the start screen so the game looks alive before the first flap
                _scrollOffset += ScrollPixelsPerSecond * step;
                _readySeconds += step;
                CharacterY = GroundY / 2.0 + Math.Sin(_readySeconds * 4) * 8;
                break;

            case GameState.Playing:
                _scrollOffset += ScrollPixelsPerSecond * step;
                MoveCharacter(step);
                MovePipes(ScrollPixelsPerSecond * step);
                UpdateScore();
                if (CharacterHitSomething())
                    State = GameState.GameOver;
                break;
        }
    }

    private void MoveCharacter(double step)
    {
        // gravity changes the speed, then the speed changes the position
        _velocityY = Math.Min(_velocityY + GravityPixelsPerSecond2 * step, MaxFallSpeedPixelsPerSecond);
        CharacterY += _velocityY * step;

        // the sky is a ceiling, so the bird can't fly over the pipes
        if (CharacterY < CharacterRadius)
        {
            CharacterY = CharacterRadius;
            _velocityY = 0;
        }
    }

    /// <summary>A pipe that leaves on the left returns on the right with a new gap.</summary>
    private void MovePipes(double distance)
    {
        for (int i = 0; i < _pipes.Count; i++)
        {
            Pipe pipe = _pipes[i];
            pipe.X -= distance;
            if (pipe.X + PipeWidth + PipeCapOverhang < 0)
            {
                pipe.X += PipeCount * PipeSpacing;
                pipe.Scored = false;

                // the pipe ahead of it in the row is the previous one in the list
                pipe.GapCenterY = RandomGapCenter(_pipes[(i + PipeCount - 1) % PipeCount].GapCenterY);
            }
        }
    }

    // Each gap stays near the previous one, so the climb to it is always possible at this pipe spacing.
    private int RandomGapCenter(int previousCenter)
    {
        int lowest = PipeGapMargin + PipeGapHeight / 2;
        int highest = GroundY - PipeGapMargin - PipeGapHeight / 2;
        int min = Math.Max(lowest, previousCenter - MaxGapChange);
        int max = Math.Min(highest, previousCenter + MaxGapChange);
        return _random.Next(min, max + 1);
    }

    private void UpdateScore()
    {
        foreach (var pipe in _pipes)
        {
            if (!pipe.Scored && pipe.X + PipeWidth < CharacterX)
            {
                pipe.Scored = true;
                Score++;
            }
        }
    }

    // ---------- Collision ----------

    private bool CharacterHitSomething()
    {
        if (CharacterY + CharacterRadius >= GroundY)
        {
            CharacterY = GroundY - CharacterRadius;
            return true;
        }

        foreach (var pipe in _pipes)
        {
            foreach (Rect part in PipeParts(pipe))
            {
                if (CircleTouchesRect(CharacterX, CharacterY, CharacterRadius, part))
                    return true;
            }
        }
        return false;
    }

    // the closest point of the rectangle to the circle's center decides whether they touch
    private static bool CircleTouchesRect(double x, double y, double radius, Rect rect)
    {
        double dx = x - Math.Clamp(x, rect.Left, rect.Right);
        double dy = y - Math.Clamp(y, rect.Top, rect.Bottom);
        return dx * dx + dy * dy <= radius * radius;
    }

    // ---------- Render ----------

    /// <summary>
    /// Draws one game frame into a buffer this class reuses every call.
    /// The returned Mat is overwritten by the next render, so call Clone() to keep a copy.
    /// </summary>
    public Mat Render(string? banner = null, string? status = null)
    {
        // the background never changes, so copy it instead of redrawing the gradient every frame
        _background.CopyTo(_canvas);

        foreach (var pipe in _pipes)
            DrawPipe(_canvas, pipe);

        // drawn after the pipes so they look planted in the ground
        DrawGround(_canvas, (int)_scrollOffset);
        DrawCharacter((int)CharacterY);

        if (State != GameState.Ready)
            DrawCenteredText(Score.ToString(), 60, 1.6, 3);
        if (banner is not null)
            DrawCenteredText(banner, GroundY / 2 - 50, 0.9, 2);
        if (status is not null)
            DrawText(status, new Point(12, Height - 14), 0.6, 1);

        return _canvas;
    }

    private void DrawCenteredText(string text, int baselineY, double scale, int thickness)
    {
        Size size = Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, scale, thickness, out _);
        DrawText(text, new Point((Width - size.Width) / 2, baselineY), scale, thickness);
    }

    // Dark copies shifted around the text keep it readable on any background. They use the same
    // thickness because in OpenCV 5 thickness also changes letter widths, so a thicker copy wouldn't line up.
    private void DrawText(string text, Point position, double scale, int thickness)
    {
        foreach (var (dx, dy) in new[] { (-2, 0), (2, 0), (0, -2), (0, 2) })
            Cv2.PutText(_canvas, text, new Point(position.X + dx, position.Y + dy), HersheyFonts.HersheySimplex, scale, Outline, thickness, LineTypes.AntiAlias);
        Cv2.PutText(_canvas, text, position, HersheyFonts.HersheySimplex, scale, Cloud, thickness, LineTypes.AntiAlias);
    }

    // ---------- Background ----------

    private static Mat BuildBackground()
    {
        var image = new Mat(Height, Width, MatType.CV_8UC3);

        // sky gradient: one horizontal line per row, blended from top color to bottom color
        for (int y = 0; y < GroundY; y++)
        {
            double t = (double)y / GroundY;
            var color = new Scalar(
                SkyTop.Val0 + (SkyBottom.Val0 - SkyTop.Val0) * t,
                SkyTop.Val1 + (SkyBottom.Val1 - SkyTop.Val1) * t,
                SkyTop.Val2 + (SkyBottom.Val2 - SkyTop.Val2) * t);
            Cv2.Line(image, new Point(0, y), new Point(Width - 1, y), color);
        }

        DrawCloud(image, 90, 80);
        DrawCloud(image, 330, 50);
        DrawCloud(image, 540, 110);

        // hills are ellipses centered low enough that only the tops show above the ground
        DrawFilledEllipse(image, new Point(110, GroundY), new Size(190, 90), FarHill);
        DrawFilledEllipse(image, new Point(520, GroundY), new Size(230, 110), FarHill);
        DrawFilledEllipse(image, new Point(330, GroundY), new Size(170, 60), NearHill);
        return image;
    }

    private static void DrawCloud(Mat image, int x, int y)
    {
        Cv2.Circle(image, new Point(x - 26, y + 4), 16, Cloud, -1, LineTypes.AntiAlias);
        Cv2.Circle(image, new Point(x, y - 6), 22, Cloud, -1, LineTypes.AntiAlias);
        Cv2.Circle(image, new Point(x + 26, y + 4), 16, Cloud, -1, LineTypes.AntiAlias);
        Cv2.Rectangle(image, new Rect(x - 26, y + 4, 52, 16), Cloud, -1);
    }

    private static void DrawGround(Mat image, int scrollOffset)
    {
        Cv2.Rectangle(image, new Rect(0, GroundY, Width, Height - GroundY), Dirt, -1);
        Cv2.Rectangle(image, new Rect(0, GroundY, Width, 14), Grass, -1);

        // stripes shift with the pipes, which makes the ground look like it is moving
        int firstStripe = -(scrollOffset % (GroundStripeWidth * 2));
        for (int x = firstStripe; x < Width; x += GroundStripeWidth * 2)
            Cv2.Rectangle(image, new Rect(x, GroundY, GroundStripeWidth, 14), GrassDark, -1);

        Cv2.Line(image, new Point(0, GroundY), new Point(Width - 1, GroundY), Outline, 2);
        Cv2.Line(image, new Point(0, GroundY + 14), new Point(Width - 1, GroundY + 14), Outline, 2);
    }

    // ---------- Pipes ----------

    // the same rectangles are drawn and used for collision, so what you see is what you hit
    private static Rect[] PipeParts(Pipe pipe)
    {
        int x = (int)pipe.X;
        int gapTop = pipe.GapCenterY - PipeGapHeight / 2;
        int gapBottom = pipe.GapCenterY + PipeGapHeight / 2;

        return new[]
        {
            new Rect(x, 0, PipeWidth, gapTop),
            new Rect(x - PipeCapOverhang, gapTop - PipeCapHeight, PipeWidth + PipeCapOverhang * 2, PipeCapHeight),
            new Rect(x, gapBottom, PipeWidth, GroundY - gapBottom),
            new Rect(x - PipeCapOverhang, gapBottom, PipeWidth + PipeCapOverhang * 2, PipeCapHeight),
        };
    }

    private static void DrawPipe(Mat image, Pipe pipe)
    {
        foreach (Rect part in PipeParts(pipe))
            DrawPipeBody(image, part, pipe.Colors);
    }

    // a light strip and a dark strip make the flat rectangle look round
    private static void DrawPipeBody(Mat image, Rect area, PipeColors colors)
    {
        Cv2.Rectangle(image, area, colors.Base, -1);
        Cv2.Rectangle(image, new Rect(area.X + 6, area.Y, 10, area.Height), colors.Light, -1);
        Cv2.Rectangle(image, new Rect(area.Right - 16, area.Y, 16, area.Height), colors.Dark, -1);
        Cv2.Rectangle(image, area, Outline, 2);
    }

    // ---------- Character ----------

    /// <summary>Loads the bird image and builds a mask marking which of its pixels belong to the bird.</summary>
    private static (Mat Sprite, Mat Mask) LoadCharacterSprite()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "assets", "bird.png");
        using var source = Cv2.ImRead(path, ImreadModes.Color);
        if (source.Empty())
            throw new FileNotFoundException("Could not load the character image.", path);

        // The image has a white background, but the eye and wing are white too, so a plain
        // color test would cut holes in the bird. Filling inward from the corners only reaches
        // the background, because the black outline stops the fill.
        using var keyed = source.Clone();
        int right = source.Width - 1;
        int bottom = source.Height - 1;
        var tolerance = Scalar.All(BackgroundTolerance);
        foreach (var corner in new[] { new Point(0, 0), new Point(right, 0), new Point(0, bottom), new Point(right, bottom) })
            Cv2.FloodFill(keyed, corner, SpriteBackgroundKey, out _, tolerance, tolerance, FloodFillFlags.FixedRange);

        using var background = new Mat();
        using var opaque = new Mat();
        Cv2.InRange(keyed, SpriteBackgroundKey, SpriteBackgroundKey, background);
        Cv2.BitwiseNot(background, opaque);

        // crop the empty space, then shrink; nearest-neighbor keeps the pixel-art edges sharp
        Rect bounds = Cv2.BoundingRect(opaque);
        var size = new Size(bounds.Width * SpriteHeight / bounds.Height, SpriteHeight);
        using var croppedSource = new Mat(source, bounds);
        using var croppedMask = new Mat(opaque, bounds);

        var sprite = new Mat();
        var mask = new Mat();
        Cv2.Resize(croppedSource, sprite, size, 0, 0, InterpolationFlags.Nearest);
        Cv2.Resize(croppedMask, mask, size, 0, 0, InterpolationFlags.Nearest);
        return (sprite, mask);
    }

    private void DrawCharacter(int centerY)
    {
        var spriteArea = new Rect(CharacterX - _sprite.Width / 2, centerY - _sprite.Height / 2, _sprite.Width, _sprite.Height);

        // a bird partly off screen is clipped, since OpenCV can't copy outside the image
        Rect visible = spriteArea & new Rect(0, 0, Width, Height);
        if (visible.Width <= 0 || visible.Height <= 0)
            return;

        var spritePart = new Rect(visible.X - spriteArea.X, visible.Y - spriteArea.Y, visible.Width, visible.Height);
        using var target = new Mat(_canvas, visible);
        using var source = new Mat(_sprite, spritePart);
        using var mask = new Mat(_spriteMask, spritePart);
        source.CopyTo(target, mask);
    }

    private static void DrawFilledEllipse(Mat image, Point center, Size axes, Scalar color) =>
        Cv2.Ellipse(image, center, axes, 0, 0, 360, color, -1, LineTypes.AntiAlias);

    // ---------- Cleanup ----------

    public void Dispose()
    {
        _background.Dispose();
        _sprite.Dispose();
        _spriteMask.Dispose();
        _canvas.Dispose();
    }
}
