using System;

namespace XRMidi
{
    // Hysteresis prevents distance noise from repeatedly starting/ending a pinch.
    // After any tracking gap, an open hand must be seen before another start.
    public sealed class PinchGestureState
    {
        private readonly Hand hand;
        private readonly double pressDistance, releaseDistance;
        private bool armed;
        public bool IsTracked { get; private set; }
        public bool IsPinching { get; private set; }
        public event Action<GestureEvent> Started;
        public event Action<GestureEvent> Ended;

        public PinchGestureState(Hand hand, double pressMeters, double releaseMeters)
        {
            if (!Numbers.Finite(pressMeters) || !Numbers.Finite(releaseMeters) || pressMeters <= 0 || releaseMeters <= pressMeters)
                throw new ArgumentException("Pinch distances must be finite, positive, and press < release.");
            this.hand = hand; pressDistance = pressMeters; releaseDistance = releaseMeters;
        }
        public void Update(bool valid, double thumbIndexDistance)
        {
            if (!valid || !Numbers.Finite(thumbIndexDistance) || thumbIndexDistance < 0)
            { Cancel(); return; }
            IsTracked = true;
            if (thumbIndexDistance >= releaseDistance)
            {
                armed = true;
                if (IsPinching) { IsPinching = false; Ended?.Invoke(new GestureEvent(hand)); }
            }
            else if (armed && !IsPinching && thumbIndexDistance <= pressDistance)
            { IsPinching = true; Started?.Invoke(new GestureEvent(hand)); }
        }
        public void Cancel()
        {
            IsTracked = false; armed = false;
            if (!IsPinching) return;
            IsPinching = false; Ended?.Invoke(new GestureEvent(hand, true));
        }
    }
}
