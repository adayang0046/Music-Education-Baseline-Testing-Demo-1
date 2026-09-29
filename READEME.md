## 1. Hand tracking and pinch

**APK:** [QuestGestureTest.apk](Builds/QuestGestureTest.apk)

Checks whether Quest tracks your hands and detects thumb-to-index pinches.

**How to use:**

- Open **XR MIDI Gesture Test** on Quest with hand tracking enabled.
- Open your hand, pinch thumb and index together, hold for three seconds, then release.
- Repeat with each hand. Move one hand out of view, bring it back, then open and pinch again.

**Success:** Hands show **TRACKED**, markers follow your fingers, and each pinch/release increases its counter once. Holding a pinch does not repeatedly trigger it; tracking loss cancels it.

## 2. Simulated piano lesson

**APK:** [QuestLessonDemo.apk](Builds/QuestLessonDemo.apk)

Tests the lesson flow using automatic simulated notes, without a MIDI keyboard.

**How to use:**

- Open **XR MIDI Quest Lesson** and leave **Auto notes ON**.
- From Ready, open your hand and pinch to start.
- To use buttons, aim the white head-direction cursor at a button for **0.8 seconds**. Look away between selections.
- Let the lesson finish, then try **Restart**, **Pause**, and **Resume**.

**Success:** Notes move toward the hit line, the automatic run finishes with **Correct 5, Missed 0**, and Pause/Resume/Restart work. This result checks simulation, not your playing. No sound is expected.

## 3. Live MIDI lesson over Wi-Fi

**APK:** [QuestMidiRelayDemo.apk](Builds/QuestMidiRelayDemo.apk)

Tests real keyboard input sent from a Mac to the Quest lesson over Wi-Fi.

**How to use:**

- Connect **MPK mini 3** to the Mac by USB; put Mac and Quest on the same Wi-Fi.
- In Unity, exit Play Mode and open **XR MIDI → MIDI → Open Mac MIDI Monitor**.
- Select **MPK mini 3** and click **Connect selected source**. Press keys to check that the note counters increase.
- Open **XR MIDI Wi-Fi Lesson** on Quest. Enter the Quest's current Wi-Fi IP in the Mac monitor and click **Start Wi-Fi MIDI relay**. Keep the monitor open.
- From Ready, open your hand and pinch to start. Play MIDI pitches **60, 62, 64, 65, 67** as the notes reach the hit line.

**Success:** Quest shows **CONNECTED**, key presses/releases produce matching MIDI events on both devices, and well-timed correct notes receive positive feedback. Hand status changes between **OPEN**, **PINCH**, and **NOT TRACKED**. No sound is expected.
