using System;

namespace XRMidi
{
    public enum SessionState { Ready, Playing, Paused, Complete }
    public sealed class LessonSession : IDisposable
    {
        private readonly ILessonRepository repository;
        private readonly IMidiInput input;
        private readonly double leadIn;
        private Lesson lesson;
        public IMusicalClock Clock { get; }
        public IPerformanceEvaluator Evaluator { get; }
        public SessionState State { get; private set; }
        public event Action<Lesson> Started;
        public event Action<PerformanceSummary> Completed;
        public LessonSession(ILessonRepository repository, IMidiInput input, IMusicalClock clock,
            IPerformanceEvaluator evaluator, double leadInSeconds)
        {
            if (!Numbers.Finite(leadInSeconds) || leadInSeconds < 0) throw new ArgumentOutOfRangeException(nameof(leadInSeconds));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator)); leadIn = leadInSeconds;
            input.NoteReceived += OnNote;
        }
        public void Start()
        {
            Clock.Pause(); State = SessionState.Ready;
            // Load and validate before making a new session active.
            lesson = repository.Load().Snapshot(); Evaluator.Reset(lesson);
            Clock.Start(lesson.tempo, leadIn); State = SessionState.Playing;
            Started?.Invoke(lesson.Snapshot());
        }
        public void Restart() => Start();
        public void Pause() { if (State != SessionState.Playing) return; Clock.Pause(); State = SessionState.Paused; }
        public void Resume() { if (State != SessionState.Paused) return; Clock.Resume(); State = SessionState.Playing; }
        public void Tick()
        {
            if (State != SessionState.Playing) return;
            Evaluator.Advance(Clock.ElapsedSeconds);
            if (Clock.ElapsedSeconds <= Evaluator.EndSeconds) return;
            Clock.Pause(); State = SessionState.Complete; Completed?.Invoke(Evaluator.Summary);
        }
        private void OnNote(NoteEvent note)
        {
            if (State != SessionState.Playing) return;
            // Inputs are delivered in timestamp order before Tick on the main thread.
            if (note.Timestamp > Evaluator.EndSeconds) { Tick(); return; }
            Evaluator.Submit(note);
        }
        public void Dispose() { input.NoteReceived -= OnNote; Clock.Pause(); }
    }
    public static class NoteTrajectory
    {
        public static double Position(double hitTime, double elapsed, double approach, double distance, double hitLine)
        {
            if (!Numbers.Finite(approach) || approach <= 0) throw new ArgumentOutOfRangeException(nameof(approach));
            return hitLine + (hitTime - elapsed) / approach * distance;
        }
    }
}
