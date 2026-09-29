# Quest Wi-Fi MIDI lesson demo

This demo keeps MPK mini 3 connected to the Mac over USB. Unity's Mac MIDI Monitor forwards note events over Wi-Fi to the standalone Quest app. The Quest USB cable can stay connected to the Mac for installation/debugging. No additional Unity packages are needed.

## Run

1. Put Mac and Quest on the same local Wi-Fi network. Guest networks may block communication between devices.
2. Launch **XR MIDI Wi-Fi Lesson** on Quest (package `com.xrmidi.questrelay`). It uses `Assets/Scenes/QuestMidiRelayDemo.unity`. The previous gesture and simulated lesson apps remain separate.
3. Leave the Mac Unity Editor out of Play Mode. Open **XR MIDI > MIDI > Open Mac MIDI Monitor**. Select **MPK mini 3**, then **Connect selected source**.
4. Enter the Quest's Wi-Fi IPv4 address and click **Start Wi-Fi MIDI relay**. The observed address during setup was `192.168.4.198`; it can change. Find it in the headset's Wi-Fi details or with `adb shell ip -4 addr show wlan0`.
5. Keep the monitor open. It should report **Quest acknowledged the relay**; the headset should show **CONNECTED**. Press a key and check that the headset's `ON/OFF MIDI` pitch matches the Mac monitor. This works before starting the lesson.
6. Pinch to start from Ready, or use the existing head-gaze Start control. Play pitches **60, 62, 64, 65, 67** at the note targets. Adjust the MPK octave setting if necessary. Simulated note buttons and automatic notes are disabled in this scene.
7. Check the larger hand status above the panel: each hand reports **OPEN**, **PINCH**, or **NOT TRACKED**. This is pinch/tracking status, not detection of which finger struck a piano key.

The Mac monitor's desktop lesson controls are independent of the Quest relay. You do not need **Connect to active lesson** or Mac Play Mode for the headset. Entering/exiting Play Mode, closing the monitor, changing the source, or unplugging the keyboard stops the relay. Restart the relay after those actions.

## Timing and failure behavior

- UDP port **45831** carries versioned note/heartbeat packets. The receiver binds to one sender stream and rejects duplicate, reordered, malformed, and competing-stream packets.
- A sequence gap, disconnect, two-second timeout, or headset focus loss pauses an active lesson. Reconnect and explicitly Resume or Restart. A restored connection never resumes the lesson automatically.
- MIDI receipt is stamped against the Quest lesson clock. Sender uptime is diagnostic data, not a synchronized lesson clock. Mac Editor polling, Wi-Fi jitter, and Quest frame timing affect scoring.
- The monitor shows median/p95 **application round-trip time**, from the latest 64 heartbeat acknowledgements. This is not measured key-to-screen delay and should not be divided by two and reported as measured one-way latency.
- There is no piano synthesizer, audio-latency measurement, direct Quest USB MIDI, or Bluetooth MIDI in this demo. The local-network protocol has no authentication/encryption; it is a development transport, not a production networking service.
- No chord-start trigger, per-finger piano attribution, passthrough keyboard calibration, or aligned note lanes are added here.

## Architecture and build

`MacMidiInput → UdpMidiSender → Wi-Fi → UdpMidiReceiver → QuestMidiRelay → RhythmDemoController → existing lesson pipeline`.

`Assets/Scripts/MIDI/Network` contains a Unity-independent assembly for protocol, connection state, and sender/receiver. `QuestMidiRelay` adapts the receiver to the shared lesson controller and status display. Lesson evaluation remains in the existing domain classes.

The new scene was created through Unity's AssetDatabase/scene API. `QuestMidiRelayBuild.ConfigureCopy` and `BuildCopy` require a disposable project copy with `QUEST_GESTURE_BUILD_COPY` and the `-questGestureBuildCopy` argument. Only that copy enables Android Internet permission and the separate app identifier. Source-project package configuration, project settings, and existing scenes are preserved.

## Manual acceptance

- Both displays agree on physical key pitch, velocity, channel, and release.
- Playing the five expected pitches at the target times updates real lesson feedback; no automatic successes occur.
- Disconnecting/stopping the sender pauses an active trial; reconnecting requires an explicit resume/restart.
- Putting the headset to sleep and resuming does not score queued keys.
- Left/right pinch status follows the hands and shows tracking loss when unavailable.
- Record RTT while wearing the headset and playing, then assess visible response. Research-grade timing needs separate clock/latency calibration.

See `QuestMidiRelayVerification.md` for executed checks and remaining device verification.
