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
        Text FindText(string name) => Object.FindObjectsOfType<Text>().Single(t => t.name == name);
        Assert.That(FindText("Title").text, Is.EqualTo("C Major - Five Quarter Notes"));
        Assert.That(Object.FindObjectsOfType<Button>().Length, Is.EqualTo(8));
        controller.StartLesson(); yield return null;
        Assert.That(FindText("Status").text, Does.Contain("Playing"));
        controller.TogglePause(); yield return null;
        string paused = FindText("Status").text;
        controller.PlayNote(60); yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(FindText("Status").text, Is.EqualTo(paused));
        Assert.That(FindText("Start label"), Is.Not.Null);
        Assert.That(FindText("Pause label").text, Is.EqualTo("Resume"));
        controller.TogglePause(); yield return null;
        Assert.That(FindText("Status").text, Does.Contain("Playing"));
        controller.RestartLesson(); yield return null;
        Assert.That(FindText("Status").text, Does.Contain("Ready in"));
        yield return new WaitForSecondsRealtime(7.2f);
        yield return null;
        Assert.That(FindText("Feedback").text, Does.Contain("Total 5 | Correct 0 | Missed 5"));
        controller.RestartLesson(); yield return null;
        Assert.That(FindText("Feedback").text, Does.Contain("Get ready"));
        Assert.That(FindText("Status").text, Does.Contain("Playing"));
        yield return new ExitPlayMode();
        LogAssert.NoUnexpectedReceived();
    }
}
