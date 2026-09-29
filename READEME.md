## 1. Hand tracking and pinch

**APK:** [QuestGestureTest.apk](https://github.com/adayang0046/Music-Education-Baseline-Testing-Demo-1/raw/refs/heads/main/Builds/QuestGestureTest.apk)

Checks whether Quest tracks your hands and detects thumb-to-index pinches.

**How to use:**

- Download the APK above; no Unity project is needed. If GitHub shows a file page, click **Download raw file**.
- Enable Developer Mode on Quest using [Meta’s setup guide](https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/). Connect Quest to your computer by USB and accept the USB debugging prompt.
- In **Meta Quest Developer Hub → Device Manager → Apps**, drag in the APK or select **Add Build** to install it ([installation guide](https://developers.meta.com/horizon/documentation/spatial-sdk/ts-mqdh-deploy-build/)). The computer is only needed for installation.
- In Quest’s **App Library → Unknown Sources**, open **XR MIDI Gesture Test** on Quest with hand tracking enabled.
- Open your hand, pinch thumb and index together, hold for three seconds, then release.
- Repeat with each hand. Move one hand out of view, bring it back, then open and pinch again.

**Success:** Hands show **TRACKED**, markers follow your fingers, and each pinch/release increases its counter once. Holding a pinch does not repeatedly trigger it; tracking loss cancels it.

## 2. Simulated piano lesson

**APK:** [QuestLessonDemo.apk](https://github.com/adayang0046/Music-Education-Baseline-Testing-Demo-1/raw/refs/heads/main/Builds/QuestLessonDemo.apk)

Tests the lesson flow using automatic simulated notes, without a MIDI keyboard.

**How to use:**

- Download the APK above; no Unity project or MIDI keyboard is needed.
- Install it on Quest using the same Developer Hub steps as Test 1.
- In **App Library → Unknown Sources**, open **XR MIDI Quest Lesson** and leave **Auto notes ON**.
- From Ready, open your hand and pinch to start.
- To use buttons, aim the white head-direction cursor at a button for **0.8 seconds**. Look away between selections.
- Let the lesson finish, then try **Restart**, **Pause**, and **Resume**.

**Success:** Notes move toward the hit line, the automatic run finishes with **Correct 5, Missed 0**, and Pause/Resume/Restart work. This result checks simulation, not your playing. No sound is expected.

## 3. Live MIDI lesson over Wi-Fi

**APK:** [QuestMidiRelayDemo.apk](https://github.com/adayang0046/Music-Education-Baseline-Testing-Demo-1/raw/refs/heads/main/Builds/QuestMidiRelayDemo.apk)

Tests real keyboard input sent from a Mac to the Quest lesson over Wi-Fi.

**How to use:**

- **This test requires a Mac, the Unity project, and a USB MIDI keyboard. The current relay does not support Windows or Linux.**
- Download the APK above and install it on Quest using the same Developer Hub steps as Test 1.
- [Download the project ZIP](https://github.com/adayang0046/Music-Education-Baseline-Testing-Demo-1/archive/refs/heads/main.zip) and extract it, or clone this repository. On GitHub, **Code → Download ZIP** does the same thing.
- Install **Unity Hub** and **Unity Editor 2022.3.62f2**. In Hub, add/open the extracted folder containing `Assets`, `Packages`, and `ProjectSettings`. Wait for Unity to finish importing and compiling.
- You do not need to rebuild the APK, switch to Android, or install Android Build Support just to run the Mac relay.
- Connect **MPK mini 3** to the Mac by USB; put Mac and Quest on the same Wi-Fi.
- In Unity, stay **out of Play Mode**; no particular scene needs to be open. Open **XR MIDI → MIDI → Open Mac MIDI Monitor**.
- Select **MPK mini 3** and click **Connect selected source**. Press keys to check that the note counters increase.
- In Quest’s **App Library → Unknown Sources**, open **XR MIDI Wi-Fi Lesson**. Enter the Quest's current Wi-Fi IP in the Mac monitor and click **Start Wi-Fi MIDI relay**. Find the address in the headset’s Wi-Fi connection details; do not use the Mac’s IP. Keep Unity and the monitor open throughout the test. The lesson runs on Quest, while Unity on the Mac forwards MIDI.
- From Ready, open your hand and pinch to start. Play MIDI pitches **60, 62, 64, 65, 67** as the notes reach the hit line.

**Success:** Quest shows **CONNECTED**, key presses/releases produce matching MIDI events on both devices, and well-timed correct notes receive positive feedback. Hand status changes between **OPEN**, **PINCH**, and **NOT TRACKED**. No sound is expected.
