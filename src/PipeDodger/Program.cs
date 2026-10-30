using System.Diagnostics;
using OpenCvSharp;
using PipeDodger.Camera;
using PipeDodger.Game;

namespace PipeDodger;

public static class Program
{
    private const string LiveWindow = "Live Original";
    private const int EscKey = 27;

    public static int Main(string[] args)
    {
        CameraOptions options;
        try
        {
            options = CameraOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(CameraOptions.Usage);
            return 2;
        }

        if (options.ListCameras)
        {
            ListCameras(options);
            return 0;
        }

        // Ctrl+C in the terminal should still go through normal cleanup instead of killing the process.
        bool stopRequested = false;
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopRequested = true; };

        try
        {
            using var camera = new CameraSource(options);
            camera.Open();
            Console.WriteLine($"Camera {options.DeviceIndex} opened via {camera.BackendName} at {camera.Width}x{camera.Height}.");
            Console.WriteLine("C = capture still | Q / Esc = quit");

            Cv2.NamedWindow(LiveWindow, WindowFlags.AutoSize);
            RunLoop(camera, () => stopRequested);

            Console.WriteLine($"Stopped after {camera.FramesRead} frames. Camera released.");
            return 0;
        }
        catch (CameraException ex)
        {
            Console.Error.WriteLine($"Camera error: {ex.Message}");
            Console.Error.WriteLine(ex.Hint);
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"{ex.Message} Expected at: {ex.FileName}");
            return 3;
        }
        finally
        {
            Cv2.DestroyAllWindows();
        }
    }

    // ---------- Main loop ----------

    private static void RunLoop(CameraSource camera, Func<bool> stopRequested)
    {
        using var liveGray = new Mat();
        using var capturedGray = new Mat();
        using var scene = new GameScene();
        var clock = Stopwatch.StartNew();
        Mat? captured = null;

        try
        {
            while (!stopRequested())
            {
                if (camera.TryReadFrame(out Mat frame))
                {
                    Cv2.CvtColor(frame, liveGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.ImShow(LiveWindow, frame);
                    Cv2.ImShow("Live Grayscale", liveGray);

                    // real elapsed time keeps the scroll speed the same at any camera frame rate
                    scene.Update(clock.Elapsed.TotalSeconds);
                    clock.Restart();
                    Cv2.ImShow("Game", scene.Render());

                    if (captured is not null)
                    {
                        Cv2.ImShow("Captured Original", captured);
                        Cv2.ImShow("Captured Grayscale", capturedGray);
                    }

                    int key = Cv2.WaitKey(1);
                    if (key is 'c' or 'C')
                    {
                        // Clone: CameraSource reuses its Mat; a reference would keep updating with live video.
                        captured?.Dispose();
                        captured = frame.Clone();
                        Cv2.CvtColor(captured, capturedGray, ColorConversionCodes.BGR2GRAY);
                    }
                    else if (QuitRequested(key))
                    {
                        return;
                    }
                }
                else if (camera.IsLost)
                {
                    throw camera.LostException();
                }
                else if (QuitRequested(Cv2.WaitKey(1)))
                {
                    return;
                }
            }
        }
        finally
        {
            captured?.Dispose();
        }
    }

    private static bool QuitRequested(int key) =>
        key is 'q' or 'Q' or EscKey
        || Cv2.GetWindowProperty(LiveWindow, WindowPropertyFlags.Visible) < 1;

    // ---------- Device selection ----------

    private static void ListCameras(CameraOptions options)
    {
        Console.WriteLine($"Probing cameras with backend {options.Backend}...");
        foreach (var (index, works, details) in CameraSource.Probe(options.Backend))
            Console.WriteLine($"  [{index}] {(works ? "OK  " : "--  ")} {details}");
    }
}
