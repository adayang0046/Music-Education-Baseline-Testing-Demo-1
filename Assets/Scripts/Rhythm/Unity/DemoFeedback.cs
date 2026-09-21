namespace XRMidi
{
    // Presentation strings are deliberately outside the evaluator.
    public sealed class DemoFeedback
    {
        public string Text { get; private set; } = "Press Start (Space) or pinch (P).";
        public void Reset() { Text = "Get ready - play when each note reaches the line."; }
        public void SetError(string message) { Text = message; }
        public void Show(PerformanceResult result)
        {
            Text = result.Judgement == Judgement.WrongPitch ? "Wrong pitch" :
                result.Judgement == Judgement.Extra ? "Extra note / outside timing window" : result.Judgement.ToString();
            if (result.IsCorrect) Text += $" ({result.TimingErrorMs.Value:+0;-0;0} ms)";
        }
        public void Complete(PerformanceSummary s)
        {
            string average = s.AverageAbsoluteErrorMs.HasValue ? $"{s.AverageAbsoluteErrorMs.Value:0.0} ms" : "N/A";
            Text = $"Complete! Total {s.Total} | Correct {s.Correct} | Missed {s.Missed}\nExtra notes {s.Extras} | Average absolute onset error: {average}";
        }
    }
}
