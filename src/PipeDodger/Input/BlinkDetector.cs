using OpenCvSharp;

namespace PipeDodger.Input;

/// <summary>
/// Turns grayscale camera frames into blink events. The eye detector finds open eyes and usually
/// misses closed ones, so a blink is eyes that were being found and then suddenly are not.
/// </summary>
public sealed class BlinkDetector : IDisposable
{
    // Face search runs on a small copy of the frame: about 10x faster, and a face is easy to find at this size.
    private const int FaceSearchWidth = 320;
    private const int MinFaceSizeInSearch = 50;

    // A head barely moves between frames, so the face is re-found every few frames. Eyes are checked every frame.
    private const int FramesBetweenFaceSearches = 3;

    // A large face is shrunk to this width before the eye search, which keeps eyes big enough to find but is much faster.
    private const int EyeSearchFaceWidth = 120;

    // Short detector dropouts should not pause the game or lose the player.
    private const double FaceGraceSeconds = 0.5;

    // Eyes must be seen this many frames in a row before a blink can count, which ignores detector flicker.
    private const int OpenFramesToArm = 2;

    // A real blink lasts about 3-12 frames at 30 fps. Raise this if the bird flaps by itself.
    private const int ClosedFramesToBlink = 2;

    private readonly CascadeClassifier _faceCascade;
    private readonly CascadeClassifier _eyeCascade;
    private readonly Mat _small = new();
    private readonly Mat _smallEyes = new();
    private Rect _lastFace;
    private double _secondsSinceFace = double.MaxValue;
    private int _framesUntilFaceSearch;
    private int _openFrames;
    private int _closedFrames;
    private bool _armed;

    public BlinkDetector()
    {
        _faceCascade = LoadCascade("haarcascade_frontalface_default.xml");
        _eyeCascade = LoadCascade("haarcascade_eye_tree_eyeglasses.xml");
    }

    public bool HasFace => _secondsSinceFace < FaceGraceSeconds;
    public int EyesVisible { get; private set; }

    // ---------- Frame processing ----------

    /// <summary>Looks for a face and eyes in one grayscale frame. Returns true on the frame a blink is recognized.</summary>
    public bool ProcessFrame(Mat gray, double deltaSeconds)
    {
        bool searchForFace = !HasFace || --_framesUntilFaceSearch <= 0;
        if (searchForFace && TryFindFace(gray, out Rect face))
        {
            _lastFace = face;
            _secondsSinceFace = 0;
            _framesUntilFaceSearch = FramesBetweenFaceSearches;
        }
        else if (HasFace)
        {
            _secondsSinceFace += deltaSeconds;
        }

        if (!HasFace)
        {
            ResetEyes();
            return false;
        }

        // During a dropout, keep checking where the face just was so a blink in that moment still counts.
        EyesVisible = CountEyes(gray, _lastFace);
        return ObserveEyes(EyesVisible);
    }

    /// <summary>The blink rule on its own: open for a moment, then closed for a moment. Holding eyes shut flaps only once.</summary>
    public bool ObserveEyes(int eyesVisible)
    {
        if (eyesVisible > 0)
        {
            _closedFrames = 0;
            _openFrames++;
            if (_openFrames >= OpenFramesToArm)
                _armed = true;
            return false;
        }

        _openFrames = 0;
        _closedFrames++;
        if (_armed && _closedFrames >= ClosedFramesToBlink)
        {
            _armed = false;
            return true;
        }
        return false;
    }

    private void ResetEyes()
    {
        EyesVisible = 0;
        _openFrames = 0;
        _closedFrames = 0;
        _armed = false;
    }

    // ---------- Detection ----------

    private bool TryFindFace(Mat gray, out Rect face)
    {
        double scale = (double)FaceSearchWidth / gray.Width;
        Cv2.Resize(gray, _small, new Size(), scale, scale, InterpolationFlags.Area);

        Rect[] found = _faceCascade.DetectMultiScale(_small, 1.1, 5, HaarDetectionTypes.ScaleImage,
            new Size(MinFaceSizeInSearch, MinFaceSizeInSearch));
        if (found.Length == 0)
        {
            face = default;
            return false;
        }

        // the largest face is the player; convert it back to full-frame coordinates
        Rect largest = found.MaxBy(f => f.Width * f.Height);
        var scaled = new Rect((int)(largest.X / scale), (int)(largest.Y / scale), (int)(largest.Width / scale), (int)(largest.Height / scale));
        face = scaled & new Rect(0, 0, gray.Width, gray.Height);
        return face.Width > 0 && face.Height > 0;
    }

    private int CountEyes(Mat gray, Rect face)
    {
        // Eyes sit in this band of the face. Searching only here is faster and avoids false hits on the mouth and nose.
        var eyeBand = new Rect(face.X, face.Y + face.Height / 5, face.Width, face.Height * 2 / 5);
        using var region = new Mat(gray, eyeBand);

        double scale = Math.Min(1.0, (double)EyeSearchFaceWidth / face.Width);
        Cv2.Resize(region, _smallEyes, new Size(), scale, scale, InterpolationFlags.Area);

        int faceWidth = (int)(face.Width * scale);
        var minEye = new Size(faceWidth / 6, faceWidth / 6);
        var maxEye = new Size(faceWidth * 2 / 5, faceWidth * 2 / 5);
        return _eyeCascade.DetectMultiScale(_smallEyes, 1.1, 3, HaarDetectionTypes.ScaleImage, minEye, maxEye).Length;
    }

    private static CascadeClassifier LoadCascade(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "assets", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Could not load a detector file.", path);

        var cascade = new CascadeClassifier(path);
        if (cascade.Empty())
        {
            cascade.Dispose();
            throw new FileNotFoundException("A detector file is not a valid cascade.", path);
        }
        return cascade;
    }

    // ---------- Cleanup ----------

    public void Dispose()
    {
        _faceCascade.Dispose();
        _eyeCascade.Dispose();
        _small.Dispose();
        _smallEyes.Dispose();
    }
}
