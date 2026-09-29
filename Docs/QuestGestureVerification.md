# Quest gesture verification

Inspected on 2026-09-22 using Unity 2022.3.62f2.

## Requirement status

| Requirement | Evidence / status |
| --- | --- |
| Separate Quest hand-tracking diagnostic | Implemented in `Assets/Scenes/QuestGestureTest.unity`; existing scenes preserved. |
| Real XR Hands joint data and independent hands | Provider reads joint poses; diagnostics show 26 markers per hand. Device behavior pending. |
| Pinch start, hold and release | Pure C# hysteresis state machine; automated edge/count tests passed. |
| Tracking loss and reacquisition | State cancellation and open-before-rearm tests passed. Provider handles invalid tips, stale samples, focus, suspend and disable; these device lifecycle paths still require headset testing. |
| Separation from lesson logic | Hardware provider implements `IGestureProvider`; diagnostics contain no scoring logic. |
| Android artifact | Development APK built successfully in an isolated project copy. |
| Quest 3 execution | Not verified: ADB returned an empty device list during this inspection. |
| Correct piano finger / physical key contact | Not implemented by this diagnostic. Requires keyboard calibration, fingertip-to-key association, and later MIDI note timing correlation. |

## Executed verification

- Existing Unity test report: **48 passed, 0 failed, 0 skipped**. Includes the lesson tests, pinch state tests, and scene Play Mode smoke test without a hardware provider. See [test XML](Verification/QuestGestureTests.xml).
- Compared working Assets and Packages files against the tested/built copy: all corresponding files match. Tests were not rerun during this inspection because executable sources have not changed.
- Previous Android build completed successfully; inspected its resulting APK during this review.
- APK: `Builds/QuestGestureTest.apk` (38,157,539 bytes), ignored by Git.
- SHA-256: `8e80af4c10c021e398f6cc66aeabb4cf123800933b7d599748a805929bafd36c`.
- AAPT confirms application ID `com.xrmidi.gesturetest`, ARM64 ABI, minimum API 32, target API 34, hand-tracking permission and feature, VR head-tracking feature, and supported-device metadata containing `eureka`.
- [APK metadata](Verification/QuestGestureApk.txt) retained for reproducibility.
- All 26 package/settings files in the pre-implementation protection snapshot remain byte-identical. XR Android settings were applied only to the disposable build copy, not the working project.

The build was successful but not warning-free: OpenXR requested a controller interaction profile and optionally prioritizing input polling. This diagnostic reads hand joints directly and has no controller interaction. Package tooling also logged code-coverage assembly-resolution and sample-cache warnings. None prevented the APK from building; headset launch and tracking remain unverified.

## Remaining acceptance checks

Follow [the headset procedure](QuestGestureTest.md#headset-procedure) after installing the APK through Meta Quest Developer Hub:

1. Head pose updates and both hands display correctly aligned joint markers.
2. Open then pinch each hand: exactly one start while held, one end on release.
3. Both hands work independently; losing usable tracking during a pinch cancels once.
4. Returning while pinching produces no start until opening and pinching again.
5. System menu, headset removal, and resume do not leave a stuck pinch or stale markers.
6. Record device OS, lighting, thresholds and observed failures, especially near a keyboard.

No headset compatibility, key-contact accuracy, finger-correctness evaluation, or physical MIDI functionality is claimed from automated tests or build success.

## Subsequent headset check

The Quest 3 was later connected, the diagnostic APK installed and launched, and
OpenXR reached the focused state. The user confirmed the panel and hand markers
were visible and that both hands passed pinch/hold/release, tracking-loss
cancellation, and open-before-rearm checks. These are user-reported manual results,
not automated measurements of tracking accuracy or piano finger correctness.
