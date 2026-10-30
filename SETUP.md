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
| C | Capture a still (original + grayscale); stays frozen until the next C |
| Q / Esc / close Live Original / Ctrl+C | Quit and release the camera |

Five windows: **Live Original**, **Live Grayscale**, **Captured Original**, **Captured Grayscale** (captured pair appears after the first C), and **Game** (scrolling art only so far). Details: [docs/ethan-image-views.md](docs/ethan-image-views.md).

*Game controls (R / P / face steering) will be added by Team 1.*

## Camera permissions

**Windows:** Settings > Privacy & security > Camera. Turn on **Camera access** and **Let desktop apps access your camera**.

**Mac:** The first run should prompt for camera access for the app that launched it (Terminal, iTerm, or VS Code). If it was denied, enable it in System Settings > Privacy & Security > Camera, then **fully quit and reopen** that app. *(Not yet verified on a Mac.)*

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Normal exit; camera released |
| 1 | Camera could not be opened or stopped delivering frames |
| 2 | Bad command-line option |
| 3 | A game image in `assets/` could not be loaded |

## Troubleshooting

| Symptom | Try |
| --- | --- |
| `Could not open camera 0` | Close Teams/Zoom/browser tabs using the camera; check permissions; run `--list-cameras` |
| Wrong camera opens (e.g., a virtual camera) | Run `--list-cameras`, then pass `--camera <n>` |
| Opens slowly or fails on Windows | Try `--backend dshow` |
| `DllNotFoundException` / `OpenCvSharpExtern` | The native runtime doesn't match your OS/CPU; check `dotnet --info` RID and the table above |
