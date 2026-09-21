using System;

namespace XRMidi
{
    public sealed class MusicalClock : IMusicalClock
    {
        private readonly Func<double> now;
        private double origin, frozen, tempo = 60, leadIn;
        public bool IsRunning { get; private set; }
        public double ElapsedSeconds => IsRunning ? frozen + now() - origin : frozen;
        public double Beat => SecondsToBeats(ElapsedSeconds);
        public MusicalClock(Func<double> monotonicSeconds)
        { now = monotonicSeconds ?? throw new ArgumentNullException(nameof(monotonicSeconds)); }
        public double BeatsToSeconds(double beats) => beats * 60.0 / tempo;
        public double SecondsToBeats(double seconds) => seconds * tempo / 60.0;
        public void Start(double bpm, double leadInSeconds)
        {
            if (!Numbers.Finite(bpm) || bpm <= 0 || !Numbers.Finite(leadInSeconds) || leadInSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(bpm));
            tempo = bpm; leadIn = leadInSeconds; Restart();
        }
        public void Restart() { frozen = -leadIn; origin = now(); IsRunning = true; }
        public void Pause() { if (!IsRunning) return; frozen = ElapsedSeconds; IsRunning = false; }
        public void Resume() { if (IsRunning) return; origin = now(); IsRunning = true; }
    }
}
