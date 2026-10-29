# Ethan: capture + grayscale comparison

## What this adds

Four labeled OpenCV windows on top of Aiden’s `CameraSource` (no second camera open):

| Window | Updates |
| --- | --- |
| Live Original | Every frame (Aiden’s buffer) |
| Live Grayscale | Every frame (`Cv2.CvtColor` BGR→Gray) |
| Captured Original | Only when you press **C** (`frame.Clone()`) |
| Captured Grayscale | Built once from that clone |

Quit is unchanged: **Q**, **Esc**, close the Live Original window, or Ctrl+C.

## Why `Clone` matters

`CameraSource.TryReadFrame` reuses one `Mat`. Storing that reference as a “still” would keep changing with the live feed. `Clone()` copies the pixels so the captured pair stays frozen while you move.

## Filter

Grayscale runs in the app via OpenCvSharp (`ColorConversionCodes.BGR2GRAY`), not as a camera setting. Same conversion for live and still.

## Where the code lives

All of this is in `Program.RunLoop` in `src/PipeDodger/Program.cs`. Capture mats are disposed on recapture and when the loop ends; the camera is still released by `CameraSource.Dispose`.

## Quick test

1. `dotnet run` from `src/PipeDodger`
2. Confirm live color + live gray both move
3. Press **C**, then leave the frame — captured windows must stay frozen
4. Press **C** again — stills refresh
5. Quit and confirm the camera is free for another app / reopen

## Screenshot naming (shared evidence)

`{member}-{os}-live-compare.png`  
`{member}-{os}-capture-persist.png`  

Example: `ethan-win-capture-persist.png`
