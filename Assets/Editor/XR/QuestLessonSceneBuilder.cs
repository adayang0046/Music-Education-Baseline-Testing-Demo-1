using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using XRMidi;

public static class QuestLessonSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/QuestLessonDemo.unity";
    [MenuItem("XR MIDI/Quest/Create Lesson Demo Scene")]
    public static void Create()
    {
        if (File.Exists(ScenePath)) { Debug.Log("Existing scene preserved: " + ScenePath); return; }
        if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
        {
            if (!Application.isBatchMode) { Debug.LogError("Save your current scene before creating the Quest lesson."); return; }
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var rig = new GameObject("Lesson XR Origin").AddComponent<XROrigin>();
        var offset = new GameObject("Camera offset"); offset.transform.SetParent(rig.transform, false);
        var camera = new GameObject("Quest camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.transform.SetParent(offset.transform, false); camera.tag = "MainCamera";
        camera.nearClipPlane = 0.03f; camera.farClipPlane = 30;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
        var driver = camera.gameObject.AddComponent<TrackedPoseDriver>();
        driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
        driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        rig.Camera = camera; rig.CameraFloorOffsetObject = offset;
        rig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device; rig.CameraYOffset = 1.4f;
        // XROrigin does not apply its height without a running XR subsystem.
        // Seed the same offset so the no-device Editor preview remains usable.
        offset.transform.localPosition = Vector3.up * 1.4f;
        var provider = new GameObject("Real Quest hand input").AddComponent<QuestHandGestureProvider>();
        var canvas = new GameObject("Quest lesson panel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
        canvas.transform.position = new Vector3(0, 1.4f, 1.4f); canvas.transform.localScale = Vector3.one * 0.0012f;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(900, 760);
        var panel = canvas.gameObject.AddComponent<FallingNotePanel>();
        var controller = new GameObject("Quest lesson composition").AddComponent<RhythmDemoController>();
        controller.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Lessons/rhythm_five_notes.json"), panel);
        controller.ConfigureQuest(provider);
        var eventSystem = new GameObject("Lesson events", typeof(EventSystem)).GetComponent<EventSystem>();
        // No legacy input module: the head-gaze fallback works with the new-only Android backend.
        var auto = new GameObject("Automatic test notes", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        auto.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)auto.transform; rect.anchoredPosition = new Vector2(0, 230); rect.sizeDelta = new Vector2(400, 46);
        var image = auto.GetComponent<Image>(); image.color = new Color32(64, 75, 61, 255);
        var button = auto.GetComponent<Button>(); button.targetGraphic = image;
        var autoLabel = Label(auto.transform, "Auto note label", Vector2.zero, new Vector2(400, 46), 22);
        var hint = Label(canvas.transform, "Gaze instructions", new Vector2(0, -414), new Vector2(1050, 52), 20);
        var controls = new GameObject("Head gaze button fallback").AddComponent<QuestLessonControls>();
        controls.Configure(camera, canvas.GetComponent<GraphicRaycaster>(), eventSystem, controller, hint, autoLabel, button);
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "Head gaze cursor"; Object.DestroyImmediate(dot.GetComponent<Collider>());
        dot.transform.SetParent(camera.transform, false); dot.transform.localPosition = new Vector3(0, 0, 0.5f);
        dot.transform.localScale = Vector3.one * 0.002f;
        dot.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuestGestureJoint.mat");
        EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
        Debug.Log("Created Quest lesson demo. Automatic notes are explicitly simulated; pinch starts only Ready lessons.");
    }
    private static Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false); text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize;
        text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
}
