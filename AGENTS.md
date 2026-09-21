# XR MIDI Learning Assistant — Development Instructions

## Project overview

This project is an adaptive mixed-reality piano learning application developed using Unity, Meta Quest 3, and a physical MIDI keyboard.

The application teaches beginner piano skills through structured exercises, real-time MIDI input, XR visualization, performance evaluation, feedback, and personalized exercise recommendations.

This is a master's research project. Maintainability, reproducibility, modularity, and clear documentation are important.

## Current development stage

We are developing the first functional demo.

The first milestone is a working lesson engine that supports:

* Loading a structured lesson.
* Displaying the expected note.
* Receiving simulated MIDI input.
* Evaluating pitch accuracy.
* Advancing through lesson steps.
* Reporting lesson completion.

Do not implement advanced recommendation algorithms or unnecessary XR features during this milestone.

## Architecture

Use modular, interface-driven architecture.

Separate:

1. Domain models and interfaces.
2. Lesson management.
3. MIDI input.
4. Performance evaluation.
5. Feedback.
6. Learner progress.
7. Recommendation.
8. XR presentation and calibration.

The domain layer must not depend on UnityEngine.

Do not place musical evaluation logic inside UI components.

Do not hardcode lessons into MonoBehaviours.

Keep hardware-specific input implementations replaceable.

Avoid global singletons and unnecessary frameworks.

## Unity-specific rules

* Preserve the existing Unity version and package configuration.
* Preserve existing scenes, prefabs, and XR setup.
* Do not regenerate the project from scratch.
* Do not edit generated Unity directories.
* Preserve Unity asset GUIDs and metadata.
* Prefer moving and renaming Unity assets through Unity Editor.
* Avoid directly editing scene and prefab YAML unless necessary.
* Do not install external dependencies without approval.

## Coding conventions

* Use C# and clear descriptive names.
* Prefer simple, testable classes.
* Use interfaces at boundaries where implementations may change.
* Avoid speculative abstractions.
* Document non-obvious behavior.
* Keep classes focused on a single responsibility.

## Testing requirements

* Add Edit Mode tests for pure lesson logic.
* Test both valid and invalid input.
* Test lesson completion and restart behavior.
* Distinguish actual test results from unexecuted tests.
* Do not claim Quest 3 or MIDI hardware compatibility without device testing.

## Working rules

Before implementation:

1. Inspect relevant existing files.
2. Explain the proposed modifications.
3. Identify possible breaking changes.

During implementation:

1. Make small, focused changes.
2. Do not modify unrelated files.
3. Preserve existing functionality.
4. Do not create Git commits unless requested.

After implementation:

1. Summarize modified files.
2. Report tests performed and results.
3. Identify anything requiring Unity Editor verification.
4. Provide manual setup steps.
5. Report remaining issues.

Never delete or overwrite existing project work without explicit approval.
