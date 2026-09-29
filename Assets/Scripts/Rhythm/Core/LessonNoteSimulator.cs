using System;
using System.Collections.Generic;
using System.Linq;

namespace XRMidi
{
    // Explicit test input, not a performance model. Exact scheduled timestamps
    // deliberately produce perfect onsets even when a frame arrives late.
    public sealed class LessonNoteSimulator
    {
        private List<NoteEvent> events = new List<NoteEvent>();
        private int next;
        public void Reset(Lesson lesson)
        {
            if (lesson == null) throw new ArgumentNullException(nameof(lesson));
            lesson.Validate();
            events.Clear(); next = 0;
            foreach (var note in lesson.notes)
            {
                events.Add(new NoteEvent(note.pitch, true, 100, note.startBeat * 60 / lesson.tempo));
                events.Add(new NoteEvent(note.pitch, false, 0, (note.startBeat + note.durationBeats) * 60 / lesson.tempo));
            }
            events = events.OrderBy(e => e.Timestamp).ToList();
        }
        public void Advance(double time, Action<NoteEvent> emit)
        {
            if (!Numbers.Finite(time)) throw new ArgumentOutOfRangeException(nameof(time));
            if (emit == null) throw new ArgumentNullException(nameof(emit));
            while (next < events.Count && events[next].Timestamp <= time) emit(events[next++]);
        }
    }
}
