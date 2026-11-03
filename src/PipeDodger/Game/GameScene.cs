using OpenCvSharp;

namespace PipeDodger.Game;

public enum GameState { Ready, Playing, GameOver }

/// <summary>Bird physics, pipes, collision, and score. Camera input only needs to call Flap().</summary>
public sealed class GameScene : IDisposable
{
    // Keep the artwork at its native resolution: no stretched scenery or smoothed pixels.
    public const int Width = 288;
    public const int Height = 512;
    public const int GroundY = 404;
    public const int CharacterX = 58;
    public const int CharacterRadius = 10;

    // Retain the forgiving blink timing, scaled to the smaller bird and portrait playfield.
    private const double GravityPixelsPerSecond2 = 300;
    private const double FlapSpeedPixelsPerSecond = 173;
    private const double MaxFallSpeedPixelsPerSecond = 253;
    private const double MaxStepSeconds = 0.1;
    private const int PipeWidth = 52;
    private const int PipeCapHeight = 24;
    private const int PipeGapHeight = 120;
    private const int PipeGapMargin = 36;
    private const int MaxGapChange = 60;
    private const int PipeSpacing = 170;
    private const int PipeCount = 3;
    private const int FirstPipeX = 320;
    private const int ScrollPixelsPerSecond = 60;

    private sealed class Pipe
    {
        public double X;
        public int GapCenterY;
        public bool Scored;
    }

    private readonly Random _random = new();
    private readonly List<Pipe> _pipes = new();
    private readonly List<Sprite> _sprites = new();
    private readonly Sprite _background;
    private readonly Sprite _ground;
    private readonly Sprite _pipe;
    private readonly Sprite _upperPipe;
    private readonly Sprite _message;
    private readonly Sprite _gameOver;
    private readonly Sprite[] _bird;
    private readonly Sprite[] _digits;
    private readonly Mat _canvas = new();
    private readonly Mat _rotatedBird = new();
    private readonly Mat _rotatedMask = new();
    private double _scrollOffset;
    private double _velocityY;
    private double _animationSeconds;

