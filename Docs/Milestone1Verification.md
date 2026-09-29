# Milestone 1 verification

Executed on 2026-09-21 using the installed Unity **2022.3.62f2** on macOS.

## Results

- Final Unity test run: **37 passed, 0 failed, 0 skipped**, duration 8.303 seconds.
- The Edit Mode suite includes a coroutine test that enters Play Mode, loads the
  actual generated demo scene, checks eight buttons and title, starts/pauses/resumes,
  completes the silent five-note lesson, verifies the summary, and restarts.
- Domain cases use an injected fake clock, including exact scoring boundaries,
  negative lead-in, pause/resume, misses, chords, duplicate scoring prevention,
  input logging, summary metrics, restart, and gesture edges/tracking loss.
- A worker-thread input test verifies delivery occurs only on main-thread drain,
  in timestamp order, and that pause/reset discards pending input.
- Unity compilation and scene generation succeeded. Core compilation explicitly
  disallows UnityEngine references.
- Inspected a Unity-rendered 1280x900 frame: panel, note, hit line, labels and
  controls are visible without overlap. The capture predates the final input-queue
  addition; no presentation/layout code changed afterward.
- Asset metadata and assembly JSON checks passed. Existing lesson scripts, old
  scenes, original lesson JSON, packages and checked project settings are unchanged.

Artifacts:

- [Final Unity NUnit XML](Verification/Milestone1Tests.xml)
- [Rendered demo preview](Verification/Milestone1Preview.png)

Tests and scene generation ran in an isolated temporary project copy. The new
scene and its metadata were copied back after generation through Unity APIs. The
final runtime/test sources match the tested copy; no existing scene YAML was edited.

The first headless test run passed 35/36 tests; the scene test was flagged by Unity's
missing-renderer warning under `-nographics`. Re-running with graphics enabled
passed 36/36. After adding queued main-thread input and its test, the final run
passed 37/37. No graphics warnings were suppressed to obtain that pass.

## Reproduction

Run the EditMode suite in Test Runner with the project opened in Unity 2022.3.62f2.
For batch runs, use graphics-enabled `-batchmode -runTests -testPlatform EditMode
-testResults <xml-path> -logFile <log-path>`. Do not add `-nographics` for the scene
test, or `-quit` for the test runner. Run on a saved project copy if preserving the
currently open scene is important: the integration test opens the demo scene.

Optionally set `XR_MIDI_CAPTURE_PATH` to an absolute PNG path before launching the
test runner to capture a frame during the scene test.

## Still requires manual/device verification

- Human keyboard timing, mouse interaction feel, and comfortable panel scale.
- Quest rendering, XR pointing/hand interaction and passthrough.
- Real left/right hand tracking, pinch reliability and tracking loss on Quest 3.
- Physical MIDI connectivity, latency and timestamp calibration.

No Quest, hand-tracking SDK, or physical MIDI integration was installed or tested.
Follow [the setup and manual acceptance checklist](Milestone1.md) before hardware
claims. The next milestone should establish one approved XR stack and physical
MIDI input on actual devices.
