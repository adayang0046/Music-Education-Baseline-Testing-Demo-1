# Wi-Fi MIDI relay verification — 2026-09-28

## Executed

- Unity 2022.3.62f2 compiled the new network assembly, Mac monitor extension, Quest adapter, and scene/build helpers in an isolated project copy.
- **26/26 targeted Edit Mode tests passed** with the Quest build copy's new Input System backend: 13 relay protocol/loopback tests, 11 existing Quest lesson logic tests, the existing simulated Quest scene test, and the new live relay scene test. See `Verification/QuestMidiRelayTests.xml`.
- The relay scene test established a real local UDP connection, disabled simulation, started the shared lesson, received MIDI 60, and confirmed disconnect paused the lesson. It explicitly models application focus because batch-mode Unity has no focused Game window.
- The first scene-test run suppressed notes as expected without focus; the next run required an explicit expectation for the diagnostic log. The final test run passed after those test-harness corrections; focus protection in production code remains enabled.
- Rendered and inspected `Verification/QuestMidiRelayPreview.png`: connection, last-note, and hand-state text are above the lesson panel; original fallback buttons remain visible. This is an Editor rendering, not proof of headset readability or hand tracking.
- Verified hashes of 36 protected files: source-project settings, packages, and all pre-existing scenes/scene metadata match their pre-relay snapshots. `git diff --check` passed.
- The first Android artifact failed manifest inspection: synchronizing source XR assets after configuring the copy had reset its Quest features. It was not installed. `BuildCopy` now reapplies the isolated configuration immediately before building; the discarded artifact is preserved outside the project's normal build output.
- The corrected ARM64 IL2CPP APK passed manifest assertions for Internet permission, Quest hand tracking, VR head tracking, and the VR launch category. Installed successfully and launched on the connected Quest 3. OpenXR reached `XR_SESSION_STATE_FOCUSED`; no managed exception was observed in the captured app log. See `Verification/QuestMidiRelayApk.txt` for manifest details and SHA-256.
- The production sender received heartbeat acknowledgements from the actual Quest receiver at `192.168.4.198:45831`. A temporary 20-second Editor probe sent two synthetic note events and measured a 90.94 ms median / 2347.44 ms p95 application RTT. **Sleep/resume occurred during the probe, so these numbers are not representative playing latency.** The probe polled at 5 ms rather than using the monitor's Editor update loop. No MIDI callback log was captured; this establishes the Wi-Fi acknowledgement path, not successful physical playing. See `Verification/QuestMidiRelayDevice.txt`.

## Remaining manual checks

Physical keyboard-to-headset key feedback, accurate scoring while playing, hand-status readability/tracking while wearing the headset, disconnect/reconnect usability, and focus/sleep behavior require the manual acceptance steps in `QuestMidiRelay.md`. Automated loopback success does not establish these hardware results.

The existing simulated lesson, desktop MIDI, and gesture diagnostic remain available. No direct Quest USB MIDI, Bluetooth support, finger-to-key attribution, chord-start trigger, or keyboard alignment is claimed.

## Files involved

- Added `Assets/Scripts/MIDI/Network`: packet format, session validation, UDP sender/receiver, and independent assembly.
- Added `Assets/Scripts/XR/QuestMidiRelay.cs`, `Assets/Scenes/QuestMidiRelayDemo.unity`, and `Assets/Editor/XR/QuestMidiRelay{SceneBuilder,Build}.cs`.
- Extended `Assets/Editor/MIDI/MacMidiMonitor.cs` with optional relay controls/RTT diagnostics.
- Extended `RhythmDemoController`, `FallingNotePanel`, and `QuestLessonControls` to support an explicitly external MIDI mode and disable simulated notes in that mode. Existing scenes retain their defaults.
- Added relay tests and assembly references; shared the existing scene-preview capture helper with the new scene test.
- Added the relay setup guide, this verification record, test XML, and rendered preview under `Docs`.
