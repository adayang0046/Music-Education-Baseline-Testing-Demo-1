using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace XRMidi
{
    // Native adapters may emit on a worker thread. Only Drain dispatches to subscribers.
    // Construct and drain on Unity's main thread, before the session's miss sweep.
    public sealed class MainThreadMidiInput : IMidiInput, IDisposable
    {
        private readonly IMidiInput source;
        private readonly int ownerThread = Thread.CurrentThread.ManagedThreadId;
        private readonly object gate = new object();
        private readonly List<NoteEvent> pending = new List<NoteEvent>();
        private bool accepting;
        public event Action<NoteEvent> NoteReceived;
        public MainThreadMidiInput(IMidiInput source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            source.NoteReceived += Enqueue;
        }
        private void Enqueue(NoteEvent note) { lock (gate) { if (accepting) pending.Add(note); } }
        public void SetAccepting(bool value)
        {
            AssertOwner();
            lock (gate) { accepting = value; pending.Clear(); }
        }
        public void Drain()
        {
            AssertOwner(); NoteEvent[] batch;
            lock (gate) { batch = pending.ToArray(); pending.Clear(); }
            // Stable ordering preserves on/off ordering for equal timestamps.
            foreach (var note in batch.OrderBy(n => n.Timestamp)) NoteReceived?.Invoke(note);
        }
        private void AssertOwner()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThread)
                throw new InvalidOperationException("Drain and lifecycle operations must run on the creating thread.");
        }
        public void Dispose() { SetAccepting(false); source.NoteReceived -= Enqueue; }
    }
}
