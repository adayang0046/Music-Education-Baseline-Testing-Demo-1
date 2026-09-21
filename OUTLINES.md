# Task: Implement Milestone 1 — Playable XR MIDI Rhythm-Game Demo

Read AGENTS.md and inspect the existing Unity project before making modifications.

## 1. Project context

We are developing a mixed-reality piano learning assistant using:

* Unity
* Meta Quest 3
* A physical MIDI keyboard
* An existing Figma UI prototype

The long-term system will include structured lessons, performance evaluation, visual feedback, gesture-based interaction, learner modeling, and personalized exercise recommendations.

The immediate goal is to build a small, playable rhythm-game demo.

The game should resemble a simplified falling-note piano tutor: notes descend toward a hit line, and the learner plays the corresponding keys at the correct time.

Maintain a modular architecture so that future research components can be replaced or extended independently.

## 2. Inspect the existing project

Before implementation:

1. Read AGENTS.md.
2. Identify the Unity and XR package versions.
3. Inspect existing scenes, scripts, input systems, and prefabs.
4. Identify existing hand-tracking support.
5. Identify existing MIDI integration.
6. Review the Figma reference if accessible.

Preserve existing functionality and project settings.

Produce a short implementation plan before changing files.

Do not install or upgrade packages without approval.

---

# 3. Core architecture

Implement or extend the following modules:

* Lesson System
* Musical Timeline
* MIDI Input System
* Falling-Note Visualization
* Performance Evaluation
* Feedback System
* Gesture Detection
* XR Presentation

Keep the modules loosely coupled through interfaces and events.

Use plain C# classes for domain logic where possible.

MonoBehaviours should primarily handle Unity lifecycle, scene references, and presentation.

Avoid implementing all functionality in one large manager script.

## Required interfaces

Define suitable interfaces for:

* ILessonRepository
* IMidiInput
* IMusicalClock
* IPerformanceEvaluator
* INoteVisualizer
* IGestureProvider

The interfaces should allow alternative implementations without modifying the lesson controller.

For example:

IMidiInput should support both simulated input and physical MIDI input.

IGestureProvider should support simulated gestures and Quest hand-tracking gestures.

INoteVisualizer should allow replacing the initial falling-note visualization with a more advanced XR representation later.

Use events or explicit method calls to communicate between systems.

---

# 4. Lesson data structure

Implement a data-driven lesson format using JSON.

Each note should contain:

* MIDI pitch
* Start beat
* Duration in beats
* Optional hand assignment

Each lesson should contain:

* Unique ID
* Title
* Tempo (BPM)
* Time signature
* Note sequence
* Optional difficulty metadata

Support multiple notes beginning at the same beat so that chords can be added later.

Include an initial lesson containing:

C4 → D4 → E4 → F4 → G4

Use MIDI pitches 60, 62, 64, 65, and 67.

Begin with five quarter notes at 60 BPM.

Create appropriate domain models for notes, lessons, and performance results.

Do not hardcode lesson content directly into MonoBehaviours.

---

# 5. Musical timeline and synchronization

Implement a shared musical clock responsible for maintaining the current lesson time.

The clock should support:

* Start
* Pause
* Resume
* Restart
* Current elapsed time
* Current beat position
* Conversion between beats and seconds

Use a monotonic, high-precision time source appropriate for Unity.

Ensure that the falling-note visualization and performance evaluator reference the same clock.

Do not use independent timers for visual movement and scoring.

For this demo, use a constant tempo per lesson.

Design the clock so that variable tempo can be supported in the future.

Define a clear pause/resume policy so that pausing does not affect timing accuracy.

Record input timestamps consistently relative to the lesson timeline.

Document the selected time source and any known latency limitations.

---

# 6. Falling-note visualization

Implement a simplified rhythm-game note visualization.

The interface should display:

* Five visible note lanes corresponding to C4–G4.
* A horizontal hit line.
* Falling note objects.
* A current lesson progress indicator.
* Basic feedback text.

