using System;

namespace XRMidi
{
    // Only a new pinch in Ready starts a lesson. Other states consume the edge
    // without deferring it; cancellation/release never starts or resumes a lesson.
    public sealed class GestureLessonBinding : IDisposable
    {
        private readonly IGestureProvider source;
        private readonly Func<SessionState> state;
        private readonly Action start;
        private readonly bool[] held = new bool[2];
        public GestureLessonBinding(IGestureProvider source, Func<SessionState> state, Action start)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.start = start ?? throw new ArgumentNullException(nameof(start));
            source.GestureStarted += OnStarted; source.GestureEnded += OnEnded;
        }
        private void OnStarted(GestureEvent e)
        {
            int hand = (int)e.Hand;
            if (e.Cancelled || held[hand]) return;
            held[hand] = true;
            bool tracked = e.Hand == Hand.Left ? source.LeftTracked : source.RightTracked;
            if (tracked && state() == SessionState.Ready) start();
        }
        private void OnEnded(GestureEvent e) { held[(int)e.Hand] = false; }
        public void Dispose() { source.GestureStarted -= OnStarted; source.GestureEnded -= OnEnded; }
    }
}
