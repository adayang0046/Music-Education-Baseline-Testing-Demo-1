# Quest 3 hand and pinch test

This is a separate hardware diagnostic, not piano finger-correctness evaluation.
The existing lesson scenes and package versions are preserved.

## Installed stack inspected

- Unity 2022.3.62f2
- OpenXR 1.17.1, XR Hands 1.8.1, XR Plug-in Management 4.6.1
- Input System 1.19.0, XR Interaction Toolkit 3.5.0
- Unity Meta OpenXR 1.0.4, AR Foundation 5.2.2

The installed package manifests declare Unity 2022.3 compatibility. This test uses
the XR Hands subsystem directly and does not need Interaction Toolkit samples,
Meta's All-in-One SDK, passthrough, or a MIDI device.

## Scene and implementation

Open `Assets/Scenes/QuestGestureTest.unity`. If absent, save your current scene,
then use **XR MIDI > Quest > Create Gesture Test Scene**. Existing scenes are never
overwritten by this command.

The scene contains:

- An XR Origin with a tracked stereo camera; device-relative origin plus 1.4 m
  camera offset. Camera and hand joints share the same tracking-space transform.
- `QuestHandGestureProvider`: discovers/reconnects to a running XRHandSubsystem,
  copies valid joint poses on Dynamic updates, and implements `IGestureProvider`.
- `QuestGestureDiagnostics`: displays 26 joint markers per hand, per-hand tracking
  status, thumb/index distance, and start/end/cancellation counters.
- A world-space status panel in front of the initial forward direction.

`PinchGestureState` is pure C# in the existing Unity-independent core assembly.
It recognizes thumb/index proximity, with press <= 25 mm and release >= 40 mm.
These Inspector-configurable defaults use hysteresis; they are provisional, not
research-validated thresholds. This is our own joint-distance detector, not Meta
Aim's semantic pinch signal. Meta Hand Tracking Aim is therefore optional here.

On first acquisition or after a tracking interruption, open the hand beyond the
release threshold before pinching. A held pinch emits one start. Release emits
one end. Tracking loss, invalid thumb/index data, focus loss, suspend, or stale
samples (>250 ms by default) cancel an active pinch once. No fabricated hand data
or automatic simulation fallback is used in this scene.

The package's tracking/pose validity flags indicate usable data, not proof that
fingertips are directly observed: the runtime may estimate occluded joints.

## Android configuration for a project copy

The automated APK build uses a **disposable copy** so the working piano project's
input backend and other project settings remain unchanged. Keep that separation
when reproducing this hardware test: enabling Input System-only globally can
affect the original demo's legacy keyboard input and StandaloneInputModule.

In a copy of the project:

1. Select Android in **File > Build Settings** and switch platform.
2. Under **Project Settings > XR Plug-in Management > Android**, enable **OpenXR**
   and **Initialize XR on Startup**. Do not enable the Mac Standalone loader to
   simulate Quest tracking; it cannot provide the headset's hand data here.
3. Under **XR Plug-in Management > OpenXR > Android**, enable **Meta Quest Support**
   and **Hand Tracking Subsystem**. In XR Hands 1.8.1 ensure automatic subsystem
   initialization is enabled. Meta Hand Tracking Aim is not required by this test.
4. Under Android Player settings, select **IL2CPP**, **ARM64**, minimum API **32**,
   target API **34** (installed here; for this sideload diagnostic), and **Input System Package (New)**.
   Use **Vulkan** and OpenXR **Single Pass Instanced** rendering. Restart the Editor
   if requested by the input backend change.
5. Run the Android **OpenXR Project Validation** checks and address errors. Keep
   package versions unchanged. The installed XR Hands build hook adds the hand
   tracking permission/feature when both Meta Quest Support and Hand Tracking
   Subsystem are enabled; do not add a competing handwritten Android manifest.
6. Include only `QuestGestureTest.unity` in this test build. Build an APK with the
   identifier `com.xrmidi.gesturetest`; this is separate from the piano application.
7. Install through Meta Quest Developer Hub, or ADB, and launch from Unknown Sources.

For scripted reproduction, create a temporary project copy containing Assets,
Packages and ProjectSettings, and add a file named `QUEST_GESTURE_BUILD_COPY` in
that copy's root. Use Unity 2022.3.62f2 batch entry points in separate invocations:

