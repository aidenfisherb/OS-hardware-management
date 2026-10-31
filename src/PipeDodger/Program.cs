using System.Diagnostics;
using OpenCvSharp;
using PipeDodger.Camera;
using PipeDodger.Game;
using PipeDodger.Input;

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
            //open webcam
            camera.Open();
            Console.WriteLine($"Camera {options.DeviceIndex} opened via {camera.BackendName} at {camera.Width}x{camera.Height}.");
            Console.WriteLine("Blink or Space = flap | R = restart | P = pause | C = capture still | Q / Esc = quit");

            Cv2.NamedWindow(LiveWindow, WindowFlags.AutoSize);
            //hand the camera to the frame loop
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
        using var blink = new BlinkDetector();
        var clock = Stopwatch.StartNew();
        bool userPaused = false;
        Mat? captured = null;

        try
        {
            // is Ctrl+C pressed?
            while (!stopRequested())
            {
                //get the next frame
                if (camera.TryReadFrame(out Mat frame))
                {
                    Cv2.CvtColor(frame, liveGray, ColorConversionCodes.BGR2GRAY);
                    //show it
                    Cv2.ImShow(LiveWindow, frame);
                    Cv2.ImShow("Live Grayscale", liveGray);

                    // real elapsed time keeps the game speed the same at any camera frame rate
                    double deltaSeconds = clock.Elapsed.TotalSeconds;
                    clock.Restart();

                    bool blinked = blink.ProcessFrame(liveGray, deltaSeconds);

                    // losing the player mid-game pauses it instead of letting the bird fall
                    bool faceLost = scene.State == GameState.Playing && !blink.HasFace;
                    bool paused = userPaused || faceLost;

                    if (blinked && !paused)
                        scene.Flap();
                    if (!paused)
                        scene.Update(deltaSeconds);

                    Cv2.ImShow("Game", scene.Render(BannerText(scene, userPaused, faceLost), StatusText(blink)));

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
                    else if (key == ' ')
                    {
                        // keyboard backup for testing, or if blink detection struggles in a room's lighting
                        if (!paused)
                            scene.Flap();
                    }
                    else if (key is 'r' or 'R')
                    {
                        scene.Restart();
                        userPaused = false;
                    }
                    else if (key is 'p' or 'P')
                    {
                        userPaused = !userPaused;
                    }
                    else if (QuitRequested(key))
                    {
                        return;
                    }
                }
                //if there are 60 failures in a row, throw a exception
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

    private static string? BannerText(GameScene scene, bool userPaused, bool faceLost)
    {
        if (userPaused) return "Paused - press P";
        if (faceLost) return "Face lost - look at the camera";
        return scene.State switch
        {
            GameState.Ready => "Blink to start",
            GameState.GameOver => "Game over - press R",
            _ => null,
        };
    }

    // shows what the detector sees, which makes lighting or glasses problems easy to spot
    private static string StatusText(BlinkDetector blink) =>
        blink.HasFace ? $"Face found | eyes open: {blink.EyesVisible}" : "No face found";

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
