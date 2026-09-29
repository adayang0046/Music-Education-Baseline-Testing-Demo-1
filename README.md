# XR MIDI Piano Learning Demo

A Unity mixed-reality piano learning prototype for Meta Quest 3, with hand tracking, a five-note lesson, and optional USB MIDI input relayed from a Mac over Wi-Fi.

- [Three demo tests: instructions and success conditions](READEME.md)
- [Mac MIDI setup](Docs/MacMidiSetup.md)
- [Quest Wi-Fi MIDI setup](Docs/QuestMidiRelay.md)
- [Development instructions](AGENTS.md)

## Open the project

Use **Unity 2022.3.62f2**. Add this repository's root folder in Unity Hub and open it. Unity regenerates its local cache and restores the packages listed in `Packages/manifest.json`.

The current USB MIDI adapter requires **macOS**. The gesture and simulated lesson APKs run on Quest without a computer during use; the Wi-Fi MIDI lesson also requires the Mac relay, a USB MIDI keyboard, and a shared local network. Piano audio and finger-to-key identification are not implemented yet.

## Builds

APKs and Unity-generated caches are excluded from Git. The test guide's `Builds/` links refer to local APKs, which must be distributed separately. Cloning this repository does not download them.

For headset build instructions, see the [gesture guide](Docs/QuestGestureTest.md), [simulated lesson guide](Docs/QuestLessonDemo.md), and [MIDI relay guide](Docs/QuestMidiRelay.md). Use a disposable project copy as described there to preserve the working project's settings.
