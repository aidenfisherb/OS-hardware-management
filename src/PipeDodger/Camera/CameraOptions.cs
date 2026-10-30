using OpenCvSharp;

namespace PipeDodger.Camera;

// figure out what camera to open and how
public sealed record CameraOptions
{
    public int DeviceIndex { get; init; } = 0;
    public VideoCaptureAPIs Backend { get; init; } = DefaultBackend();

    // 640x480 works on nearly every webcam, can change this default easily
    public int RequestedWidth { get; init; } = 640;
    public int RequestedHeight { get; init; } = 480;
    public bool ListCameras { get; init; }

    /// <summary>Test hook: pretend frame reads fail after this many frames (0 = off).</summary>
    public int SimulateReadFailureAfter { get; init; }

    // name each OS's native media API explicitly so we can decide which one to use
    public static VideoCaptureAPIs DefaultBackend() =>
        OperatingSystem.IsWindows() ? VideoCaptureAPIs.MSMF :
        OperatingSystem.IsMacOS() ? VideoCaptureAPIs.AVFOUNDATION :
        VideoCaptureAPIs.ANY;


    // troubleshooting method for parsing command-line arguments into CameraOptions
    public static CameraOptions Parse(string[] args)
    {
        var options = new CameraOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--camera":
                    options = options with { DeviceIndex = ParseInt(args, ref i) };
                    break;
                case "--backend":
                    options = options with { Backend = ParseBackend(NextValue(args, ref i)) };
                    break;
                case "--size":
                    var parts = NextValue(args, ref i).Split('x');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int w) || !int.TryParse(parts[1], out int h))
                        throw new ArgumentException("--size must look like 640x480");
                    options = options with { RequestedWidth = w, RequestedHeight = h };
                    break;
                case "--list-cameras":
                    options = options with { ListCameras = true };
                    break;
                case "--simulate-read-failure":
                    options = options with { SimulateReadFailureAfter = ParseInt(args, ref i) };
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        return options;
    }

    public const string Usage =
        "Options:\n" +
        "  --camera <index>               Camera to open (default 0)\n" +
        "  --backend <msmf|dshow|avfoundation|any>\n" +
        "  --size <width>x<height>        Requested frame size (default 640x480)\n" +
        "  --list-cameras                 Probe indexes 0-4 and exit\n" +
        "  --simulate-read-failure <n>    Fail frame reads after n frames (testing)";

    private static string NextValue(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"{args[i]} needs a value.");
        return args[++i];
    }

    private static int ParseInt(string[] args, ref int i)
    {
        string flag = args[i];
        return int.TryParse(NextValue(args, ref i), out int value) && value >= 0
            ? value
            : throw new ArgumentException($"{flag} needs a non-negative number.");
    }

    private static VideoCaptureAPIs ParseBackend(string name) => name.ToLowerInvariant() switch
    {
        "msmf" => VideoCaptureAPIs.MSMF,
        "dshow" => VideoCaptureAPIs.DSHOW,
        "avfoundation" => VideoCaptureAPIs.AVFOUNDATION,
        "any" => VideoCaptureAPIs.ANY,
        _ => throw new ArgumentException($"Unknown backend '{name}'."),
    };
}
