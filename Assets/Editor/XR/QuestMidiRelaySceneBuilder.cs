using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using XRMidi;

public static class QuestMidiRelaySceneBuilder
{
    public const string ScenePath = "Assets/Scenes/QuestMidiRelayDemo.unity";
    [MenuItem("XR MIDI/Quest/Create MIDI Relay Scene")]
    public static void Create()
    {
        if (File.Exists(ScenePath)) { Debug.Log("Existing relay scene preserved."); return; }
        if (!File.Exists(QuestLessonSceneBuilder.ScenePath)) { Debug.LogError("Create the Quest lesson demo first."); return; }
        if (!Application.isBatchMode && string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
        { Debug.LogError("Save the current untitled scene first."); return; }
        // Copy through the AssetDatabase to preserve the original scene and references.
        if (!AssetDatabase.CopyAsset(QuestLessonSceneBuilder.ScenePath, ScenePath)) throw new IOException("Could not copy the lesson scene.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var roots = scene.GetRootGameObjects();
        var controller = roots.SelectMany(r => r.GetComponentsInChildren<RhythmDemoController>()).Single();
        var hands = roots.SelectMany(r => r.GetComponentsInChildren<QuestHandGestureProvider>()).Single();
        var canvas = roots.SelectMany(r => r.GetComponentsInChildren<Canvas>()).Single();
        controller.ConfigureExternalMidi("LIVE MIDI: laptop Wi-Fi relay");
        var display = new GameObject("MIDI and gesture status", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        display.transform.SetParent(canvas.transform, false);
        display.rectTransform.anchoredPosition = new Vector2(0, 445); display.rectTransform.sizeDelta = new Vector2(1100, 100);
        display.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); display.fontSize = 21;
        display.color = Color.white; display.alignment = TextAnchor.MiddleCenter; display.raycastTarget = false;
        var relay = new GameObject("Quest MIDI relay receiver").AddComponent<QuestMidiRelay>();
        relay.Configure(controller, hands, display);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }
}
