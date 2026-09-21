using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XRMidi;

public static class Milestone1SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Milestone1Demo.unity";
    [MenuItem("XR MIDI/Create Milestone 1 Demo Scene")]
    public static void Create()
    {
        if (File.Exists(ScenePath))
        {
            Debug.Log("Demo scene already exists. Open " + ScenePath + "; existing work was not overwritten.");
            return;
        }
        // Additive creation preserves the currently open scene, including unsaved work.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var camera = new GameObject("Demo camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -3);
        camera.orthographic = true; camera.orthographicSize = 0.9f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(26, 26, 26, 255);
        var canvas = new GameObject("Floating lesson panel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(900, 760);
        canvas.transform.localScale = Vector3.one * 0.002f;
        var panel = canvas.gameObject.AddComponent<FallingNotePanel>();
        var controller = new GameObject("Rhythm lesson composition").AddComponent<RhythmDemoController>();
        controller.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Lessons/rhythm_five_notes.json"), panel);
        new GameObject("Demo event system", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Created " + ScenePath);
    }
}
