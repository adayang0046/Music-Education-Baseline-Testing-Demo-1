using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMidi;
using XRMidi.Network;

public sealed class QuestMidiRelaySceneTests
{
    [UnityTest]
    public IEnumerator RelaySceneReceivesLiveNotesAndPausesOnDisconnect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/QuestMidiRelayDemo.unity");
        yield return new EnterPlayMode(); yield return null; yield return null;
        var controller = Object.FindObjectOfType<RhythmDemoController>();
        var status = Object.FindObjectsOfType<Text>().Single(t => t.name == "MIDI and gesture status");
        Assert.That(controller.AllowSimulatedNotes, Is.False);
        Assert.That(controller.AutomaticNotes, Is.False);
        controller.ToggleAutomaticNotes(); Assert.That(controller.AutomaticNotes, Is.False);
        Assert.That(status.text, Does.Contain("WAITING").And.Contain("NOT TRACKED"));
        QuestLessonSceneTests.CapturePreview("QuestMidiRelayPreview.png");
        // Batch-mode Editor has no focused Game window. Explicitly model headset focus.
        Object.FindObjectOfType<QuestMidiRelay>().SendMessage("OnApplicationFocus", true);
        yield return null;
        using (var sender = new UdpMidiSender("127.0.0.1", UdpMidiReceiver.DefaultPort, () => Time.realtimeSinceStartupAsDouble))
        {
            for (int i = 0; i < 100 && !sender.Acknowledged; i++) { sender.Tick(); yield return null; }
            Assert.That(sender.Acknowledged, Is.True);
            Object.FindObjectsOfType<Button>().Single(b => b.name == "Start").onClick.Invoke();
            yield return null;
            Assert.That(controller.State, Is.EqualTo(SessionState.Playing));
            LogAssert.Expect(LogType.Log, "[MidiRelay] ON MIDI 60 | velocity 100 | channel 1");
            sender.SendNote(new NoteEvent(60, true, 100, 0));
            yield return null; yield return null;
            Assert.That(status.text, Does.Contain("ON MIDI 60"));
        }
        yield return null; yield return null;
        Assert.That(controller.State, Is.EqualTo(SessionState.Paused));
        yield return new ExitPlayMode(); LogAssert.NoUnexpectedReceived();
    }
}
