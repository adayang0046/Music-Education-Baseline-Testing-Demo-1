using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using XRMidi;

public static class QuestGestureSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/QuestGestureTest.unity";
    [MenuItem("XR MIDI/Quest/Create Gesture Test Scene")]
    public static void Create()
    {
        if (File.Exists(ScenePath)) { Debug.Log("Open " + ScenePath + ". Existing scene was not overwritten."); return; }
        if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
        {
            if (!Application.isBatchMode) { Debug.LogError("Save your current untitled scene first."); return; }
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var rig = new GameObject("Gesture test XR Origin").AddComponent<XROrigin>();
        var offset = new GameObject("Camera offset - shared tracking space"); offset.transform.SetParent(rig.transform, false);
        var camera = new GameObject("XR Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.transform.SetParent(offset.transform, false); camera.tag = "MainCamera";
        camera.nearClipPlane = 0.03f; camera.farClipPlane = 30;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
        var driver = camera.gameObject.AddComponent<TrackedPoseDriver>();
        driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
        driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        rig.Camera = camera; rig.CameraFloorOffsetObject = offset;
        rig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device; rig.CameraYOffset = 1.4f;
        var source = new GameObject("Quest XR Hands provider").AddComponent<QuestHandGestureProvider>();
        var canvas = new GameObject("Gesture status", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
        canvas.transform.position = new Vector3(0, 1.5f, 1.2f); canvas.transform.localScale = Vector3.one * 0.0015f;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(800, 540);
        var text = new GameObject("Status text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(canvas.transform, false); text.rectTransform.sizeDelta = new Vector2(800, 540);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 26;
        text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        text.text = "QUEST HAND TRACKING TEST\nBuild for Quest to receive real hand data.";
        const string materialPath = "Assets/QuestGestureJoint.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Unlit/Color")); material.color = Color.white;
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var display = new GameObject("Gesture diagnostics").AddComponent<QuestGestureDiagnostics>();
        display.Configure(source, offset.transform, text, material);
        EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
        Debug.Log("Created " + ScenePath + ". Android OpenXR setup is still required; see Docs/QuestGestureTest.md.");
    }
}
