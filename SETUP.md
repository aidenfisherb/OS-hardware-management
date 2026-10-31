# Setup and Run

## Requirements

| Item | Version |
| --- | --- |
| .NET SDK | 10.0 (verified with 10.0.400) |
| OpenCvSharp5 | 5.0.0.20261018 |
| Native runtime | Picked automatically for your OS/CPU (see below) |
| Editor | VS Code + C# Dev Kit (optional; the terminal works too) |

Check your SDK with `dotnet --version`.

## Native runtime selection

`PipeDodger.csproj` adds exactly one native OpenCV package based on the machine you build on:

| Machine | Package |
| --- | --- |
| Windows x64 | `OpenCvSharp5.runtime.win` |
| Windows ARM64 | `OpenCvSharp5.runtime.win-arm64` |
| Mac (Intel) | `OpenCvSharp5.runtime.osx.x64` |
| Mac (Apple Silicon) | `OpenCvSharp5.runtime.osx.arm64` |

Don't add these by hand. If a Mac with Apple Silicon has the x64 .NET SDK installed, the build picks the wrong runtime. Install the Arm64 SDK instead.

## Build and run

```
cd src/PipeDodger
dotnet run
```

The first run downloads packages from NuGet, so it needs internet access.

### Options

| Flag | Purpose |
| --- | --- |
| `--list-cameras` | Try camera indexes 0-4 and print which ones work |
| `--camera <n>` | Open camera `n` (default `0`) |
| `--backend <msmf\|dshow\|avfoundation\|any>` | Force an OS camera API (default: `msmf` on Windows, `avfoundation` on Mac) |
| `--size <w>x<h>` | Requested frame size (default `640x480`; the driver may choose the nearest size) |
| `--simulate-read-failure <n>` | Test hook: fail every read after `n` frames |

### Controls

| Key | Action |
| --- | --- |
| Blink / Space | Flap (the first flap starts the game) |
| R | Restart the game |
| P | Pause / resume the game |
| C | Capture a still (original + grayscale); stays frozen until the next C |
| Q / Esc / close Live Original / Ctrl+C | Quit and release the camera |

An OpenCV window must have keyboard focus for keys to work. The game also pauses by itself if your face is lost for more than half a second.

Five windows: **Live Original**, **Live Grayscale**, **Captured Original**, **Captured Grayscale** (captured pair appears after the first C), and **Game**. Details: [docs/ethan-image-views.md](docs/ethan-image-views.md).

### Blink detection tips

The bottom of the Game window shows what the detector sees. With your eyes open it should say `eyes open: 1` or `2`, and it should drop to `0` when you blink.

- Face the camera with light on your face, not behind you.
- Blink firmly. A very fast blink can fall between camera frames.
- If the bird flaps by itself, raise `ClosedFramesToBlink` in `Input/BlinkDetector.cs`. Space always works as a backup.

Detector files in `assets/` come from [OpenCV](https://github.com/opencv/opencv/tree/4.x/data/haarcascades); their license text is kept at the top of each file.

## Camera permissions

**Windows:** Settings > Privacy & security > Camera. Turn on **Camera access** and **Let desktop apps access your camera**.

**Mac:** The first run should prompt for camera access for the app that launched it (Terminal, iTerm, or VS Code). If it was denied, enable it in System Settings > Privacy & Security > Camera, then **fully quit and reopen** that app. *(Not yet verified on a Mac.)*

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Normal exit; camera released |
| 1 | Camera could not be opened or stopped delivering frames |
| 2 | Bad command-line option |
| 3 | A file in `assets/` (game image or detector) could not be loaded |

## Troubleshooting

| Symptom | Try |
| --- | --- |
| `Could not open camera 0` | Close Teams/Zoom/browser tabs using the camera; check permissions; run `--list-cameras` |
| Wrong camera opens (e.g., a virtual camera) | Run `--list-cameras`, then pass `--camera <n>` |
| Opens slowly or fails on Windows | Try `--backend dshow` |
| `DllNotFoundException` / `OpenCvSharpExtern` | The native runtime doesn't match your OS/CPU; check `dotnet --info` RID and the table above |