- `-executeMethod QuestGestureBuild.ConfigureCopy -questGestureBuildCopy -quit`
- `-buildTarget Android -executeMethod QuestGestureBuild.BuildCopy -questGestureBuildCopy -quit`

Supply `-projectPath <copy>` and `-logFile <path>` in both. The build is written to
`<copy>/Builds/QuestGestureTest.apk`. The helper refuses to run outside batch mode
or without the explicit marker/flag; it changes settings only in the copy supplied.

Android modules can be located at the editor installation's sibling
`PlaybackEngines/AndroidPlayer` directory rather than inside `Unity.app/Contents`.
Use the SDK/NDK/JDK belonging to 2022.3.62f2.

## Headset procedure

The inspected development APK is at `Builds/QuestGestureTest.apk`. Install it
through Meta Quest Developer Hub. This APK already contains the isolated Android
configuration; using it does not require changing the working project's settings.

1. Enable hand tracking in Quest settings. Use a well-lit room and put controllers
   aside so the runtime can switch to hands. Enable Developer Mode and accept USB
   debugging for this computer.
2. Launch **XR MIDI Gesture Test**. Face the initial forward direction. Expect a
   dark environment, status panel, cyan left joints and green right joints.
   This test intentionally has no passthrough.
3. Confirm **XR Hands subsystem running**, then **TRACKED** for each hand. These
   are separate checks: a running subsystem does not prove hands are tracked.
4. Open the left hand, then touch thumb and index together. Expect yellow markers
   and the left start count increasing by exactly one. Hold for three seconds:
   the count should not increase further.
5. Open the hand: the end count increases once and markers turn cyan. Repeat on
   the right; the left counters should not change.
6. Pinch both hands. Move one hand out of view: its active gesture should cancel
   when tracking/usable tips are lost; the other hand should remain independent.
7. Bring the hand back while still pinching: no new start until you open and pinch
   again. Test taking the headset off/returning and opening/closing the system menu.
8. Repeat at different comfortable distances and near the keyboard. Record false
   starts, lost joints, and cases where the runtime estimates hidden fingers.

Log lines use `[QuestGesture] Left/Right pinch START`, `END`, or `CANCEL`. Record
the headset OS version, package versions, distance thresholds, and observations
when reporting results. A successful build or automated test is not hardware proof.

## Troubleshooting

- **No ADB device:** connect a data-capable USB cable, wake the headset, accept its
  debugging prompt, and check `adb devices -l`. `unauthorized` requires approval
  inside the headset. An empty list means no device is currently available to ADB.
- **No running subsystem:** check Android OpenXR loader, Meta Quest Support, Hand
  Tracking Subsystem, and initialization. A Mac Editor run normally has no hands.
- **Subsystem running, no hands:** check headset hand-tracking mode, controller
  switching, lighting, app focus and hand visibility.
- **Tracked but no pinch:** first open the hand; inspect the displayed thumb/index
  distance and whether both tips are available. Adjust thresholds only after
  measuring real data, and keep release greater than press.
- **No panel in headset:** face/recenter toward the application's initial forward
  direction. Verify camera tracking and the shared XR Origin offset.

## Files added

- `Assets/Scripts/Rhythm/Core/PinchGestureState.cs`
- `Assets/Scripts/XR/XRMidi.XR.asmdef`
- `Assets/Scripts/XR/QuestHandGestureProvider.cs`
- `Assets/Scripts/XR/QuestGestureDiagnostics.cs`
- `Assets/Editor/XR/XRMidi.XR.Editor.asmdef`
- `Assets/Editor/XR/QuestGestureSceneBuilder.cs`
- `Assets/Editor/XR/QuestGestureBuild.cs`
- `Assets/Tests/EditMode/PinchGestureTests.cs`
- `Assets/Tests/XR/XRMidi.XR.Tests.asmdef` and `QuestGestureSceneTests.cs`
- `Assets/Scenes/QuestGestureTest.unity` and `Assets/QuestGestureJoint.mat`
- Accompanying Unity metadata and this documentation.

See `QuestGestureVerification.md` for executed tests/build results and remaining
headset verification. No piano finger assignment or MIDI changes are included.
