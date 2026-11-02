# Face-Controlled Pipe Dodger

A **C# application for COSC 335 Project 3: Device Management**. Blink to flap a bird through scrolling pipes, with Space as a backup. The game uses the downloaded Flappy Bird sprites at their native 288×512 portrait resolution; see [artwork attribution](src/PipeDodger/assets/flappy-bird/README.md).

**Required camera features:** live video, a persistent captured still, grayscale on both, original/filtered comparisons, useful errors, and normal resource cleanup. Complete these before game polish.

## Teams and exact responsibilities

### Team 1 - Jake Barton (gameplay lead) + Aidan Bodkin

- **Jake:** Build character movement, pipes, collision, scoring, pause/restart, and game drawing using `Cv2.Circle/Rectangle/PutText` and `Stopwatch`. Lead integration and assemble the final source/assets/submission package.
- **Aidan:** Use `CascadeClassifier.DetectMultiScale` to detect a face, map its center to normalized vertical input, and flag missing/stale input. Bundle the detector asset. Compile everyone's research, troubleshooting, AI-use, and contribution notes; coordinate rehearsal.
- **Done when:** face movement controls a playable game; lost tracking pauses it; restart leaves the camera and captured images available.

### Team 2 - Aiden Fisher (camera lead) + Ethan Koch

- **Aiden:** Own camera selection/open/read/stop, live-frame delivery, failure messages, and cleanup using `VideoCapture`/`Mat`. Maintain platform dependencies and write verified setup/run instructions plus the referenced device-stack explanation.
- **Ethan:** Implement still capture with `Mat.Clone`, grayscale with `Cv2.CvtColor`, four labeled comparison views, and keyboard controls with `Cv2.ImShow/WaitKey`. Explain image ownership/filter behavior and observed responsiveness. Prepare the shared test checklist and evidence naming.
- **Done when:** live originals/grayscale update; captured originals/grayscale remain frozen while the scene changes; failed connections/reads are handled; normal exit releases resources.

**Everyone** must research, code, test on their own computer, contribute their own notes, review the other team's code, and explain any randomly assigned section. Document owners compile contributions; they do not replace them.

## Tools and Windows/Mac setup

Use **.NET 10 SDK** (unless the course pins another version), **VS Code + C# Dev Kit**, **Git/GitHub**, and **NuGet**. One C# console project opens an OpenCV graphical window.

Use [OpenCvSharp5](https://www.nuget.org/packages/OpenCvSharp5) (`5.0.0.20261018` as the starting version) plus the matching **full** native runtime:

| Target/process | Native package |
| --- | --- |
| Windows x64 | `OpenCvSharp5.runtime.win` |
| Windows ARM64, if applicable | `OpenCvSharp5.runtime.win-arm64` |
| Mac with Intel processor | `OpenCvSharp5.runtime.osx.x64` |
| Mac with Apple Silicon | `OpenCvSharp5.runtime.osx.arm64` |

Aiden configures runtime selection per target in the shared project; everyone uses compatible pinned versions. Follow the [package guide](https://shimat.github.io/opencvsharp/articles/getting-started/package-selection.html). This design needs camera I/O and HighGUI, so use full runtimes.

**Course OS check:** our machines include Windows and Mac. Aiden confirms each demo meets the OS assignment from Projects 1-2; an OS change requires instructor permission. Document VM camera access if applicable.

Bundle [haarcascade_frontalface_default.xml](https://github.com/opencv/opencv/blob/4.x/data/haarcascades/haarcascade_frontalface_default.xml) in `assets/`, copy it to build output, and retain its attribution.

**Current controls:** **Space** = start/flap; blink = flap; **C** = capture; **R** = reset to the start screen; **P** = pause/resume; **Q/Esc** = exit. Original/filtered pairs stay available in separate windows.

## Timeline, order, and blockers

**Proposed end-of-day targets: October 27-November 2, 2026.** Demo assumed around November 3; Jake confirms the actual deadline on Day 1. Blockers below are dependencies/risks, not reported failures.

| # | Due | Owner / deliverable | Needs first / possible blocker |
| --- | --- | --- | --- |
| 1 | Oct 27 | Everyone: shared project builds; OS/CPU, SDK/packages, window, model path, and frame format checked. | Course environment; native-library compatibility. |
| 2 | Oct 28 | Aiden + Ethan: live camera and clean close work on all four computers. | #1; permissions, device selection, VM passthrough. |
| 3 | Oct 28 | Jake + Aidan: playable game with temporary controls, collision, score, and restart. | #1 only; independent of camera progress. |
| 4 | Oct 29 | Ethan + Aiden: persistent capture, both grayscale results, and labeled comparisons. | #2; image format, stable frames, correct copying. |
| 5 | Oct 29 | Aidan + Jake: face-height input and missing/stale detection work. | #2; model loading and usable detection. |
| 6 | Oct 30 | Both teams: face-controlled game and camera tools integrated. | #3-5; coordinate mismatch, input delay, blocked window events. |
| 7 | Oct 31 | Everyone: full tests on four computers, screenshots, failure checks; freeze features. | #6; platform differences and remaining defects. |
| 8 | Nov 1 | Named owners above: complete setup, device stack, test evidence, research/AI notes, contributions, and source/assets. | Real results and everyone's written contributions. |
| 9 | Nov 2 | Everyone: timed 30-minute rehearsal; random code explanations. Fixes only. | #7-8; unresolved defects or knowledge gaps. |

## Handoffs and acceptance

- Aiden owns the only camera connection and supplies stable frames. Aidan returns `HasFace` and `TargetY01` (`0` top, `1` bottom); Jake consumes them. Ethan preserves originals and captures an independent raw-frame copy.
- Keep window event handling active each loop. Coordinate shared-file edits and raise blockers promptly.
- **Each member records:** OS/CPU, runtime/library versions, camera/device, permissions/VM setup, feature results, actual symptoms/investigation/fixes, limitations, and labeled live/still comparison screenshots.
- **Each computer demonstrates:** live video/filter, capture then movement to prove persistence, filtered still comparison, gameplay, normal exit, and successful reopen. The group also documents how camera-open/frame-read failure handling was tested; identify simulated tests honestly.
- **Aiden's device stack:** application calls, library, actual OS API/backend, driver role, physical webcam; cite sources and distinguish documented facts from observations.
- **Submission check - Jake:** source/assets, verified setup/run/controls, four test records with screenshots, device-stack explanation/references, research/troubleshooting/AI record, and contributions. Include limitations and one proposed improvement.
- **Rehearsal - Aidan:** 2 minutes overview, 8 minutes for four computers, 4 minutes device stack, 7 minutes code, 5 minutes research/troubleshooting/contributions, 4 minutes limitations/questions. Every member prepares to explain the entire app.

Reference: supplied COSC 335 Project 3 webcam rubric. This README is the work plan; implementation and test evidence must be completed before submission.
