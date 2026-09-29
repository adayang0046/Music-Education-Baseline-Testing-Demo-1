using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace XRMidi.Network
{
    public sealed class UdpMidiSender : IDisposable
    {
        private readonly UdpClient socket;
        private readonly IPEndPoint destination;
        private readonly Func<double> realtime;
        private readonly ulong stream = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0) | 1UL;
        private readonly Dictionary<uint, double> pending = new Dictionary<uint, double>();
        private readonly Queue<double> roundTrips = new Queue<double>();
        private uint sequence;
        private bool disposed;
        private double nextHeartbeat, lastAck = double.NegativeInfinity;
        public int SentNotes { get; private set; }
        public bool Acknowledged => realtime() - lastAck < MidiRelaySession.TimeoutSeconds;
        public double MedianRoundTripMs => Percentile(0.5);
        public double P95RoundTripMs => Percentile(0.95);
        public UdpMidiSender(string address, int port, Func<double> realtime)
        {
            if (!IPAddress.TryParse(address, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Enter the Quest IPv4 address.");
            this.realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));
            destination = new IPEndPoint(ip, port); socket = new UdpClient(new IPEndPoint(IPAddress.Any, 0)); socket.Client.Blocking = false;
        }
        private void Send(MidiRelayPacket packet)
        {
            if (sequence == uint.MaxValue) throw new InvalidOperationException("Restart the relay to renew its sequence.");
            packet.Stream = stream; packet.Sequence = ++sequence; packet.SentTime = realtime();
            byte[] bytes = packet.Encode(); socket.Send(bytes, bytes.Length, destination);
            if (packet.Kind == MidiRelayKind.Heartbeat) pending[packet.Sequence] = packet.SentTime;
        }
        public void SendNote(NoteEvent note)
        {
            Send(new MidiRelayPacket { Kind = MidiRelayKind.Note, Pitch = (byte)note.Pitch, Velocity = (byte)note.Velocity, Channel = (byte)note.Channel, On = note.IsNoteOn });
            SentNotes++;
        }
        public void Tick()
        {
            double now = realtime();
            if (now >= nextHeartbeat) { Send(new MidiRelayPacket { Kind = MidiRelayKind.Heartbeat }); nextHeartbeat = now + 0.25; }
            foreach (uint key in pending.Where(p => now - p.Value > 3).Select(p => p.Key).ToArray()) pending.Remove(key);
            for (int i = 0; i < 256 && socket.Available > 0; i++)
            {
                IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes;
                try { bytes = socket.Receive(ref peer); }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock) { break; }
                if (!peer.Equals(destination) || !MidiRelayPacket.TryDecode(bytes, out var ack) || ack.Kind != MidiRelayKind.Ack || ack.Stream != stream ||
                    !pending.TryGetValue(ack.Sequence, out double sent)) continue;
                pending.Remove(ack.Sequence); lastAck = now; roundTrips.Enqueue((now - sent) * 1000);
                while (roundTrips.Count > 64) roundTrips.Dequeue();
            }
        }
        private double Percentile(double p)
        {
            if (roundTrips.Count == 0) return double.NaN;
            var values = roundTrips.OrderBy(v => v).ToArray(); return values[Math.Max(0, (int)Math.Ceiling(p * values.Length) - 1)];
        }
        public void Dispose()
        {
            if (disposed) return;
            try { Send(new MidiRelayPacket { Kind = MidiRelayKind.Goodbye }); }
            catch (SocketException) { }
            finally { disposed = true; socket.Dispose(); }
        }
    }
}
