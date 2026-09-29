using System;

namespace XRMidi
{
    // MIDI 1.0 byte stream, including running status and interleaved realtime bytes.
    // SysEx, system-common and non-note channel messages are consumed but not emitted.
    public sealed class MidiByteParser
    {
        private int status, first, received;
        public event Action<int, int, int> Note; // status byte, pitch, velocity
        public void Reset() { status = first = received = 0; }
        public void Push(byte value)
        {
            if (value >= 0xF8) return;
            if (value >= 0x80)
            {
                received = 0;
                status = value < 0xF0 ? value : 0;
                return;
            }
            if (status == 0) return;
            int type = status & 0xF0;
            int length = type == 0xC0 || type == 0xD0 ? 1 : 2;
            if (received++ == 0) first = value;
            if (received < length) return;
            received = 0;
            if (type == 0x80 || type == 0x90) Note?.Invoke(status, first, value);
        }
    }
}
