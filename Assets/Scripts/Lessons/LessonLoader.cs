using System;
using UnityEngine;

public class LessonLoader : MonoBehaviour
{
    [Header("Lesson JSON")]
    [SerializeField]
    private TextAsset lessonJsonFile;

    public LessonData CurrentLesson { get; private set; }

    private void Awake()
    {
        LoadLesson();
    }

    public bool LoadLesson()
    {
        if (lessonJsonFile == null)
        {
            Debug.LogError(
                "LessonLoader: No JSON lesson file is assigned."
            );

            return false;
        }

        try
        {
            LessonData loadedLesson =
                JsonUtility.FromJson<LessonData>(lessonJsonFile.text);

            if (loadedLesson == null)
            {
                Debug.LogError(
                    "LessonLoader: The JSON could not be converted into a lesson."
                );

                return false;
            }

            if (!loadedLesson.IsValid(out string errorMessage))
            {
                Debug.LogError(
                    $"LessonLoader: Invalid lesson. {errorMessage}"
                );

                return false;
            }

            CurrentLesson = loadedLesson;
            PrintLesson();

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"LessonLoader: Failed to load the lesson. " +
                exception.Message
            );

            return false;
        }
    }

    private void PrintLesson()
    {
        Debug.Log(
            $"Loaded lesson: {CurrentLesson.lessonName}\n" +
            $"ID: {CurrentLesson.lessonId}\n" +
            $"Difficulty: {CurrentLesson.difficulty}\n" +
            $"Tempo: {CurrentLesson.tempo} BPM\n" +
            $"Notes: {CurrentLesson.targetNotes.Count}"
        );

        for (int i = 0; i < CurrentLesson.targetNotes.Count; i++)
        {
            TargetNote note = CurrentLesson.targetNotes[i];

            Debug.Log(
                $"Target {i + 1}: " +
                $"MIDI {note.midiNoteNumber}, " +
                $"start {note.expectedStartTime:F1}s, " +
                $"duration {note.expectedDuration:F1}s"
            );
        }
    }
}