# MPK mini USB MIDI on Mac

## Scope

This first hardware-input path runs in the **macOS Unity Editor**. Connect the MPK
mini to the Mac using a USB data cable. No driver, DAW, Akai instrument software,
or additional Unity package was installed for this implementation.

The existing Quest APK does not receive these notes. A keyboard connected to the
Mac does not automatically send MIDI to a standalone Quest application. Direct
Quest USB/Bluetooth support or an explicit computer-to-Quest transport is future work.

This is MIDI input only: no piano sound synthesis is included.

## Test the keyboard

1. Connect the MPK mini to the Mac and let Unity finish importing scripts.
2. Open **XR MIDI > MIDI > Open Mac MIDI Monitor**.
3. Select **MPK mini 3** and click **Connect selected source**. If absent, check the
   cable and click **Refresh devices**. The monitor also refreshes its list every second.
4. Press and release several keys. Expect an ON line on press and OFF on release,
   with pitch, velocity, and channel. Harder presses should normally change velocity.
5. Hold a chord and release it: each key should have its own events. A MIDI note-on
   with velocity zero is normalized to OFF.
6. Unplug the device: expect a disconnected status. Reconnect and explicitly select
   Connect again. This monitor never silently switches to a different input device.

The displayed channel is 1–16; the domain model stores 0–15. Use numeric MIDI pitch
for comparison because octave labels vary among keyboard manufacturers and software.
Pads also emit notes and may therefore appear in this monitor. Knobs, sustain,
pitch bend, and other non-note messages are intentionally not evaluated.

## Use the five-note lesson

1. Open `Assets/Scenes/Milestone1Demo.unity` and enter Play Mode.
2. Reconnect the source in the MIDI monitor if Play Mode's domain reload closed it.
3. Click **Connect to active lesson**. This finds the active demo controller and
   enables forwarding; manual target assignment remains available.
4. Click **Start / Resume lesson**. Check that routing says **Playing — receiving
   MIDI** and that **Events forwarded to lesson** increases when you press keys.
   Clicking another window can pause the demo; use Start / Resume again if needed.
5. Play MIDI pitches **60, 62, 64, 65, 67** as the notes reach the hit line. Use the
   MPK mini's octave buttons to reach this range; the monitor shows the actual pitch.
6. Confirm feedback and completion, then test Pause and Restart.

The monitor does not start lessons automatically. Forwarding is reset at Play Mode
transitions. The controller ignores hardware input while inactive, paused, ready,
or complete. Simulated keyboard/UI input remains available.

## Architecture and timing

- `Tools/Midi/MacMidi.c`: project-owned CoreMIDI bridge. Native callbacks copy bytes
  into a mutex-protected bounded buffer; no callback enters Unity or managed code.
- `Assets/Plugins/macOS/XRMidiMac.dylib`: universal arm64/x86_64 bridge, imported for
  the macOS Editor only and excluded from player builds, including Android.
- `Assets/Scripts/Rhythm/Core/MidiByteParser.cs`: pure C# MIDI 1.0 stream decoder.
  Handles fragmented messages, running status, realtime bytes, and system-message
  boundaries; consumes non-note messages without interpreting them as notes.
- `Assets/Editor/MIDI/MacMidiInput.cs`: replaceable `IMidiInput` adapter. Polls on
  the main thread, emits normalized `NoteEvent` values, closes CoreMIDI resources
  on disposal, and resets the parser on disconnect or buffer overflow.
- `Assets/Editor/MIDI/MacMidiMonitor.cs`: Editor diagnostics and explicit opt-in
  forwarding to the selected demo controller. Closing the window stops MIDI input.
- `RhythmDemoController.ReceivePhysicalMidi`: stamps forwarded notes using the
  existing lesson clock and uses the existing input queue/evaluator. Musical
  evaluation remains outside the monitor. The existing simulated input emitter's
  Send method now also accepts an optional channel without changing existing calls.

Monitor timestamps use Editor uptime at polling. Forwarded lesson notes use lesson
elapsed time at receipt. **Native event timestamps are not mapped yet**, so Editor
polling adds latency and jitter and batches may share timestamps. This is a functional
input test, not a research-quality onset latency measurement. A blocked Editor can
also defer events; restart a trial after a stall rather than interpreting its score.
The queue reports overflow and resets parsing; it does not synthesize missing note-offs.
No duration scoring or held-note sound engine is present.

## Rebuild and tests

The checked-in native binary is built from repository source with Apple SDKs:

```sh
sh Tools/Midi/build-macos.sh
```

Quit Unity before replacing an already loaded native library, then reopen it.
No package download is required; rebuilding requires Xcode command-line tools.

`Tools/Midi/native-smoke.c` creates a temporary virtual CoreMIDI source, sends a
known note-on/off sequence, checks received bytes, and disposes resources.
`Tools/Midi/listen-macos.c` listens to an explicitly named physical source for 30
seconds. Build either probe with `MacMidi.c`, CoreMIDI and CoreFoundation frameworks.
Neither probe installs a permanent virtual MIDI device.

## Verification — 2026-09-28

- macOS CoreMIDI enumerated the connected keyboard as **MPK mini 3**.
- Native virtual-source smoke test: **passed**, including byte receipt and disposal.
- Unity 2022.3.62f2, isolated project copy: **56 tests passed, 0 failed, 0 skipped**.
  Includes the prior 48 tests, seven MIDI parser/channel tests, and a native-adapter
  load/enumerate/poll/dispose test. Results: `Verification/MacMidiTests.xml`.
- Both arm64 and x86_64 binary slices compiled. Runtime testing was on this Apple
  Silicon Mac; Intel runtime execution was not tested.
- Physical capture opened the MPK mini but received **0 bytes during its 30-second
  window**. This does not establish a fault or successful note input. Press/release
  and actual lesson forwarding require the manual checks above.
- `git diff --check`: passed.
- Existing package versions, scenes and project settings were not changed by this
  MIDI work. No Quest MIDI support or full hardware lesson completion is claimed.

API reference: [Apple CoreMIDI MIDIInputPortCreate](https://developer.apple.com/documentation/coremidi/midiinputportcreate(_:_:_:_:_:)).
Keyboard reference: [Akai MPK mini mk3 FAQ](https://support.akaipro.com/en/support/solutions/articles/69000798861-akai-pro-mpk-mini-mk3-frequently-asked-questions).

## Unity window and routing troubleshooting

- Seeing ON/OFF in the monitor proves input reached the monitor, not that a lesson
  is connected. Forwarding resets on Play Mode transitions. Connect to active
  lesson after entering Play Mode, then Start / Resume; inspect routing status.
- The demo evaluates notes during the lesson. It does not make sound or illuminate
  a virtual keyboard whenever an idle key is pressed.
- Stop Play Mode with Cmd+P. To unmaximize a tab, hover over it and press Shift+Space,
  or toggle Maximize from the tab menu. Choose Play Focused rather than Play
  Maximized in the Game toolbar before entering Play Mode.
- A Scene view is an editing camera; the Game view shows the demo camera. Backwards
  text, stereo side-by-side output, and a maximized window are different problems.
  No XR configuration was changed without identifying which of these is occurring.

The user has confirmed physical key events appear in the MIDI monitor. Full
physical-keyboard lesson completion still requires manual confirmation.

Routing follow-up: all 56 tests passed again after adding the one-click lesson
connection and status display. The scene smoke test now checks that physical-input
entry rejects notes in Ready/Paused and accepts note-on/off during Playing. This
verifies software routing; the user's actual keyboard-to-lesson trial remains pending.