Notes should spawn or become visible ahead of their scheduled hit time.

They should descend toward the hit line according to their scheduled start beat.

The note should reach the hit line exactly when the musical clock reaches its scheduled time, subject to rendering accuracy.

For example:

At 60 BPM:

* C4 reaches the hit line at beat 0.
* D4 reaches the hit line at beat 1.
* E4 reaches the hit line at beat 2.

Include a configurable approach time, initially 2 seconds.

Start the lesson clock with an appropriate lead-in so the first note is visible before it reaches the hit line.

Use a time-based position calculation rather than accumulating frame-by-frame movement.

This should prevent visual drift when frame rates change.

Each falling note should represent a scheduled musical event.

Use a simple object pool if helpful, but avoid overengineering.

For the first implementation, use simple colored rectangles or lightweight meshes.

Do not spend time implementing final visual polish.

Keep the visualizer separate from the lesson data and evaluator.

Create configurable parameters for:

* Note fall distance
* Approach duration
* Lane spacing
* Hit-line position
* Note dimensions

The implementation should eventually support replacing the flat display with spatially aligned XR keyboard visualization.

---

# 7. MIDI input

Implement simulated MIDI input first.

Provide a simple way to trigger C4–G4 using computer keyboard keys or temporary UI buttons.

Normalize input into a common NoteEvent model.

Each NoteEvent should contain:

* MIDI pitch
* Note-on or note-off status
* Velocity
* Timestamp
* Optional channel

Define MIDI note-on with velocity zero as note-off when appropriate.

Ensure Unity-facing callbacks are handled safely on the main thread if input originates from another thread.

Preserve the IMidiInput abstraction so that a physical MIDI keyboard can be integrated later.

Do not claim that physical MIDI works until tested on the actual device.

---

# 8. Rhythm-based performance evaluation

Implement a basic pitch-and-onset-timing evaluator.

The evaluator should compare MIDI input against expected lesson notes.

For each expected note, record:

* Whether the correct pitch was played.
* Expected onset time.
* Actual onset time.
* Timing error in milliseconds.
* Whether the note was missed.
* Whether an incorrect note was played.

Use configurable timing windows.

Initial provisional settings:

* Perfect: within ±50 ms.
* Good: within ±120 ms.
* Late/Early but accepted: within ±200 ms.
* Miss: outside the accepted window.

These values are starting parameters, not research-validated scoring criteria.

Do not make them fixed constants scattered throughout the project.

Handle early and late input consistently.

Prevent a single MIDI input event from scoring multiple notes.

Ensure each expected note can be matched at most once.

Keep incorrect extra-note events in the performance log.

For the first demo, score note onset only.

Record note-off events for future duration evaluation, but do not require accurate note duration to complete the first lesson.

Design the evaluator to support chords and duration scoring later.

If chords are not fully implemented, document the limitation.

---

# 9. Feedback and lesson progression

Provide immediate feedback when a learner plays a note.

Possible feedback results:

* Perfect
* Good
* Early
* Late
* Wrong pitch
* Miss

The visualizer should respond to evaluation events.

For example:

* Correct notes disappear or change appearance.
* Incorrect notes trigger a brief visual response.
* Missed notes are marked after their scoring window expires.

Keep feedback logic separate from the evaluator.

The lesson should continue according to the musical timeline rather than waiting indefinitely for every correct note.

At the end, display:

* Total expected notes
* Correct notes
* Missed notes
* Incorrect extra notes
* Average absolute timing error

Provide Restart and Pause controls.

Ensure restarting resets the clock, evaluation state, visual objects, and lesson progress.

---

# 10. Gesture detection initialization

Initialize a modular gesture-detection foundation for Meta Quest 3.

First inspect the existing XR configuration.

Determine whether the project already includes compatible hand-tracking support through OpenXR, XR Hands, or Meta's SDK.

Reuse the existing configuration wherever possible.

Do not blindly install multiple overlapping hand-tracking systems.

## Required functionality

Create an IGestureProvider interface.

