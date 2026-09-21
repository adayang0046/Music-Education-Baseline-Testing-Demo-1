using System;
using System.Collections.Generic;

namespace XRMidi
{
    // Field names are the version-1 JSON contract. Legacy LessonData remains unchanged.
    [Serializable]
    public sealed class Lesson
    {
        public string id;
        public string title;
        public double tempo;
        public int beatsPerBar;
        public int beatUnit;
        public string difficulty;
        public List<LessonNote> notes;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Lesson ID and title are required.");
            if (!Numbers.Finite(tempo) || tempo <= 0)
                throw new ArgumentException("Tempo must be finite and positive.");
            if (beatsPerBar <= 0 || beatUnit <= 0 || (beatUnit & (beatUnit - 1)) != 0)
                throw new ArgumentException("Time signature requires a positive numerator and power-of-two denominator.");
            if (notes == null || notes.Count == 0)
                throw new ArgumentException("A lesson must contain notes.");
            double previous = -1;
            foreach (var note in notes)
            {
                if (note == null || note.pitch < 0 || note.pitch > 127 ||
                    !Numbers.Finite(note.startBeat) || note.startBeat < previous || note.startBeat < 0 ||
                    !Numbers.Finite(note.durationBeats) || note.durationBeats <= 0)
                    throw new ArgumentException("Notes need valid pitches, nonnegative ordered beats, and positive finite durations.");
                if (!Numbers.Finite((note.startBeat + note.durationBeats) * 60.0 / tempo))
                    throw new ArgumentException("Note schedule exceeds the supported time range.");
                previous = note.startBeat;
            }
        }

        public Lesson Snapshot()
        {
            Validate();
            var copy = new Lesson { id = id, title = title, tempo = tempo,
                beatsPerBar = beatsPerBar, beatUnit = beatUnit, difficulty = difficulty,
                notes = new List<LessonNote>() };
            foreach (var n in notes)
                copy.notes.Add(new LessonNote { pitch = n.pitch, startBeat = n.startBeat,
                    durationBeats = n.durationBeats, hand = n.hand });
            return copy;
        }
    }

    [Serializable]
    public sealed class LessonNote
    {
        public int pitch;
        public double startBeat;
        public double durationBeats;
        public string hand;
    }

    public static class Numbers
    {
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
