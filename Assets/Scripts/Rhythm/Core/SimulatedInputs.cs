using System;

namespace XRMidi
{
    public sealed class SimulatedMidiInput : IMidiInput
    {
        private readonly IMusicalClock clock;
        public event Action<NoteEvent> NoteReceived;
        public SimulatedMidiInput(IMusicalClock clock) { this.clock = clock ?? throw new ArgumentNullException(nameof(clock)); }
        public void Send(int pitch, bool on, int velocity = 100, int channel = 0) => NoteReceived?.Invoke(new NoteEvent(pitch, on, velocity, clock.ElapsedSeconds, channel));
        public void SendScheduled(NoteEvent note) => NoteReceived?.Invoke(note);
    }
    // A discrete simulation, not a claim of physical pinch recognition.
    public sealed class SimulatedGestureProvider : IGestureProvider
    {
        private readonly bool[] tracked = { true, true };
        private readonly bool[] active = new bool[2];
        public bool LeftTracked => tracked[0];
        public bool RightTracked => tracked[1];
        public event Action<GestureEvent> GestureStarted;
        public event Action<GestureEvent> GestureEnded;
        public void SetTracked(Hand hand, bool value)
        {
            tracked[(int)hand] = value;
            if (!value && active[(int)hand]) { active[(int)hand] = false; GestureEnded?.Invoke(new GestureEvent(hand, true)); }
        }
        public void SetPinch(Hand hand, bool value)
        {
            int i = (int)hand;
            if (!tracked[i] || active[i] == value) return;
            active[i] = value;
            if (value) GestureStarted?.Invoke(new GestureEvent(hand)); else GestureEnded?.Invoke(new GestureEvent(hand));
        }
    }
}
