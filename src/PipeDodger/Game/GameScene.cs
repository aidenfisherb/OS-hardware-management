using OpenCvSharp;

namespace PipeDodger.Game;

/// <summary>
/// The game's art: a background and scrolling pipes drawn with OpenCV shapes, plus the character image.
/// Collision, scoring, and character movement are not here yet; set CharacterY to move the character.
/// </summary>
public sealed class GameScene : IDisposable
{
    public const int Width = 640;
    public const int Height = 480;
    public const int GroundY = 420;
    public const int CharacterX = 150;
    public const int CharacterRadius = 18;

    private const int PipeWidth = 70;
    private const int PipeCapOverhang = 6;
    private const int PipeCapHeight = 24;
    private const int PipeGapHeight = 150;
    private const int PipeSpacing = 230;
    private const int PipeCount = 4;
    private const int ScrollPixelsPerSecond = 120;
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
    }

    private readonly Random _random = new();
    private readonly List<Pipe> _pipes = new();
    private readonly Mat _background;
    private readonly Mat _sprite;
    private readonly Mat _spriteMask;
    private readonly Mat _canvas = new();
    private double _scrollOffset;

    public GameScene()
    {
        (_sprite, _spriteMask) = LoadCharacterSprite();
        _background = BuildBackground();
        for (int i = 0; i < PipeCount; i++)
        {
            _pipes.Add(new Pipe
            {
                X = 400 + i * PipeSpacing,
                GapCenterY = RandomGapCenter(),
                Colors = i % 2 == 0 ? RedPipe : BluePipe,
            });
        }
    }

    public double CharacterY { get; set; } = GroundY / 2.0;

    // ---------- Update ----------

    /// <summary>Scrolls the pipes and ground. A pipe that leaves on the left returns on the right with a new gap.</summary>
    public void Update(double deltaSeconds)
    {
        double distance = ScrollPixelsPerSecond * deltaSeconds;
        _scrollOffset += distance;

        foreach (var pipe in _pipes)
        {
            pipe.X -= distance;
            if (pipe.X + PipeWidth + PipeCapOverhang < 0)
            {
                pipe.X += PipeCount * PipeSpacing;
                pipe.GapCenterY = RandomGapCenter();
            }
        }
    }

    private int RandomGapCenter()
    {
        const int margin = 40;
        return _random.Next(margin + PipeGapHeight / 2, GroundY - margin - PipeGapHeight / 2);
    }

    // ---------- Render ----------

    /// <summary>
    /// Draws one game frame into a buffer this class reuses every call.
    /// The returned Mat is overwritten by the next render, so call Clone() to keep a copy.
    /// </summary>
    public Mat Render()
    {
        // the background never changes, so copy it instead of redrawing the gradient every frame
        _background.CopyTo(_canvas);

        foreach (var pipe in _pipes)
            DrawPipe(_canvas, (int)pipe.X, pipe.GapCenterY, pipe.Colors);

        // drawn after the pipes so they look planted in the ground
        DrawGround(_canvas, (int)_scrollOffset);
        DrawCharacter((int)CharacterY);
        return _canvas;
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

    private static void DrawPipe(Mat image, int x, int gapCenterY, PipeColors colors)
    {
        int gapTop = gapCenterY - PipeGapHeight / 2;
        int gapBottom = gapCenterY + PipeGapHeight / 2;

        DrawPipeBody(image, new Rect(x, 0, PipeWidth, gapTop), colors);
        DrawPipeBody(image, new Rect(x - PipeCapOverhang, gapTop - PipeCapHeight, PipeWidth + PipeCapOverhang * 2, PipeCapHeight), colors);

        DrawPipeBody(image, new Rect(x, gapBottom, PipeWidth, GroundY - gapBottom), colors);
        DrawPipeBody(image, new Rect(x - PipeCapOverhang, gapBottom, PipeWidth + PipeCapOverhang * 2, PipeCapHeight), colors);
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
