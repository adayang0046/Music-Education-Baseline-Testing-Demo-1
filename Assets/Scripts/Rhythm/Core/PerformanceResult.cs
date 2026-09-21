using System;

namespace XRMidi
{
    public enum Judgement { Perfect, Good, Early, Late, WrongPitch, Extra, Miss }
    [Serializable]
    public sealed class TimingWindows
    {
        public double perfectMs = 50;
        public double goodMs = 120;
        public double acceptedMs = 200;
        public void Validate()
        {
            if (!Numbers.Finite(perfectMs) || !Numbers.Finite(goodMs) || !Numbers.Finite(acceptedMs) ||
                perfectMs < 0 || goodMs < perfectMs || acceptedMs < goodMs || acceptedMs <= 0)
                throw new ArgumentException("Timing windows must be finite and ordered: 0 <= perfect <= good <= accepted.");
        }
    }
    public sealed class PerformanceResult
    {
        public int NoteIndex { get; }
        public int ExpectedPitch { get; }
        public double ExpectedSeconds { get; }
        public NoteEvent? Input { get; }
        public Judgement Judgement { get; }
        public double? TimingErrorMs => Input.HasValue && NoteIndex >= 0 ? (Input.Value.Timestamp - ExpectedSeconds) * 1000 : (double?)null;
        public bool IsCorrect => Judgement == Judgement.Perfect || Judgement == Judgement.Good || Judgement == Judgement.Early || Judgement == Judgement.Late;
        public PerformanceResult(int index, int pitch, double expected, NoteEvent? input, Judgement judgement)
        { NoteIndex = index; ExpectedPitch = pitch; ExpectedSeconds = expected; Input = input; Judgement = judgement; }
    }
    public sealed class PerformanceSummary
    {
        public int Total { get; }
        public int Correct { get; }
        public int Missed { get; }
        public int Extras { get; }
        public double? AverageAbsoluteErrorMs { get; }
        public PerformanceSummary(int total, int correct, int missed, int extras, double? average)
        { Total = total; Correct = correct; Missed = missed; Extras = extras; AverageAbsoluteErrorMs = average; }
    }
}
