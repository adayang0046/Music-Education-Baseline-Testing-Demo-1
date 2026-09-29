using System.Collections.Generic;
using NUnit.Framework;
using XRMidi;

public sealed class MidiByteParserTests
{
    private MidiByteParser parser;
    private List<NoteEvent> notes;
    [SetUp] public void SetUp()
    {
        parser = new MidiByteParser(); notes = new List<NoteEvent>();
        parser.Note += (status, pitch, velocity) => notes.Add(new NoteEvent(pitch, (status & 0xF0) == 0x90, velocity, 1.25, status & 15));
    }
    private void Send(params byte[] bytes) { foreach (byte value in bytes) parser.Push(value); }
    [Test] public void FragmentedNotesPreserveChannelVelocityAndZeroVelocityOff()
    {
        Send(0x92, 60); Assert.That(notes, Is.Empty);
        Send(100, 0x82, 60, 25, 0x92, 62, 0);
        Assert.That(notes.Count, Is.EqualTo(3));
        Assert.That(notes[0].Channel, Is.EqualTo(2)); Assert.That(notes[0].Velocity, Is.EqualTo(100));
        Assert.That(notes[1].IsNoteOn, Is.False); Assert.That(notes[1].Velocity, Is.EqualTo(25));
        Assert.That(notes[2].IsNoteOn, Is.False);
    }
    [Test] public void RunningStatusAndRealtimeBytesDoNotCreateExtraNotes()
    {
        Send(0x90, 60, 0xF8, 100, 62, 0xFE, 110, 64, 0);
        Assert.That(notes.Count, Is.EqualTo(3)); Assert.That(notes[1].Pitch, Is.EqualTo(62));
        Assert.That(notes[2].IsNoteOn, Is.False);
    }
    [Test] public void ControllersAndProgramChangesAreNotNotes()
    {
        Send(0xB0, 64, 127, 65, 0, 0xC0, 2, 3, 0xD0, 50, 0xE0, 0, 64);
        Assert.That(notes, Is.Empty);
        Send(0x90, 60, 127); Assert.That(notes.Count, Is.EqualTo(1));
    }
    [Test] public void SystemMessagesCancelRunningStatus()
    {
        Send(0x90, 60, 100, 0xF0, 1, 2, 0xF8, 3, 0xF7, 62, 100, 0xF2, 1, 2);
        Assert.That(notes.Count, Is.EqualTo(1));
        Send(0x90, 64, 90); Assert.That(notes.Count, Is.EqualTo(2));
    }
    [Test] public void StrayDataAndInterruptedMessagesDoNotEmitNotes()
    {
        Send(60, 100, 0x90, 60, 0x80, 62, 30);
        Assert.That(notes.Count, Is.EqualTo(1)); Assert.That(notes[0].Pitch, Is.EqualTo(62));
        Assert.That(notes[0].IsNoteOn, Is.False);
    }
    [Test] public void ResetDropsPartialMessageAndRunningStatus()
    {
        Send(0x90, 60); parser.Reset(); Send(100, 62, 110);
        Assert.That(notes, Is.Empty); Send(0x90, 64, 100); Assert.That(notes.Count, Is.EqualTo(1));
    }
    [Test] public void InputPreservesExplicitChannelAndLessonTimestamp()
    {
        var clock = new MusicalClock(() => 100.0); clock.Start(60, 2);
        var input = new SimulatedMidiInput(clock);
        input.NoteReceived += notes.Add; input.Send(60, true, 98, 3);
        Assert.That(notes[0].Channel, Is.EqualTo(3)); Assert.That(notes[0].Timestamp, Is.EqualTo(-2));
    }
#if UNITY_EDITOR_OSX
    [Test] public void MacAdapterLoadsNativeBridgeAndDisposesWithoutDevice()
    {
        using (var input = new MacMidiInput(() => 0))
        {
            Assert.That(input.Sources(), Is.Not.Null);
            Assert.That(input.ConnectedSource, Is.Zero);
            input.Poll(); input.Disconnect();
        }
    }
#endif
}
