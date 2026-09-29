using System;
using System.IO;

namespace XRMidi.Network
{
    public enum MidiRelayKind : byte { Heartbeat = 0, Note = 1, Goodbye = 2, Ack = 3 }

    // Version 1: fixed 32-byte, little-endian datagram. Network timestamps are
    // diagnostic only; sender and receiver clocks are NOT synchronized.
    public struct MidiRelayPacket
    {
        public ulong Stream;
        public uint Sequence;
        public MidiRelayKind Kind;
        public byte Pitch, Velocity, Channel;
        public bool On;
        public double SentTime;
        public byte[] Encode()
        {
            using (var memory = new MemoryStream(32))
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(new byte[] { 88, 77, 73, 68, 1, (byte)Kind, Pitch, Velocity, Channel, (byte)(On ? 1 : 0), 0, 0 });
                writer.Write(Stream); writer.Write(Sequence); writer.Write(SentTime);
                return memory.ToArray();
            }
        }
        public static bool TryDecode(byte[] bytes, out MidiRelayPacket packet)
        {
            packet = default;
            if (bytes == null || bytes.Length != 32 || bytes[0] != 88 || bytes[1] != 77 || bytes[2] != 73 || bytes[3] != 68 ||
                bytes[4] != 1 || bytes[5] > 3 || bytes[6] > 127 || bytes[7] > 127 || bytes[8] > 15 || bytes[9] > 1 || bytes[10] != 0 || bytes[11] != 0) return false;
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
            {
                reader.BaseStream.Position = 12;
                packet = new MidiRelayPacket { Kind = (MidiRelayKind)bytes[5], Pitch = bytes[6], Velocity = bytes[7], Channel = bytes[8], On = bytes[9] != 0,
                    Stream = reader.ReadUInt64(), Sequence = reader.ReadUInt32(), SentTime = reader.ReadDouble() };
            }
            if (packet.Stream == 0 || packet.Sequence == 0 || !Numbers.Finite(packet.SentTime) || packet.SentTime < 0) return false;
            return packet.Kind == MidiRelayKind.Note || (packet.Pitch == 0 && packet.Velocity == 0 && packet.Channel == 0 && !packet.On);
        }
    }
}
