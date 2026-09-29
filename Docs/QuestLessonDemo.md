# Quest lesson integration demo

This scene connects real Quest hand input to the existing lesson engine. Its MIDI
input is explicitly simulated. It does not receive the USB keyboard attached to
the Mac, infer piano finger correctness, or synthesize piano audio.

## Three testing scenes

| Scene | Purpose | Inputs |
| --- | --- | --- |
| Milestone1Demo | Fast desktop lesson and scoring iteration | Keyboard/UI simulation, optional Mac USB MIDI monitor, simulated pinch |
| QuestGestureTest | Isolate headset tracking issues | Real XR Hands, pinch counters and joint markers |
| QuestLessonDemo | Integrated spatial lesson user flow | Real pinch-to-start, simulated MIDI, head-gaze button fallback |

Use one scene at a time when testing. Scene creation is additive to preserve work;
close other scenes before Play Mode to avoid duplicate cameras and EventSystems.
Builders refuse to overwrite existing scene assets.

## Headset procedure

Install `Builds/QuestLessonDemo.apk` and launch **XR MIDI Quest Lesson**. Its package
ID is `com.xrmidi.questlesson`, separate from the existing gesture diagnostic.

1. Face the initial forward direction. A fixed floating panel displays the five-note
   lesson; a small white dot follows your head direction. No passthrough or hand mesh
   is rendered in this scene. The bottom line reports left/right hand availability.
2. **Auto notes: ON (test)** is the default. The bottom line also says **SIMULATED
   MIDI: AUTO NOTES ON**. This mode generates scheduled test notes and should produce
   five correct notes; it is not measuring your playing or MIDI timing accuracy.
3. Open your hand, then pinch thumb and index. A new pinch starts the lesson only
   while its state is Ready. Either hand works. Hold the pinch: it must not restart.
4. As an independent fallback, point the dot at **Start** for 0.8 seconds. Button
   selection uses head gaze, not eye tracking or a hand ray. Look away before
   selecting the same button again. The hint shows the current target and progress.
5. Let the simulated notes complete the exercise. Expect **Correct 5, Missed 0**.
6. Look at **Restart**, or **Start** after completion, to run again. Pinch does not
   restart a completed lesson; that is intentionally an explicit button action.
7. During a trial, look at **Pause**, look away, then look at **Resume**. Check that
   note positions and scoring remain paused between these actions.
8. Remove a hand while pinching. Losing hands must not alter the lesson or score;
   the gesture provider cancels the pinch. On reacquisition, open before pinching.
9. Open the system menu or remove the headset. The lesson pauses on focus loss or
   suspension. On return, look away and select **Resume**; it must not resume itself.

To try manual simulated notes, turn **Auto notes OFF** while Ready or Complete.
Then start the lesson and select the MIDI 60/62/64/65/67 buttons by gaze. These are
momentary note-on/off pairs. The dwell delay makes this a UI exercise, not a precise
piano input method. Misses are expected if selection is not aligned with note timing.
The automatic mode cannot change during a running or paused trial.

## Architecture

- The same `RhythmDemoController`, lesson JSON, `LessonSession`, musical clock,
  evaluator, input queue and `FallingNotePanel` serve both desktop and Quest scenes.
- The scene assigns a `MonoBehaviour` implementing `IGestureProvider`. Quest uses
  `QuestHandGestureProvider`; desktop still uses `SimulatedGestureProvider`.
- Pure C# `GestureLessonBinding` translates new, tracked pinch edges into the same
  Start action used by the button. It ignores Playing, Paused and Complete states,
  consumes duplicate starts until release, and unsubscribes on disposal.
- Pure C# `LessonNoteSimulator` schedules explicit test input from lesson data.
  It emits through the existing simulated MIDI emitter and `MainThreadMidiInput`
  boundary before the evaluator's miss sweep. It resets on restart and advances
  only with the playing lesson clock. Exact scheduled timestamps intentionally
  produce ideal timing even when rendering is delayed.
- `QuestLessonControls` selects ordinary uGUI Buttons through a camera-centered
  GraphicRaycaster. Pure C# `DwellSelection` enforces one activation per continuous
  look. Focus loss resets dwell. Button callbacks invoke application actions; the
  XR presentation never evaluates pitch or musical timing.
- Desktop keyboard polling is explicitly disabled in this scene and compiled out
  when the legacy input backend is unavailable. Android uses its existing new-input
  OpenXR build configuration. The original desktop defaults remain enabled.

## Files added

- `Assets/Scripts/Rhythm/Core/GestureLessonBinding.cs`
- `Assets/Scripts/Rhythm/Core/LessonNoteSimulator.cs`
- `Assets/Scripts/Rhythm/Core/DwellSelection.cs`
- `Assets/Scripts/XR/QuestLessonControls.cs`
- `Assets/Editor/XR/QuestLessonSceneBuilder.cs`
- `Assets/Editor/XR/QuestLessonBuild.cs`
- `Assets/Scenes/QuestLessonDemo.unity`
- `Assets/Tests/EditMode/QuestLessonLogicTests.cs`
- `Assets/Tests/XR/QuestLessonSceneTests.cs`
- Unity metadata and this documentation.

Shared changes: `RhythmDemoController`, `FallingNotePanel`, and `SimulatedInputs`
provide input selection, accurate presentation labels and scheduled test input.
XR runtime/editor/test assembly definitions reference the shared runtime assembly.
Existing lesson scenes, package versions and working-project settings are preserved.

## Reproduce the Android build

Use a disposable copy of Assets, Packages and ProjectSettings with Unity 2022.3.62f2.
Place `QUEST_GESTURE_BUILD_COPY` in its root. Run in separate batch invocations:

1. `-executeMethod QuestLessonBuild.ConfigureCopy -questGestureBuildCopy -quit`
2. `-buildTarget Android -executeMethod QuestLessonBuild.BuildCopy -questGestureBuildCopy -quit`

Pass `-batchmode -projectPath <copy> -logFile <file>` in each invocation. The helper
reuses the verified gesture build configuration, then selects the lesson package ID
and scene. Its output is `<copy>/Builds/QuestLessonDemo.apk`. It refuses to overwrite
an existing APK. Do not change the working project's input backend merely to run
this isolated test; that can affect desktop keyboard controls.

## Acceptance criteria

- Automated core tests cover new-pinch gating, duplicate events, tracking loss,
  invalid timing, dwell reset, simulator scheduling and restart.
- Editor scene test completes the five-note automatic run, checks pause/resume,
  restart, mode labeling and fallback buttons without claiming real hand tracking.
- Existing desktop MIDI and lesson tests remain green.
- On Quest, verify readable panel, pinch-to-start, head-gaze controls, completion,
  pause/resume, restart and focus/tracking recovery. These remain hardware acceptance
  checks even when the build and Editor tests pass.

See `QuestLessonVerification.md` for executed results and remaining device checks.
