# Milestone 1: timed piano demo

## Scope and compatibility

Implements the timed demo in OUTLINES.md alongside the original pitch-only prototype.
Unity remains 2022.3.62f2. Packages, project settings, existing scenes, existing script
GUIDs, and `beginner_scale.json` are preserved. No physical MIDI or Quest SDK is added.
The original `LessonManager`, `LessonLoader`, and `LessonDebugTester` still provide
their original behavior; the new scene uses `RhythmDemoController` instead.

## Architecture and files

```
Assets/Scripts/
  XRMidi.Runtime.asmdef             Unity adapters + existing scripts
  Rhythm/Core/
    XRMidi.Core.asmdef              noEngineReferences: true
    Contracts.cs                   six replaceable module interfaces and input events
    Lesson.cs                      validated beat-based JSON models and snapshots
    MusicalClock.cs                injected monotonic time source, pause/resume
    PerformanceResult.cs           immutable results, summary, timing configuration
    OnsetEvaluator.cs              one-to-one onset matching and miss detection
    LessonSession.cs               application lifecycle and absolute note trajectory
    SimulatedInputs.cs             MIDI and edge-triggered gesture simulation
    MainThreadMidiInput.cs          thread-safe queue, ordered main-thread delivery
  Rhythm/Unity/
    JsonLessonRepository.cs        Unity JSON adapter
    RhythmDemoController.cs        composition, main-thread input, lifecycle
    FallingNotePanel.cs            world-space uGUI lanes, notes, controls, status
    DemoFeedback.cs                presentation wording and summary formatting
Assets/Editor/
  XRMidi.Editor.asmdef
  Milestone1SceneBuilder.cs         non-overwriting Unity scene creation command
Assets/Resources/Lessons/
  rhythm_five_notes.json            C4-D4-E4-F4-G4, beats 0-4, quarter notes, 60 BPM
Assets/Scenes/Milestone1Demo.unity   new, separate Editor-playable demo
Assets/Tests/EditMode/
  XRMidi.EditModeTests.asmdef
  RhythmCoreTests.cs                deterministic domain, timing, input tests
  DemoSceneTests.cs                 enters Play Mode to check the actual scene
```

Core dependencies flow through `ILessonRepository`, `IMidiInput`, `IMusicalClock`,
and `IPerformanceEvaluator`. `INoteVisualizer` receives snapshots and evaluation
results. `IGestureProvider` exposes left/right availability and started/ended
events. The controller is the Unity composition boundary; swapping an adapter
does not change `LessonSession`. No global singleton or dependency framework.
The visualizer computes positions and displays results; it never decides correctness.

The root runtime assembly adds a compilation boundary for existing scripts without
renaming or moving them. The nested core assembly cannot reference UnityEngine.

## Open and run

1. Open the project with Unity **2022.3.62f2** and let scripts import.
2. Open `Assets/Scenes/Milestone1Demo.unity`. It is intentionally not added to Build
   Settings. Do not run it additively with another camera/EventSystem scene.
3. If the scene is missing, use **XR MIDI > Create Milestone 1 Demo Scene**. This
   creates a new scene through Unity APIs and refuses to overwrite an existing one.
4. Enter Play Mode and focus the Game view. Use a landscape Game view, preferably
   1280x900. The dark panel is a World Space Canvas (1.8 x 1.52 Unity units).
5. Click **Start** or press **Space**. There is a two-second lead-in.
6. Press **A S D F G** for **C4 D4 E4 F4 G4**, at the hit line. Each key-down is a
   note-on; key-up is recorded as note-off. The onscreen note buttons are an
   alternative and send an immediate on/off pair.
7. **Space** or **Pause** pauses/resumes; **R** or **Restart** starts a clean run.
   Completion occurs just after beat 5, including all note-end/window deadlines.

The scene has a desktop camera, world-space Canvas, EventSystem, and composition
object with assigned JSON and panel references. Child UI elements are generated at
runtime; edit the panel's serialized approach, spacing, distance, hit line and note
size in the Inspector. The panel supports the five named lanes only and rejects
unsupported pitches explicitly. Timing windows live together on the controller.

## Timeline, scoring, and input policy

- Beat units are **quarter notes**, independent of the displayed time-signature
  denominator. Seconds = beats * 60 / BPM. Constant tempo per run.
- Unity supplies `Time.realtimeSinceStartupAsDouble` to `MusicalClock`; tests use a
  fake source. This double-precision clock is independent of `Time.timeScale`.
  See https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Time-realtimeSinceStartupAsDouble.html.
- Timeline starts at minus the approach duration (default -2 seconds). At beat 0,
  C4 reaches the hit line. Position is recomputed from absolute lesson time every
  frame, never integrated from frame deltas.
