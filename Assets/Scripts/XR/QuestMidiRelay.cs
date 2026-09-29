using System;
using UnityEngine;
using UnityEngine.UI;
using XRMidi.Network;

namespace XRMidi
{
    // Transport/presentation adapter only. All musical evaluation stays in the lesson.
    public sealed class QuestMidiRelay : MonoBehaviour
    {
        [SerializeField] private RhythmDemoController lesson;
        [SerializeField] private QuestHandGestureProvider hands;
        [SerializeField] private Text display;
        [SerializeField] private int port = UdpMidiReceiver.DefaultPort;
        private UdpMidiReceiver receiver;
        private bool focused = true, suspended;
        private string lastNote = "Play a key on the Mac-connected keyboard";
        private string failure;
        public void Configure(RhythmDemoController controller, QuestHandGestureProvider provider, Text status)
        { lesson = controller; hands = provider; display = status; }
        private void OnEnable()
        {
            try
            {
                receiver = new UdpMidiReceiver(port, () => lesson.LessonElapsedSeconds, () => Time.realtimeSinceStartupAsDouble);
                receiver.NoteReceived += OnNote; receiver.ConnectionInterrupted += Interrupt;
            }
            catch (Exception e) { failure = "MIDI receiver: " + e.Message; Debug.LogError(failure); }
        }
        private void Update()
        {
            if (receiver != null)
            {
                try { receiver.Poll(focused && !suspended); }
                catch (Exception e) { failure = "MIDI connection error: " + e.Message; Interrupt(); Close(); }
            }
            if (receiver == null || !receiver.Session.Connected) lesson.PauseLesson();
            if (display == null) return;
            string link = receiver == null ? failure : receiver.Session.Connected ? "CONNECTED: " + receiver.Session.Peer : "WAITING for Mac relay on UDP " + port;
            display.text = link + $" | events {receiver?.ReceivedNotes ?? 0} | missing packets {receiver?.Session.MissingPackets ?? 0}\n" +
                lastNote + "\nHands — L: " + HandStatus(Hand.Left) + "   R: " + HandStatus(Hand.Right);
        }
        private string HandStatus(Hand hand)
        {
            bool tracked = hand == Hand.Left ? hands.LeftTracked : hands.RightTracked;
            return !tracked ? "<color=#AAAAAA>NOT TRACKED</color>" : hands.IsPinching(hand)
                ? "<color=#FFD34D>PINCH</color>" : "<color=#8EDD91>OPEN</color>";
        }
        private void OnNote(NoteEvent note)
        {
            lastNote = $"{(note.IsNoteOn ? "ON" : "OFF")} MIDI {note.Pitch} | velocity {note.Velocity} | channel {note.Channel + 1}";
            Debug.Log("[MidiRelay] " + lastNote);
            lesson.ReceivePhysicalMidi(note.Pitch, note.IsNoteOn, note.Velocity, note.Channel);
        }
        private void Interrupt()
        {
            lesson.PauseLesson();
            lastNote = "Link interrupted / packet gap. Check connection, then Resume or Restart.";
        }
        private void OnApplicationFocus(bool value)
        { focused = value; receiver?.DiscardPending(); if (!value) lesson.PauseLesson(); }
        private void OnApplicationPause(bool value)
        { suspended = value; receiver?.DiscardPending(); if (value) lesson.PauseLesson(); }
        private void Close() { receiver?.Dispose(); receiver = null; }
        private void OnDisable() { if (lesson != null) lesson.PauseLesson(); Close(); }
    }
}
