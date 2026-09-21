using UnityEngine;

public class LessonDebugTester : MonoBehaviour
{
    [SerializeField]
    private LessonManager lessonManager;

    private void OnGUI()
    {
        if (lessonManager == null)
        {
            return;
        }

        GUILayout.BeginArea(
            new Rect(20, 20, 240, 300),
            "Lesson Debug",
            GUI.skin.window
        );

        GUILayout.Label(
            "Click notes to simulate MIDI input."
        );

        if (lessonManager.IsLessonComplete)
        {
            GUILayout.Label("Lesson complete!");
        }
        else if (lessonManager.CurrentTargetNote != null)
        {
            GUILayout.Label(
                "Current target MIDI note: " +
                lessonManager.CurrentTargetNote.midiNoteNumber
            );
        }

        if (GUILayout.Button("Play C4 — MIDI 60"))
        {
            lessonManager.SubmitPlayedNote(60);
        }

        if (GUILayout.Button("Play D4 — MIDI 62"))
        {
            lessonManager.SubmitPlayedNote(62);
        }

        if (GUILayout.Button("Play E4 — MIDI 64"))
        {
            lessonManager.SubmitPlayedNote(64);
        }

        if (GUILayout.Button("Play F4 — MIDI 65"))
        {
            lessonManager.SubmitPlayedNote(65);
        }

        if (GUILayout.Button("Play G4 — MIDI 67"))
        {
            lessonManager.SubmitPlayedNote(67);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Restart Lesson"))
        {
            lessonManager.StartLesson();
        }

        GUILayout.EndArea();
    }
}