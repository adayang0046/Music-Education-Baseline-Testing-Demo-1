using System;
using System.Net;
using System.Net.Sockets;

namespace XRMidi.Network
{
    // Poll on the owning/main thread. Bounded work avoids stalling rendering on a flood.
    public sealed class UdpMidiReceiver : IMidiInput, IDisposable
    {
        public const int DefaultPort = 45831;
        private readonly UdpClient socket;
        private readonly Func<double> lessonClock, realtime;
        private bool discardPending;
        public MidiRelaySession Session { get; } = new MidiRelaySession();
        public event Action<NoteEvent> NoteReceived;
        public event Action ConnectionInterrupted;
        public int InvalidPackets { get; private set; }
        public int ReceivedNotes { get; private set; }
        public int Port => ((IPEndPoint)socket.Client.LocalEndPoint).Port;
        public UdpMidiReceiver(int port, Func<double> lessonClock, Func<double> realtime)
        {
            this.lessonClock = lessonClock ?? throw new ArgumentNullException(nameof(lessonClock));
            this.realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));
            socket = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            socket.Client.Blocking = false;
        }
        public void Poll(bool deliverNotes = true)
        {
            if (Session.Expire(realtime())) ConnectionInterrupted?.Invoke();
            for (int i = 0; i < 256 && socket.Available > 0; i++)
            {
                IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes;
                try { bytes = socket.Receive(ref peer); }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock) { break; }
                if (!MidiRelayPacket.TryDecode(bytes, out var packet)) { InvalidPackets++; continue; }
                ulong missing = Session.MissingPackets;
                if (!Session.Accept(packet, peer.ToString(), realtime())) continue;
                if (missing != Session.MissingPackets || packet.Kind == MidiRelayKind.Goodbye) ConnectionInterrupted?.Invoke();
                if (packet.Kind == MidiRelayKind.Heartbeat)
                {
                    packet.Kind = MidiRelayKind.Ack;
                    byte[] ack = packet.Encode(); socket.Send(ack, ack.Length, peer);
                }
                if (packet.Kind == MidiRelayKind.Note)
                {
                    ReceivedNotes++;
                    if (deliverNotes && !discardPending) NoteReceived?.Invoke(new NoteEvent(packet.Pitch, packet.On, packet.Velocity, lessonClock(), packet.Channel));
                }
            }
            if (socket.Available == 0) discardPending = false;
        }
        public void DiscardPending() { discardPending = true; }
        public void Dispose() { socket.Dispose(); Session.Clear(); }
    }
}
