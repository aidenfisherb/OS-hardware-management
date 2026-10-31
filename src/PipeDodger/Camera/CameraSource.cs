using OpenCvSharp;

namespace PipeDodger.Camera;

/// <summary>
/// The app's only connection to the webcam. Opens the device, hands out one frame per loop,
/// detects a lost camera, and releases the device on Dispose.
/// </summary>
public sealed class CameraSource : IDisposable
{
    // About 2 seconds at 30 fps: long enough to ride out a hiccup, short enough to notice a dead camera.
    private const int MaxConsecutiveFailures = 60;

    private readonly CameraOptions _options;
    private readonly VideoCapture _capture = new();
    private readonly Mat _frame = new();
    private int _consecutiveFailures;
    private bool _disposed;

    // constructor receives the cameraoptions object and stores it in _options
    public CameraSource(CameraOptions options) => _options = options;

    public string BackendName { get; private set; } = "";
    public int Width { get; private set; }
    public int Height { get; private set; }
    public long FramesRead { get; private set; }
    public bool IsLost => _consecutiveFailures >= MaxConsecutiveFailures;

    // ---------- Open ----------

    public void Open()
    {
        //opens camera
        bool opened = _capture.Open(_options.DeviceIndex, _options.Backend);
        if (!opened || !_capture.IsOpened())
        {
            throw new CameraException(
                $"Could not open camera {_options.DeviceIndex} using {_options.Backend}.",
                OpenFailureHint());
        }

        // the driver may pick the nearest size it supports
        _capture.Set(VideoCaptureProperties.FrameWidth, _options.RequestedWidth);
        _capture.Set(VideoCaptureProperties.FrameHeight, _options.RequestedHeight);

        BackendName = _capture.GetBackendName();
        // read back the actual size the driver chose
        Width = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
        Height = (int)_capture.Get(VideoCaptureProperties.FrameHeight);
    }

    // ---------- Read ----------

    /// <summary>
    /// Reads the next frame into a buffer this class reuses every call.
    /// The returned Mat is overwritten by the next read, so call Clone() to keep a copy.
    /// </summary>
    public bool TryReadFrame(out Mat frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        //newest picture replaces old one
        frame = _frame;

        bool simulatedFailure = _options.SimulateReadFailureAfter > 0
                                && FramesRead >= _options.SimulateReadFailureAfter;

        // waits for the camera and writes the next frame into the buffer
        if (simulatedFailure || !_capture.Read(_frame) || _frame.Empty())
        {
            _consecutiveFailures++;
            return false;
        }

        _consecutiveFailures = 0;
        FramesRead++;
        return true;
    }

    public CameraException LostException() => new(
        $"Camera stopped delivering frames ({MaxConsecutiveFailures} failed reads in a row).",
        "Check that the camera is still connected and that no other app took it over, then restart.");

    // ---------- Cleanup ----------

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        //disconnect the camera
        _capture.Release();
        // free the C++ VideoCapture object
        _capture.Dispose();
        // free the .9 MB of pixel memory
        _frame.Dispose();
    }

    // ---------- Helpers ----------

    private string OpenFailureHint()
    {
        var hints = new List<string>
        {
            "Close other apps that may be using the camera (Teams, Zoom, browser tabs).",
            "Try another index with --camera 1, or run --list-cameras.",
        };

        if (OperatingSystem.IsWindows())
        {
            hints.Add("Check Settings > Privacy & security > Camera: allow camera access and desktop apps.");
            hints.Add("Try the other Windows backend: --backend dshow.");
        }
        else if (OperatingSystem.IsMacOS())
        {
            hints.Add("Check System Settings > Privacy & Security > Camera: allow your terminal or VS Code.");
        }

        return string.Join(Environment.NewLine, hints.Select(h => "  - " + h));
    }

    /// <summary>Tries to open indexes 0..maxIndex and reports which work. Used for device selection.</summary>
    public static IEnumerable<(int Index, bool Works, string Details)> Probe(VideoCaptureAPIs backend, int maxIndex = 4)
    {
        for (int index = 0; index <= maxIndex; index++)
        {
            using var capture = new VideoCapture();
            if (!capture.Open(index, backend) || !capture.IsOpened())
            {
                yield return (index, false, "not available");
                continue;
            }

            using var frame = new Mat();
            bool gotFrame = capture.Read(frame) && !frame.Empty();
            string details = gotFrame
                ? $"{frame.Width}x{frame.Height}, {frame.Channels()} channels via {capture.GetBackendName()}"
                : "opened but returned no frame";
            capture.Release();
            yield return (index, gotFrame, details);
        }
    }
}