- Pause freezes lesson time, including lead-in. Resume discards paused wall time.
  Input while ready, paused, or complete is ignored. Focus loss/disable pauses the
  session; returning requires explicit resume. Held keys do not synthesize new
  onsets on resume; release and press again.
- Provisional inclusive windows: Perfect <= 50 ms, Good <= 120 ms, accepted
  Early/Late <= 200 ms. Signed error = actual minus expected onset.
  These are configurable engineering defaults, not validated learning metrics.
- Among pending targets of the same pitch in the accepted window, choose the
  nearest onset, breaking ties by sequence index. One input resolves at most one
  target. A target is resolved at most once. Different-pitch simultaneous notes
  are supported as independent onsets; overlapping same-lane rectangles and chord
  group grading are not implemented.
- A wrong pitch near a pending onset logs WrongPitch without consuming it.
  Other unmatched inputs log Extra. An input outside its target's window does
  not earn a hit; the target independently becomes Miss after the late deadline.
  The summary counts WrongPitch and Extra together as incorrect extra notes.
- Note-off (including velocity-zero note-on) is logged during active play but
  does not score duration. The run continues even if every note is missed.
- Average absolute timing error includes accepted onsets only; no hits shows N/A.
- Restart reloads/validates JSON and snapshots targets, resets input/results and
  clock, recreates note visuals, and clears feedback/progress. Missing or invalid
  configuration logs an explicit error instead of starting a partial session.

Keyboard inputs are collected in Update and UI actions through EventSystem.
`MainThreadMidiInput` buffers source callbacks under a lock and drains in timestamp
order in LateUpdate, **before session Tick** and visualization. This also safely
marshals future worker-thread MIDI callbacks to Unity's main thread. Input acceptance
is disabled while paused/ready/complete; pause and restart discard pending events.
A future native adapter must still convert device timestamps into the lesson
timeline and deliver before the applicable scoring deadline. Do not call the
Unity clock or Unity-facing subscribers from native threads.
This simulator polls once per frame; input/render latency and device offset are
not calibrated. It is not an audio-sample-accurate metronome or physical MIDI test.

## Gesture simulation

- Both simulated hands initialize as available. The panel explicitly labels this
  as simulation, not hardware tracking.
- Hold **P** for a right-hand pinch; release ends it. One start event per hold.
  In Ready state this optionally starts the lesson. It never scores a piano note.
- Press **T** to toggle right-hand tracking availability. Tracking loss cancels
  an active pinch exactly once. Returning tracking permits a new pinch.
- Start/Space and all note controls work without gestures. No joint data is
  fabricated by the simulator.

## Quest and MIDI follow-up

Inspection found built-in XR modules only: no XR Plug-in Management, OpenXR,
XR Hands, Meta hand-tracking SDK, or physical MIDI plugin. The disabled legacy
Standalone Oculus/OpenVR setting is not an operational Quest configuration.
There is consequently no honest SDK-backed Quest adapter to initialize here.

For the next hardware milestone, select one compatible XR/hand-tracking stack
after approval, configure its Quest provider and required permissions, and adapt
its tracking/pinch events to `IGestureProvider`. Bind a tracked camera and suitable
XR UI interaction to the floating panel. Replace the desktop camera/input module
as part of that deliberate setup; world-space placement alone does not enable XR.
On Quest 3 verify left/right availability, single pinch edges, release, tracking
loss cancellation, focus/suspend behavior, panel legibility, and fallback controls.
Only then claim hardware support. USB/Bluetooth MIDI integration, permissions,
thread dispatch, timestamp mapping and latency measurement need separate device tests.

## Tests and manual acceptance

Open **Window > General > Test Runner > EditMode**, run `XRMidi.EditModeTests`.
`DemoSceneTests` enters/exits Play Mode; save your working scene before running it.
Batch execution uses `-runTests -testPlatform EditMode -testResults <path>` without
`-quit` (the runner exits after finishing). See `Milestone1Verification.md` for
actual executed results; authored tests alone are not a pass claim.

Manual Play Mode checklist:

1. Start: first note approaches for two seconds and reaches the line at beat 0.
2. Hit A/S/D/F/G one second apart: immediate judgements and five correct results.
3. Try wrong, early/late, repeated and missing notes: no double scoring; the
   timeline continues and the summary distinguishes misses from extra input.
4. Pause during lead-in and during play; wait, then resume. Notes do not jump.
5. Restart while playing, paused and completed: clean clock, visuals and results.
6. Leave the Game view/application: focus loss pauses; resume explicitly.
7. Hold P, release, and lose tracking using T: no repeated gesture starts while held.
8. Confirm all onscreen controls are clickable and labels fit at target resolution.

No hand tracking, physical keyboard alignment, audio playback, sustain scoring,
recommendation engine, learner persistence, or final Figma styling is claimed.
Recommended next milestone: verify the chosen Quest rendering/input stack and
physical MIDI timestamp pipeline before adding calibration or more lesson content.
