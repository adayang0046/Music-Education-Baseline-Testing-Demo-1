using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMidi;

public sealed class DemoSceneTests
{
    [UnityTest]
    public IEnumerator DemoStartsPausesCompletesAndRestartsInPlayMode()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Milestone1Demo.unity");
        yield return new EnterPlayMode();
        yield return null;
        var controller = Object.FindObjectOfType<RhythmDemoController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(controller.ReceivePhysicalMidi(60, true, 100, 0), Is.False, "Ready lessons must ignore hardware notes.");
        Text FindText(string name) => Object.FindObjectsOfType<Text>().Single(t => t.name == name);
        Assert.That(FindText("Title").text, Is.EqualTo("C Major - Five Quarter Notes"));
        Assert.That(Object.FindObjectsOfType<Button>().Length, Is.EqualTo(8));
        controller.StartLesson(); yield return null;
        Assert.That(FindText("Status").text, Does.Contain("Playing"));
        Assert.That(controller.CanReceivePhysicalMidi, Is.True);
        Assert.That(controller.ReceivePhysicalMidi(60, true, 100, 0), Is.True);
        Assert.That(controller.ReceivePhysicalMidi(60, false, 0, 0), Is.True);
        controller.TogglePause(); yield return null;
        string paused = FindText("Status").text;
        Assert.That(controller.MidiLessonStatus, Is.EqualTo("Paused"));
        Assert.That(controller.ReceivePhysicalMidi(62, true, 100, 0), Is.False);
        controller.PlayNote(60); yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(FindText("Status").text, Is.EqualTo(paused));
        Assert.That(FindText("Start label"), Is.Not.Null);
        Assert.That(FindText("Pause label").text, Is.EqualTo("Resume"));
        controller.TogglePause(); yield return null;
        Assert.That(FindText("Status").text, Does.Contain("Playing"));
        controller.RestartLesson(); yield return null;
        Assert.That(FindText("Status").text, Does.Contain("Ready in"));
        CaptureIfRequested();
        yield return new WaitForSecondsRealtime(7.2f);
        yield return null;
        Assert.That(FindText("Feedback").text, Does.Contain("Total 5 | Correct 0 | Missed 5"));
        controller.RestartLesson(); yield return null;
        Assert.That(FindText("Feedback").text, Does.Contain("Get ready"));
        Assert.That(FindText("Status").text, Does.Contain("Playing"));
        yield return new ExitPlayMode();
        LogAssert.NoUnexpectedReceived();
    }

    private static void CaptureIfRequested()
    {
        string path = System.Environment.GetEnvironmentVariable("XR_MIDI_CAPTURE_PATH");
        if (string.IsNullOrEmpty(path)) return;
        Canvas.ForceUpdateCanvases();
        var camera = Camera.main;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1280, 900, 24);
        var pixels = new Texture2D(1280, 900, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            Object.DestroyImmediate(pixels); Object.DestroyImmediate(target);
        }
    }
}
