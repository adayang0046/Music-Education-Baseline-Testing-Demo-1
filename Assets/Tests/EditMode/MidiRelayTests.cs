using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using XRMidi;
using XRMidi.Network;

public sealed class MidiRelayTests
{
    private static MidiRelayPacket Packet(uint sequence, MidiRelayKind kind = MidiRelayKind.Heartbeat, ulong stream = 7)
        => new MidiRelayPacket { Stream = stream, Sequence = sequence, Kind = kind, SentTime = 1 };
    [Test] public void PacketRoundTripPreservesNoteAndNormalizesZeroVelocity()
    {
        var packet = Packet(3, MidiRelayKind.Note); packet.Pitch = 60; packet.Channel = 2; packet.On = true;
        Assert.That(MidiRelayPacket.TryDecode(packet.Encode(), out var copy), Is.True);
        var note = new NoteEvent(copy.Pitch, copy.On, copy.Velocity, 0, copy.Channel);
        Assert.That(note.IsNoteOn, Is.False); Assert.That(note.Channel, Is.EqualTo(2));
        Assert.That(copy.Stream, Is.EqualTo(7)); Assert.That(copy.Sequence, Is.EqualTo(3));
    }
    [TestCase(0, 0)] [TestCase(4, 2)] [TestCase(5, 10)] [TestCase(6, 128)] [TestCase(8, 16)] [TestCase(9, 2)] [TestCase(10, 1)]
    public void InvalidDatagramsAreRejected(int offset, int value)
    {
        var bytes = Packet(1).Encode(); bytes[offset] = (byte)value;
        Assert.That(MidiRelayPacket.TryDecode(bytes, out _), Is.False);
    }
    [Test] public void InvalidLengthAndTimeAreRejected()
    {
        Assert.That(MidiRelayPacket.TryDecode(new byte[31], out _), Is.False);
        var packet = Packet(1); packet.SentTime = double.NaN;
        Assert.That(MidiRelayPacket.TryDecode(packet.Encode(), out _), Is.False);
        packet = Packet(1, stream: 0); Assert.That(MidiRelayPacket.TryDecode(packet.Encode(), out _), Is.False);
    }
    [Test] public void ReceiverRequiresHeartbeatAndRejectsDuplicatesAndOtherSenders()
    {
        var session = new MidiRelaySession();
        Assert.That(session.Accept(Packet(1, MidiRelayKind.Note), "a", 0), Is.False);
        Assert.That(session.Accept(Packet(1), "a", 0), Is.True);
        Assert.That(session.Accept(Packet(1), "a", 0.1), Is.False);
        Assert.That(session.Accept(Packet(2), "b", 0.1), Is.False);
        Assert.That(session.Accept(Packet(2, stream: 8), "a", 0.1), Is.False);
        Assert.That(session.Accept(Packet(3, MidiRelayKind.Note), "a", 0.1), Is.True);
        Assert.That(session.MissingPackets, Is.EqualTo(1));
        Assert.That(session.Accept(Packet(2, MidiRelayKind.Note), "a", 0.2), Is.False);
    }
    [Test] public void TimeoutAndGoodbyePermitNewStream()
    {
        var session = new MidiRelaySession(); session.Accept(Packet(1), "a", 0);
        Assert.That(session.Expire(1.9), Is.False); Assert.That(session.Expire(2), Is.True);
        Assert.That(session.Accept(Packet(1, stream: 8), "b", 2.1), Is.True);
        Assert.That(session.Accept(Packet(2, MidiRelayKind.Goodbye, 8), "b", 2.2), Is.True);
        Assert.That(session.Connected, Is.False);
    }
    [Test] public void LoopbackTransfersNotesAcknowledgesAndDropsSuspendedBacklog()
    {
        double now = 1; var notes = new List<NoteEvent>(); int interruptions = 0;
        using (var receiver = new UdpMidiReceiver(0, () => -0.5, () => now))
        using (var sender = new UdpMidiSender("127.0.0.1", receiver.Port, () => now))
        {
            receiver.NoteReceived += notes.Add; receiver.ConnectionInterrupted += () => interruptions++;
            sender.Tick();
            for (int i = 0; i < 100 && !sender.Acknowledged; i++) { Thread.Sleep(2); receiver.Poll(); now += 0.002; sender.Tick(); }
            Assert.That(sender.Acknowledged, Is.True); Assert.That(sender.MedianRoundTripMs, Is.GreaterThanOrEqualTo(0));
            sender.SendNote(new NoteEvent(62, true, 90, 0, 1));
            for (int i = 0; i < 100 && notes.Count == 0; i++) { Thread.Sleep(2); receiver.Poll(); }
            Assert.That(notes.Count, Is.EqualTo(1)); Assert.That(notes[0].Timestamp, Is.EqualTo(-0.5));
            Assert.That(notes[0].Pitch, Is.EqualTo(62)); Assert.That(notes[0].Channel, Is.EqualTo(1));
            sender.SendNote(new NoteEvent(62, false, 0, 0, 1)); Thread.Sleep(10); receiver.DiscardPending(); receiver.Poll();
            Assert.That(notes.Count, Is.EqualTo(1));
            now += 3; receiver.Poll(); Assert.That(interruptions, Is.EqualTo(1));
            Assert.That(receiver.Session.Connected, Is.False);
        }
    }
    [Test] public void GapInterruptsBeforeDeliveringTheFollowingNote()
    {
        using (var receiver = new UdpMidiReceiver(0, () => 0, () => 1))
        using (var socket = new UdpClient())
        {
            var order = new List<string>(); receiver.ConnectionInterrupted += () => order.Add("interrupt"); receiver.NoteReceived += _ => order.Add("note");
            var endpoint = new IPEndPoint(IPAddress.Loopback, receiver.Port);
            var hello = Packet(1).Encode(); socket.Send(hello, hello.Length, endpoint);
            var note = Packet(3, MidiRelayKind.Note); note.Pitch = 60; note.Velocity = 100; note.On = true;
            var bytes = note.Encode(); socket.Send(bytes, bytes.Length, endpoint);
            for (int i = 0; i < 100 && order.Count < 2; i++) { Thread.Sleep(2); receiver.Poll(); }
            CollectionAssert.AreEqual(new[] { "interrupt", "note" }, order);
        }
    }
}
