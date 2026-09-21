using UnityEngine;

public class LessonManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private LessonLoader lessonLoader;

    public int CurrentNoteIndex { get; private set; }

    public bool IsLessonActive { get; private set; }

    public bool IsLessonComplete { get; private set; }

    public TargetNote CurrentTargetNote
    {
        get
        {
            if (lessonLoader == null ||
                lessonLoader.CurrentLesson == null ||
                lessonLoader.CurrentLesson.targetNotes == null ||
                CurrentNoteIndex < 0 ||
                CurrentNoteIndex >=
                lessonLoader.CurrentLesson.targetNotes.Count)
            {
                return null;
            }

            return lessonLoader.CurrentLesson
                .targetNotes[CurrentNoteIndex];
        }
    }

    private void Start()
    {
        if (lessonLoader == null)
        {
            Debug.LogError(
                "LessonManager: LessonLoader has not been assigned."
            );

            return;
        }

        if (lessonLoader.CurrentLesson == null)
        {
            Debug.LogError(
                "LessonManager: No lesson has been loaded."
            );

            return;
        }

        StartLesson();
    }

    public void StartLesson()
    {
        if (lessonLoader == null ||
            lessonLoader.CurrentLesson == null)
        {
            Debug.LogError(
                "LessonManager: Cannot start because no lesson is loaded."
            );

            return;
        }

        CurrentNoteIndex = 0;
        IsLessonActive = true;
        IsLessonComplete = false;

        Debug.Log(
            $"Lesson started: " +
            $"{lessonLoader.CurrentLesson.lessonName}"
        );

        PrintCurrentTarget();
    }

    public bool SubmitPlayedNote(int midiNoteNumber)
    {
        if (!IsLessonActive)
        {
            Debug.LogWarning(
                "LessonManager: No lesson is currently active."
            );

            return false;
        }

        TargetNote targetNote = CurrentTargetNote;

        if (targetNote == null)
        {
            Debug.LogError(
                "LessonManager: The current target note is missing."
            );

            return false;
        }

        bool isCorrect =
            midiNoteNumber == targetNote.midiNoteNumber;

        if (isCorrect)
        {
            Debug.Log(
                $"Correct: {GetMidiNoteName(midiNoteNumber)} " +
                $"({midiNoteNumber})"
            );

            AdvanceToNextNote();
        }
        else
        {
            Debug.Log(
                $"Incorrect: played " +
                $"{GetMidiNoteName(midiNoteNumber)} " +
                $"({midiNoteNumber}), expected " +
                $"{GetMidiNoteName(targetNote.midiNoteNumber)} " +
                $"({targetNote.midiNoteNumber})"
            );
        }

        return isCorrect;
    }

    private void AdvanceToNextNote()
    {
        CurrentNoteIndex++;

        int noteCount =
            lessonLoader.CurrentLesson.targetNotes.Count;

        if (CurrentNoteIndex >= noteCount)
        {
            CompleteLesson();
            return;
        }

        PrintCurrentTarget();
    }

    private void CompleteLesson()
    {
        IsLessonActive = false;
        IsLessonComplete = true;

        Debug.Log(
            $"Lesson completed: " +
            $"{lessonLoader.CurrentLesson.lessonName}"
        );
    }

    private void PrintCurrentTarget()
    {
        TargetNote targetNote = CurrentTargetNote;

        if (targetNote == null)
        {
            return;
        }

        int displayedIndex = CurrentNoteIndex + 1;
        int totalNotes =
            lessonLoader.CurrentLesson.targetNotes.Count;

        Debug.Log(
            $"Target {displayedIndex}/{totalNotes}: Play " +
            $"{GetMidiNoteName(targetNote.midiNoteNumber)} " +
            $"({targetNote.midiNoteNumber})"
        );
    }

    private string GetMidiNoteName(int midiNoteNumber)
    {
        string[] noteNames =
        {
            "C", "C#", "D", "D#", "E", "F",
            "F#", "G", "G#", "A", "A#", "B"
        };

        int noteIndex = midiNoteNumber % 12;
        int octave = midiNoteNumber / 12 - 1;

        return noteNames[noteIndex] + octave;
    }
}