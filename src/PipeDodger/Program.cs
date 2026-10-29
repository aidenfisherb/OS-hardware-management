using OpenCvSharp;
using PipeDodger.Camera;

namespace PipeDodger;

public static class Program
{
    private const string WindowName = "Pipe Dodger";
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
            Console.WriteLine("Press Q or Esc in the window to quit.");

            Cv2.NamedWindow(WindowName, WindowFlags.AutoSize);
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
        finally
        {
            Cv2.DestroyAllWindows();
        }
    }

    // ---------- Main loop ----------

    private static void RunLoop(CameraSource camera, Func<bool> stopRequested)
    {
        while (!stopRequested())
        {
            if (camera.TryReadFrame(out Mat frame))
            {
                // Temporary display. Ethan's comparison views and Jake's game drawing replace this.
                Cv2.ImShow(WindowName, frame);
            }
            else if (camera.IsLost)
            {
                throw camera.LostException();
            }

            // WaitKey also pumps window events; without it the window freezes.
            int key = Cv2.WaitKey(1);
            if (key == 'q' || key == 'Q' || key == EscKey || WindowClosed())
                return;
        }
    }

    private static bool WindowClosed() =>
        Cv2.GetWindowProperty(WindowName, WindowPropertyFlags.Visible) < 1;

    // ---------- Device selection ----------

    private static void ListCameras(CameraOptions options)
    {
        Console.WriteLine($"Probing cameras with backend {options.Backend}...");
        foreach (var (index, works, details) in CameraSource.Probe(options.Backend))
            Console.WriteLine($"  [{index}] {(works ? "OK  " : "--  ")} {details}");
    }
}
