# Quest lesson verification — 2026-09-28

## Automated results

- Full desktop-configured test copy: **68 passed, 0 failed, 0 skipped**. Includes
  the prior 56 tests, 11 gesture/dwell/simulation logic cases, and the new Quest
  lesson scene test. See `Verification/QuestLessonDesktopTests.xml`.
- After correcting the Editor camera offset and replacing the initial desktop
  keyboard hint, the Quest lesson scene was tested again with the **new input
  backend only**: **1 passed, 0 failed**. See `Verification/QuestLessonNewInputTests.xml`.
- The scene test covers automatic five-note completion, pause/resume, restart,
  mode-switch restrictions, real-provider wiring and fallback-button presence.
  Button actions are exercised programmatically; headset gaze aiming and real
  pinch delivery still require device acceptance testing.
- Inspected `Verification/QuestLessonPreview.png`: the complete panel, auto-note
  mode, note buttons, lesson controls and tracking label are visible and not mirrored.
  This is an Editor camera render, not a headset screenshot. The unfocused hint in
  the image reflects the batch Editor's focus state.

An exploratory full-suite run under the Quest new-input-only settings produced
66 passes and two failures: the legacy desktop scene's StandaloneInputModule read
UnityEngine.Input, then its interrupted Play Mode test affected the next scene test.
That desktop scene requires its original desktop input configuration. The isolated
Quest scene subsequently passed under new-input-only settings. The working project
retains its original settings; do not run every platform's scene with one global
input configuration and interpret those results as hardware compatibility.

## Remaining hardware acceptance

Follow `QuestLessonDemo.md` on the Quest. Verify pinch starts only Ready, holding
or losing a pinch cannot restart/score notes, head-gaze selection fires once until
looking away, the automatic trial completes, and pause/restart/focus recovery work.
The existing user's successful gesture diagnostic and laptop MIDI tests are separate
from acceptance of this integrated scene. Simulated perfect timing is not evidence
of physical MIDI timing or finger-to-key correctness.

## Android artifact

Build succeeded in the isolated copy. APK: `Builds/QuestLessonDemo.apk` (38167013 bytes).
SHA-256: `c23236966ea8a88c7cc050760d6e3686060bfe19bfaad6a45d2b4dfbcafc4bbe`.
AAPT confirmed package `com.xrmidi.questlesson`, ARM64, minimum API 32, target API 34,
and hand-tracking permission/feature. Metadata is in `Verification/QuestLessonApk.txt`.

## Device installation and startup

Installed the new package successfully on the connected Quest 3 and launched its
UnityPlayerActivity. Startup logs list Hand Tracking Subsystem and show OpenXR
reaching FOCUSED; no exception or fatal error appeared in the inspected app log.
The session subsequently lost focus and became idle. This is startup evidence,
not confirmation of successful pinch/gaze interaction or lesson completion.
See `Verification/QuestLessonStartup.txt`. The user must now perform the headset
procedure above; reopen XR MIDI Quest Lesson if the headset has returned home.

Final protected-file hash check found no changes to the original scenes, package
files or working-project settings. Runtime/scene sources match the build copy.
`git diff --check` passed. No commits were created.
