using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMidi;

public sealed class QuestLessonSceneTests
{
    [UnityTest]
    public IEnumerator QuestCompositionCompletesWithSimulatedNotesAndKeepsFallbackControls()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/QuestLessonDemo.unity");
        yield return new EnterPlayMode(); yield return null; yield return null;
        var controller = Object.FindObjectOfType<RhythmDemoController>();
        Assert.That(controller.State, Is.EqualTo(SessionState.Ready)); Assert.That(controller.AutomaticNotes, Is.True);
        Assert.That(Object.FindObjectOfType<QuestHandGestureProvider>(), Is.Not.Null);
        Assert.That(Object.FindObjectsOfType<Button>().Length, Is.EqualTo(9));
        Text Label(string name) => Object.FindObjectsOfType<Text>().Single(t => t.name == name);
        Assert.That(Label("Gesture simulation").text, Does.Contain("SIMULATED MIDI").And.Not.Contain("SIMULATED right hand"));
        CapturePreview();
        var start = Object.FindObjectsOfType<Button>().Single(b => b.name == "Start");
        start.onClick.Invoke(); yield return null;
        Assert.That(controller.State, Is.EqualTo(SessionState.Playing));
        controller.TogglePause(); yield return new WaitForSecondsRealtime(0.15f);
        Assert.That(controller.State, Is.EqualTo(SessionState.Paused));
        controller.ToggleAutomaticNotes(); Assert.That(controller.AutomaticNotes, Is.True, "Cannot change simulation during a paused trial.");
        controller.TogglePause();
        yield return new WaitForSecondsRealtime(7.3f); yield return null;
        Assert.That(controller.State, Is.EqualTo(SessionState.Complete));
        Assert.That(Label("Feedback").text, Does.Contain("Total 5 | Correct 5 | Missed 0"));
        controller.ToggleAutomaticNotes(); Assert.That(controller.AutomaticNotes, Is.False);
        controller.RestartLesson(); yield return null;
        Assert.That(controller.State, Is.EqualTo(SessionState.Playing));
        Assert.That(Label("Feedback").text, Does.Contain("Get ready"));
        yield return new ExitPlayMode(); LogAssert.NoUnexpectedReceived();
    }
    internal static void CapturePreview(string filename = "QuestLessonPreview.png")
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../QUEST_GESTURE_BUILD_COPY"))) return;
        Canvas.ForceUpdateCanvases();
        var camera = Camera.main; var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
        var target = new RenderTexture(1024, 1024, 24); var pixels = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../" + filename), pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            Object.DestroyImmediate(pixels); Object.DestroyImmediate(target);
        }
    }
}
