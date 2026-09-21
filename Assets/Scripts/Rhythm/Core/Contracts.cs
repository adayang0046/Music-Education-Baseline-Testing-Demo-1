using System;
using System.Collections.Generic;

namespace XRMidi
{
    public interface ILessonRepository { Lesson Load(); }
    public interface IMidiInput { event Action<NoteEvent> NoteReceived; }
    public interface IMusicalClock
    {
        double ElapsedSeconds { get; }
        double Beat { get; }
        bool IsRunning { get; }
        void Start(double tempo, double leadInSeconds);
        void Pause();
        void Resume();
        void Restart();
        double BeatsToSeconds(double beats);
        double SecondsToBeats(double seconds);
    }
    public interface IPerformanceEvaluator
    {
        event Action<PerformanceResult> Evaluated;
        IReadOnlyList<PerformanceResult> Results { get; }
        IReadOnlyList<NoteEvent> InputLog { get; }
        double EndSeconds { get; }
        void Reset(Lesson lesson);
        void Submit(NoteEvent input);
        void Advance(double elapsedSeconds);
        bool IsResolved(int noteIndex);
        PerformanceSummary Summary { get; }
    }
    public interface INoteVisualizer
    {
        void Reset(Lesson lesson);
        void Render(double elapsedSeconds, IPerformanceEvaluator evaluator);
        void ApplyFeedback(PerformanceResult result);
    }
    public enum Hand { Left, Right }
    public struct GestureEvent
    {
        public Hand Hand { get; }
        public bool Cancelled { get; }
        public GestureEvent(Hand hand, bool cancelled = false) { Hand = hand; Cancelled = cancelled; }
    }
    public interface IGestureProvider
    {
        bool LeftTracked { get; }
        bool RightTracked { get; }
        event Action<GestureEvent> GestureStarted;
        event Action<GestureEvent> GestureEnded;
    }
    public struct NoteEvent
    {
        public int Pitch { get; }
        public bool IsNoteOn { get; }
        public int Velocity { get; }
        // Seconds on the shared lesson timeline, including negative lead-in time.
        public double Timestamp { get; }
        public int Channel { get; }
        public NoteEvent(int pitch, bool isNoteOn, int velocity, double timestamp, int channel = 0)
        {
            if (pitch < 0 || pitch > 127 || velocity < 0 || velocity > 127 || channel < 0 || channel > 15 || !Numbers.Finite(timestamp))
                throw new ArgumentOutOfRangeException(nameof(pitch), "Invalid MIDI event.");
            Pitch = pitch; IsNoteOn = isNoteOn && velocity > 0; Velocity = velocity;
            Timestamp = timestamp; Channel = channel;
        }
    }
}