    public GameScene()
    {
        try
        {
            _background = Load("background-day.png");
            _ground = Load("base.png");
            _pipe = Load("pipe-green.png");
            _upperPipe = Load("pipe-green.png", flip: true);
            _message = Load("message.png");
            _gameOver = Load("gameover.png");
            _bird = new[] { Load("yellowbird-upflap.png"), Load("yellowbird-midflap.png"), Load("yellowbird-downflap.png") };
            _digits = Enumerable.Range(0, 10).Select(i => Load($"{i}.png")).ToArray();
            Restart();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public double CharacterY { get; private set; }

    public void Restart()
    {
        State = GameState.Ready;
        Score = 0;
        CharacterY = Height / 2.0;
        _velocityY = 0;
        _animationSeconds = 0;
        _scrollOffset = 0;

        _pipes.Clear();
        int gapCenter = Height / 2;
        for (int i = 0; i < PipeCount; i++)
        {
            gapCenter = RandomGapCenter(gapCenter);
            _pipes.Add(new Pipe { X = FirstPipeX + i * PipeSpacing, GapCenterY = gapCenter });
        }
    }

    /// <summary>The first flap starts the game; later flaps reset upward speed.</summary>
    public void Flap()
    {
        if (State == GameState.GameOver)
            return;

        State = GameState.Playing;
        _velocityY = -FlapSpeedPixelsPerSecond;
    }

    public void Update(double deltaSeconds)
    {
        double step = Math.Min(deltaSeconds, MaxStepSeconds);
        if (State == GameState.GameOver)
            return;

        _scrollOffset += ScrollPixelsPerSecond * step;
        _animationSeconds += step;
        if (State == GameState.Ready)
        {
            CharacterY = Height / 2.0 + Math.Sin(_animationSeconds * 4) * 4;
            return;
        }

        _velocityY = Math.Min(_velocityY + GravityPixelsPerSecond2 * step, MaxFallSpeedPixelsPerSecond);
        CharacterY += _velocityY * step;
        if (CharacterY < CharacterRadius)
        {
            CharacterY = CharacterRadius;
            _velocityY = 0;
        }

        MovePipes(ScrollPixelsPerSecond * step);
        foreach (var pipe in _pipes)
        {
            if (!pipe.Scored && pipe.X + PipeWidth < CharacterX)
            {
                pipe.Scored = true;
                Score++;
            }
        }
        if (CharacterHitSomething())
            State = GameState.GameOver;
    }

    private void MovePipes(double distance)
    {
        for (int i = 0; i < _pipes.Count; i++)
        {
            Pipe pipe = _pipes[i];
            pipe.X -= distance;
            if (pipe.X + PipeWidth < 0)
            {
                pipe.X += PipeCount * PipeSpacing;
                pipe.Scored = false;
                pipe.GapCenterY = RandomGapCenter(_pipes[(i + PipeCount - 1) % PipeCount].GapCenterY);
            }
        }
    }

    private int RandomGapCenter(int previousCenter)
    {
        int min = Math.Max(PipeGapMargin + PipeGapHeight / 2, previousCenter - MaxGapChange);
        int max = Math.Min(GroundY - PipeGapMargin - PipeGapHeight / 2, previousCenter + MaxGapChange);
        return _random.Next(min, max + 1);
    }

    private bool CharacterHitSomething()
    {
        if (CharacterY + CharacterRadius >= GroundY)
        {
            CharacterY = GroundY - CharacterRadius;
            return true;
        }

        foreach (var pipe in _pipes)
        {
            int x = (int)pipe.X;
            int top = pipe.GapCenterY - PipeGapHeight / 2;
            int bottom = pipe.GapCenterY + PipeGapHeight / 2;
            // The pipe lips are wider than the stems; match the actual sprite silhouette.
            foreach (Rect part in new[]
            {
                new Rect(x + 2, 0, PipeWidth - 4, top - PipeCapHeight),
                new Rect(x, top - PipeCapHeight, PipeWidth, PipeCapHeight),
                new Rect(x, bottom, PipeWidth, PipeCapHeight),
                new Rect(x + 2, bottom + PipeCapHeight, PipeWidth - 4, GroundY - bottom - PipeCapHeight),
            })
            {
                double dx = CharacterX - Math.Clamp(CharacterX, part.Left, part.Right);
                double dy = CharacterY - Math.Clamp(CharacterY, part.Top, part.Bottom);
                if (dx * dx + dy * dy <= CharacterRadius * CharacterRadius)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Returns a reused image buffer, overwritten by the next render.</summary>
    public Mat Render()
    {
        _background.Image.CopyTo(_canvas);
        if (State != GameState.Ready)
        {
            foreach (var pipe in _pipes)
            {
                _upperPipe.Draw(_canvas, (int)pipe.X, pipe.GapCenterY - PipeGapHeight / 2 - _upperPipe.Image.Height);
                _pipe.Draw(_canvas, (int)pipe.X, pipe.GapCenterY + PipeGapHeight / 2);
            }
        }

        // Tile the original base without stretching it. It also hides the pipe ends.
        int offset = (int)_scrollOffset % _ground.Image.Width;
        for (int x = -offset; x < Width; x += _ground.Image.Width)
            _ground.Draw(_canvas, x, GroundY);

        DrawCharacter();
        if (State == GameState.Ready)
        {
            _message.Draw(_canvas, (Width - _message.Image.Width) / 2, 61);
            DrawCenteredText("Press SPACE to start", 61 + _message.Image.Height + 30);
        }
        else
            DrawScore();
        if (State == GameState.GameOver)
        {
            _gameOver.Draw(_canvas, (Width - _gameOver.Image.Width) / 2, 170);
            DrawCenteredText("Press R to restart", 170 + _gameOver.Image.Height + 30);
        }
        return _canvas;
    }

    // The sprite set has no letters, so this uses OpenCV text. Dark copies shifted around it act as an
    // outline; a thicker copy wouldn't line up, because in OpenCV 5 thickness also changes letter widths.
    private void DrawCenteredText(string text, int baselineY)
    {
        const double scale = 0.6;
        const int thickness = 2;
        Size size = Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, scale, thickness, out _);
        int x = (Width - size.Width) / 2;
        foreach (var (dx, dy) in new[] { (-2, 0), (2, 0), (0, -2), (0, 2) })
            Cv2.PutText(_canvas, text, new Point(x + dx, baselineY + dy), HersheyFonts.HersheySimplex, scale, new Scalar(40, 40, 40), thickness, LineTypes.AntiAlias);
        Cv2.PutText(_canvas, text, new Point(x, baselineY), HersheyFonts.HersheySimplex, scale, Scalar.White, thickness, LineTypes.AntiAlias);
    }

    private void DrawScore()
    {
        Sprite[] digits = Score.ToString().Select(c => _digits[c - '0']).ToArray();
        int x = (Width - digits.Sum(d => d.Image.Width)) / 2;
        foreach (Sprite digit in digits)
        {
            digit.Draw(_canvas, x, 51);
            x += digit.Image.Width;
        }
    }

    private void DrawCharacter()
    {
        int wing = (int)(_animationSeconds / 0.1) % 4;
        Sprite bird = _bird[wing == 3 ? 1 : wing];
        if (State == GameState.Ready)
        {
            bird.Draw(_canvas, CharacterX - bird.Image.Width / 2, (int)CharacterY - bird.Image.Height / 2);
            return;
        }

        // A fixed transparent square leaves room for the bird to tilt without clipping its wings.
        const int size = 48;
        double angle = Math.Clamp(-_velocityY * 0.2, -90, 25);
        using var rotation = Cv2.GetRotationMatrix2D(new Point2f(bird.Image.Width / 2f, bird.Image.Height / 2f), angle, 1);
        rotation.Set(0, 2, rotation.At<double>(0, 2) + (size - bird.Image.Width) / 2.0);
        rotation.Set(1, 2, rotation.At<double>(1, 2) + (size - bird.Image.Height) / 2.0);
        Cv2.WarpAffine(bird.Image, _rotatedBird, rotation, new Size(size, size), InterpolationFlags.Nearest);
        Cv2.WarpAffine(bird.Mask, _rotatedMask, rotation, new Size(size, size), InterpolationFlags.Nearest);
        Blit(_canvas, _rotatedBird, _rotatedMask, CharacterX - size / 2, (int)CharacterY - size / 2);
    }

    private Sprite Load(string name, bool flip = false)
    {
        var sprite = new Sprite(name, flip);
        _sprites.Add(sprite);
        return sprite;
    }

    // One clipped copy handles alpha sprites, offscreen pipes, and the scrolling base.
    private static void Blit(Mat canvas, Mat image, Mat mask, int x, int y)
    {
        Rect visible = new Rect(x, y, image.Width, image.Height) & new Rect(0, 0, canvas.Width, canvas.Height);
        if (visible.Width <= 0 || visible.Height <= 0)
            return;

        var sourceArea = new Rect(visible.X - x, visible.Y - y, visible.Width, visible.Height);
        using var target = new Mat(canvas, visible);
        using var source = new Mat(image, sourceArea);
        using var alpha = new Mat(mask, sourceArea);
        source.CopyTo(target, alpha);
    }

    private sealed class Sprite : IDisposable
    {
        public Mat Image { get; } = new();
        public Mat Mask { get; } = new();

        public Sprite(string name, bool flip)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "assets", "flappy-bird", name);
            using var source = Cv2.ImRead(path, ImreadModes.Unchanged);
            if (source.Empty())
                throw new FileNotFoundException("Could not load a game sprite.", path);

            if (flip)
                Cv2.Flip(source, source, FlipMode.X);
            if (source.Channels() == 4)
            {
                Cv2.CvtColor(source, Image, ColorConversionCodes.BGRA2BGR);
                Cv2.ExtractChannel(source, Mask, 3);
            }
            else
            {
                source.CopyTo(Image);
                Mask.Create(source.Rows, source.Cols, MatType.CV_8UC1);
                Mask.SetTo(Scalar.All(255));
            }
        }

        public void Draw(Mat canvas, int x, int y) => Blit(canvas, Image, Mask, x, y);

        public void Dispose()
        {
            Image.Dispose();
            Mask.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (Sprite sprite in _sprites)
            sprite.Dispose();
        _canvas.Dispose();
        _rotatedBird.Dispose();
        _rotatedMask.Dispose();
    }
}