Implement a Quest hand-tracking adapter using the available SDK if possible.

Expose:

* Left-hand tracking availability
* Right-hand tracking availability
* Hand pose or joint data where supported
* Gesture events

For the first demo, initialize one simple gesture, such as a pinch.

The gesture should produce events like:

GestureStarted
GestureEnded

Implement configurable thresholds or use an appropriate existing gesture API.

Avoid emitting duplicate events continuously while a gesture is held.

Ensure tracking loss safely ends or cancels active gestures.

Create a simulated gesture provider so the system can be tested in the Unity Editor without the headset.

## Initial gesture interactions

Use gestures only for a simple, optional interaction, such as selecting or starting a lesson.

Do not make gesture detection a requirement for completing the MIDI exercise.

Keep ordinary UI controls available as a fallback.

Do not attempt full finger-technique evaluation, physical key-contact recognition, or automatic keyboard calibration yet.

If the existing SDK does not support a requested gesture feature, implement the interface and simulation layer, then document the remaining hardware integration work.

Distinguish successful initialization from verified hand tracking on Quest 3.

---

# 11. XR presentation

Create a simple demo scene using the existing XR setup.

Use a floating lesson panel that can eventually be positioned above the physical MIDI keyboard.

The panel should contain:

* Lesson title
* Falling-note display
* Hit line
* Feedback display
* Progress information
* Start, Pause, and Restart controls

Keep the initial scene minimal.

Prefer Unity UI or the existing UI framework already used by the project.

Do not introduce a new UI framework unnecessarily.

Use the existing Figma design as a reference if available, but do not attempt pixel-perfect implementation during this milestone.

Avoid changing existing XR settings unless required.

If headset rendering cannot be verified locally, make the demo testable in the Unity Editor and clearly document the remaining Quest verification steps.

Manual alignment with the physical keyboard can be implemented in a later milestone.

---

# 12. Testing

Implement Edit Mode tests for the pure application and domain logic.

Required tests:

1. Lesson JSON loads correctly.
2. Beat-to-time conversion is correct.
3. Pause and resume preserve lesson time.
4. Falling-note position follows the expected timeline.
5. Correct pitch and timing are accepted.
6. Incorrect pitch is rejected.
7. Early and late notes are classified correctly.
8. Missing notes are detected.
9. A single input cannot score multiple notes.
10. Restart resets the complete lesson state.

Use an injectable or fake clock for deterministic timing tests.

Add tests for gesture state transitions if gesture recognition logic is implemented.

Include a manual Play Mode testing procedure covering the complete five-note lesson.

Do not claim tests passed unless they were actually executed.

---

# 13. Scope restrictions

Do not implement:

* Machine-learning recommendation algorithms.
* Full learner profiling.
* Automatic physical keyboard recognition.
* Finger-to-key contact detection.
* Advanced gesture classification.
* Multiplayer features.
* A complete music editor.
* Final visual polish.
* Network synchronization.

These features belong to later milestones.

The current goal is a functional architecture and one small playable demo.

---

# 14. Deliverables

After implementation, provide:

1. A concise architecture explanation.
2. The folder and file structure.
3. A list of created and modified files.
4. Unity scene setup instructions.
5. Instructions for running the simulated MIDI lesson.
6. Instructions for testing gesture simulation.
7. Quest 3 hand-tracking setup and verification instructions.
8. Automated test results.
9. Known limitations.
10. Recommended next implementation milestone.

Do not commit changes automatically.

Do not report the physical MIDI keyboard or Quest gesture system as working unless verified on the actual hardware.

## Definition of done

The demo should allow a learner to:

1. Start a lesson.
2. See notes falling toward a hit line.
3. Press the corresponding simulated MIDI notes.
4. Receive pitch-and-timing feedback.
5. Complete the five-note exercise.
6. View a performance summary.
7. Restart the lesson.

A gesture provider and simulated gesture input must be initialized.

Quest hand-tracking integration should be implemented when supported by the existing SDK, with hardware verification documented separately.
