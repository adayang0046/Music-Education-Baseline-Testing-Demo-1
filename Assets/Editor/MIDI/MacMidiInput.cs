#if UNITY_EDITOR_OSX
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using XRMidi;

// Editor-only adapter: native callbacks enqueue bytes; Poll emits on the main thread.
public sealed class MacMidiInput : IMidiInput, IDisposable
{
    private const string Library = "XRMidiMac";
    [DllImport(Library)] private static extern IntPtr xm_create();
    [DllImport(Library)] private static extern void xm_destroy(IntPtr input);
    [DllImport(Library)] private static extern int xm_count();
    [DllImport(Library)] private static extern uint xm_source(int index);
    [DllImport(Library)] private static extern int xm_name(uint source, byte[] buffer, int size);
    [DllImport(Library)] private static extern int xm_connect(IntPtr input, uint source);
    [DllImport(Library)] private static extern void xm_disconnect(IntPtr input);
    [DllImport(Library)] private static extern int xm_poll(IntPtr input, byte[] buffer, int size, out int overflow);
    private IntPtr handle;
    private readonly byte[] buffer = new byte[65536];
    private readonly MidiByteParser parser = new MidiByteParser();
    private readonly Func<double> clock;
    private double sampleTime;
    public event Action<NoteEvent> NoteReceived;
    public int OverflowCount { get; private set; }
    public uint ConnectedSource { get; private set; }
    public MacMidiInput(Func<double> clock)
    {
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        handle = xm_create();
        if (handle == IntPtr.Zero) throw new InvalidOperationException("CoreMIDI client creation failed.");
        parser.Note += OnNote;
    }
    public List<KeyValuePair<uint, string>> Sources()
    {
        var result = new List<KeyValuePair<uint, string>>();
        var name = new byte[1024];
        for (int i = 0; i < xm_count(); i++)
        {
            uint source = xm_source(i);
            if (source == 0) continue;
            Array.Clear(name, 0, name.Length);
            string label = xm_name(source, name, name.Length) != 0
                ? Encoding.UTF8.GetString(name).TrimEnd('\0') : "MIDI source " + source;
            result.Add(new KeyValuePair<uint, string>(source, label));
        }
        return result;
    }
    public void Connect(uint source)
    {
        Disconnect();
        int error = xm_connect(handle, source);
        if (error != 0) throw new InvalidOperationException("CoreMIDI connection error: " + error);
        ConnectedSource = source;
    }
    public void Disconnect()
    {
        if (handle != IntPtr.Zero) xm_disconnect(handle);
        ConnectedSource = 0; parser.Reset();
    }
    // Device timestamps are intentionally not used in this diagnostic. All notes
    // drained together share the polling timestamp; this is not latency calibration.
    public void Poll(bool discard = false)
    {
        if (handle == IntPtr.Zero) return;
        int count = xm_poll(handle, buffer, buffer.Length, out int overflow);
        if (overflow != OverflowCount) { parser.Reset(); OverflowCount = overflow; }
        if (discard) { parser.Reset(); return; }
        sampleTime = clock();
        for (int i = 0; i < count; i++) parser.Push(buffer[i]);
    }
    private void OnNote(int status, int pitch, int velocity)
        => NoteReceived?.Invoke(new NoteEvent(pitch, (status & 0xF0) == 0x90, velocity, sampleTime, status & 15));
    public void Dispose()
    {
        if (handle == IntPtr.Zero) return;
        xm_destroy(handle); handle = IntPtr.Zero; ConnectedSource = 0; parser.Note -= OnNote;
    }
}
#endif
