# Implementation and Testing Methodology

## Why we use three test APKs

We develop the application in one Unity project and build separate APKs for different testing purposes. This helps us identify the source of a problem: lesson logic and UI can be tested without a physical keyboard, while hand tracking can be tested without the full lesson flow. The integrated demo then checks whether the tested components work together. Separate app identifiers allow the three demos to remain installed on the same Quest.

| APK | Current purpose | Future testing role |
| --- | --- | --- |
| `QuestLessonDemo.apk` | Test lesson progression, feedback, pause, and restart with simulated MIDI. | Test the lesson library, lesson selection, recommendation behavior, and UI. Use controlled inputs and learner histories to repeat the same scenarios. |
| `QuestGestureTest.apk` | Test hand tracking, pinch recognition, and tracking-loss recovery. | Test new gestures and investigate finger-to-key identification after keyboard calibration is available. |
| `QuestMidiRelayDemo.apk` | Connect real keyboard input, the lesson engine, and Quest presentation through the Mac MIDI relay. | Serve as our main integration test for the newest demo, combining features that have passed their focused tests. |

The future roles describe planned development. A lesson library, recommendation system, and finger-to-key identification are not implemented yet. The current relay requires macOS.

## How we make functions reusable

The APKs share source code; each scene selects and connects the components it needs. We integrate features by composing these components and rebuilding an APK, rather than merging APK files or copying logic between scenes.

- **Shared lesson logic:** `LessonSession`, `MusicalClock`, and `OnsetEvaluator` manage progression, timing, and scoring. These are plain C# classes without Unity dependencies, so they can be tested independently of the headset.
- **Replaceable inputs:** `IMidiInput` and the shared `NoteEvent` format provide a common boundary for simulated and hardware input. `IGestureProvider` similarly separates gesture consumers from simulated or Quest hand tracking.
- **Data-driven lessons:** `ILessonRepository` supplies lesson data. The current JSON repository allows lesson content to change without embedding exercises in scene scripts.
- **Separate presentation:** `INoteVisualizer` defines a visualization boundary. Unity components display notes and feedback, while musical evaluation stays in the shared logic. `RhythmDemoController` connects the lesson components.
- **Focused adapters:** `QuestHandGestureProvider` handles headset hand data; the network sender and receiver handle MIDI transport. Hardware or transport changes should remain behind these boundaries.

For example, a new gesture is first checked in the gesture APK. Once reliable, its provider can be connected to a lesson action in the main demo. A new lesson is first loaded and exercised in the lesson APK, then used by the same lesson engine with real MIDI in the relay APK. A future recommendation module should select lesson identifiers from learner results through a defined interface; its selection logic should not be placed inside UI buttons.

## How we develop and test a new feature

1. **Define expected behavior.** Specify the input, expected output, success condition, and behavior when input is invalid or unavailable.
2. **Implement the smallest shared component.** Keep application logic separate from Unity display code and hardware access. Reuse existing interfaces where they fit.
3. **Run automated tests.** Use simulated MIDI, fake clocks, and controlled data to check normal behavior, invalid input, completion, restart, and interruption handling. For recommendations, repeat tests with fixed learner histories.
4. **Test in the focused APK.** Use the lesson APK for lesson/library/recommendation/UI changes and the gesture APK for tracking and gesture changes. Add a small diagnostic scene when neither isolates the feature adequately.
5. **Integrate into the relay APK.** Connect the shared component to the real input and presentation, then test the complete user flow on Quest with the keyboard.
6. **Check for regressions and record results.** Repeat affected earlier tests. Record the source commit, APK version or hash, device/setup, steps, expected result, actual result, and known limitations. Identify checks that were not performed.

## What counts as success

Automated tests demonstrate repeatable logic behavior. Focused APK tests demonstrate the relevant interaction on the device. The relay APK demonstrates how the components behave together, including connection loss, pause/resume, tracking loss, and restart.

A simulated perfect score does not prove real MIDI timing accuracy. A running hand-tracking subsystem does not prove correct finger identification. Recommendation tests can verify selection rules, but their educational value requires later learner evaluation. We report these levels separately so that each new demo has clear evidence of what was tested.

See [the testing guide](READEME.md) for APK downloads, setup steps, and observable success conditions.
