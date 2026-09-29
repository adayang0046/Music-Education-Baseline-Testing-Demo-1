using System;

namespace XRMidi
{
    // Target ID zero means no target. Once fired, gaze must leave before firing again.
    public sealed class DwellSelection
    {
        private readonly double seconds;
        private int target;
        private double elapsed;
        private bool fired;
        public double Progress => Math.Min(1, elapsed / seconds);
        public DwellSelection(double seconds)
        {
            if (!Numbers.Finite(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            this.seconds = seconds;
        }
        public bool Update(int targetId, double delta)
        {
            if (!Numbers.Finite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
            if (targetId != target || targetId == 0) { Reset(); target = targetId; }
            if (target == 0 || fired) return false;
            elapsed += delta;
            if (elapsed < seconds) return false;
            fired = true; return true;
        }
        public void Reset() { target = 0; elapsed = 0; fired = false; }
    }
}
