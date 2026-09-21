using System;
using System.Collections.Generic;

[Serializable]
public class LessonData
{
    public string lessonId;
    public string lessonName;
    public string description;
    public float tempo;
    public string difficulty;

    public List<TargetNote> targetNotes = new List<TargetNote>();

    public bool IsValid(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(lessonId))
        {
            errorMessage = "Lesson ID is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(lessonName))
        {
            errorMessage = "Lesson name is missing.";
            return false;
        }

        if (tempo <= 0)
        {
            errorMessage = "Tempo must be greater than zero.";
            return false;
        }

        if (targetNotes == null || targetNotes.Count == 0)
        {
            errorMessage = "The lesson has no target notes.";
            return false;
        }

        for (int i = 0; i < targetNotes.Count; i++)
        {
            TargetNote note = targetNotes[i];

            if (note.midiNoteNumber < 0 || note.midiNoteNumber > 127)
            {
                errorMessage =
                    $"Note {i + 1} has an invalid MIDI note number.";

                return false;
            }

            if (note.expectedStartTime < 0)
            {
                errorMessage =
                    $"Note {i + 1} has a negative start time.";

                return false;
            }

            if (note.expectedDuration <= 0)
            {
                errorMessage =
                    $"Note {i + 1} must have a positive duration.";

                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}

[Serializable]
public class TargetNote
{
    public int midiNoteNumber;
    public float expectedStartTime;
    public float expectedDuration;
}