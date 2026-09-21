using System;
using System.Collections.Generic;

namespace XRMidi
{
    public sealed class OnsetEvaluator : IPerformanceEvaluator
    {
        private readonly double perfect, good, accepted;
        private Lesson lesson;
        private bool[] resolved = new bool[0];
        private readonly List<PerformanceResult> results = new List<PerformanceResult>();
        private readonly List<NoteEvent> inputs = new List<NoteEvent>();
        public event Action<PerformanceResult> Evaluated;
        public IReadOnlyList<PerformanceResult> Results => results.AsReadOnly();
        public IReadOnlyList<NoteEvent> InputLog => inputs.AsReadOnly();
        public double EndSeconds { get; private set; }
        public OnsetEvaluator(TimingWindows windows)
        {
            if (windows == null) throw new ArgumentNullException(nameof(windows));
            windows.Validate(); perfect = windows.perfectMs; good = windows.goodMs; accepted = windows.acceptedMs;
        }
        public void Reset(Lesson source)
        {
            lesson = source.Snapshot(); resolved = new bool[lesson.notes.Count]; results.Clear(); inputs.Clear(); EndSeconds = 0;
            foreach (var n in lesson.notes)
                EndSeconds = Math.Max(EndSeconds, Math.Max(Seconds(n.startBeat + n.durationBeats), Seconds(n.startBeat) + accepted / 1000));
        }
        private double Seconds(double beats) => beats * 60.0 / lesson.tempo;
        public bool IsResolved(int index) => resolved[index];
        public void Submit(NoteEvent input)
        {
            if (lesson == null) throw new InvalidOperationException("Load a lesson first.");
            inputs.Add(input);
            if (!input.IsNoteOn) return;
            Advance(input.Timestamp);
            int match = -1, nearby = -1;
            double best = double.MaxValue, nearError = double.MaxValue;
            for (int i = 0; i < lesson.notes.Count; i++)
            {
                if (resolved[i]) continue;
                double error = Math.Abs((input.Timestamp - Seconds(lesson.notes[i].startBeat)) * 1000);
                if (error > accepted + 1e-7) continue;
                if (error < nearError) { nearby = i; nearError = error; }
                if (lesson.notes[i].pitch == input.Pitch && error < best) { match = i; best = error; }
            }
            if (match < 0)
            {
                // An extra input never consumes a target. Out-of-window pitches are Extra,
                // and their unmatched target subsequently becomes Miss.
                Emit(nearby, input, nearby < 0 ? Judgement.Extra : Judgement.WrongPitch);
                return;
            }
            resolved[match] = true;
            double signed = input.Timestamp - Seconds(lesson.notes[match].startBeat);
            var judgement = best <= perfect + 1e-7 ? Judgement.Perfect : best <= good + 1e-7 ? Judgement.Good : signed < 0 ? Judgement.Early : Judgement.Late;
            Emit(match, input, judgement);
        }
        public void Advance(double elapsedSeconds)
        {
            if (!Numbers.Finite(elapsedSeconds)) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (lesson == null) return;
            for (int i = 0; i < resolved.Length; i++)
                if (!resolved[i] && elapsedSeconds > Seconds(lesson.notes[i].startBeat) + accepted / 1000 + 1e-10)
                { resolved[i] = true; Emit(i, null, Judgement.Miss); }
        }
        private void Emit(int index, NoteEvent? input, Judgement judgement)
        {
            var result = new PerformanceResult(index, index < 0 ? -1 : lesson.notes[index].pitch,
                index < 0 ? double.NaN : Seconds(lesson.notes[index].startBeat), input, judgement);
            results.Add(result); Evaluated?.Invoke(result);
        }
        public PerformanceSummary Summary
        {
            get
            {
                int correct = 0, missed = 0, extras = 0; double error = 0;
                foreach (var r in results)
                    if (r.IsCorrect) { correct++; error += Math.Abs(r.TimingErrorMs.Value); }
                    else if (r.Judgement == Judgement.Miss) missed++; else extras++;
                return new PerformanceSummary(resolved.Length, correct, missed, extras, correct == 0 ? (double?)null : error / correct);
            }
        }
    }
}
