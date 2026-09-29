namespace XRMidi.Network
{
    // Locks to one sender endpoint/stream. Reordered or duplicate datagrams cannot
    // retrigger notes. A new stream must wait for goodbye or the connection timeout.
    public sealed class MidiRelaySession
    {
        public const double TimeoutSeconds = 2;
        public string Peer { get; private set; }
        public ulong Stream { get; private set; }
        public uint Sequence { get; private set; }
        public ulong MissingPackets { get; private set; }
        private double lastSeen;
        public bool Connected => Peer != null;
        public bool Expire(double now)
        {
            if (!Connected || now - lastSeen < TimeoutSeconds) return false;
            Clear(); return true;
        }
        public bool Accept(MidiRelayPacket packet, string peer, double now)
        {
            if (packet.Kind == MidiRelayKind.Ack || string.IsNullOrEmpty(peer) || !Numbers.Finite(now)) return false;
            Expire(now);
            if (!Connected)
            {
                if (packet.Kind != MidiRelayKind.Heartbeat) return false;
                Peer = peer; Stream = packet.Stream; Sequence = packet.Sequence; lastSeen = now; return true;
            }
            if (peer != Peer || packet.Stream != Stream || packet.Sequence <= Sequence) return false;
            MissingPackets += packet.Sequence - Sequence - 1;
            Sequence = packet.Sequence; lastSeen = now;
            if (packet.Kind == MidiRelayKind.Goodbye) Clear();
            return true;
        }
        public void Clear() { Peer = null; Stream = 0; Sequence = 0; }
    }
}
