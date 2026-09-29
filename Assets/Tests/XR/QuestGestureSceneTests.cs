using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMidi;

public sealed class QuestGestureSceneTests
{
    [UnityTest]
    public IEnumerator SceneInitializesWithoutPretendingEditorHasTrackedHands()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/QuestGestureTest.unity");
        yield return new EnterPlayMode();
        yield return null;
        yield return null;
        var provider = Object.FindObjectOfType<QuestHandGestureProvider>();
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.SubsystemRunning, Is.False, "This smoke test expects the Mac editor without a hardware provider.");
        Assert.That(provider.LeftTracked, Is.False);
        Assert.That(provider.RightTracked, Is.False);
        Assert.That(provider.IsPinching(Hand.Left), Is.False);
        var text = Object.FindObjectsOfType<Text>().Single(t => t.name == "Status text");
        // Batch-mode Play Mode may start unfocused; both states must report no hands.
        Assert.That(text.text, Does.Contain("No running XR Hands subsystem").Or.Contain("Application not focused / suspended"));
        Assert.That(text.text, Does.Contain("NOT TRACKED"));
        provider.enabled = false; yield return null;
        provider.enabled = true; yield return null;
        Assert.That(provider.IsPinching(Hand.Right), Is.False);
        yield return new ExitPlayMode();
        LogAssert.NoUnexpectedReceived();
    }
}
