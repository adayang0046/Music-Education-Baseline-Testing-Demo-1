using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XRMidi
{
    // Lightweight uGUI presentation. Coordinates are local Canvas units, not musical state.
    public sealed class FallingNotePanel : MonoBehaviour, INoteVisualizer
    {
        [SerializeField, Min(0.1f)] private float approachSeconds = 2;
        [SerializeField, Min(1)] private float fallDistance = 240;
        [SerializeField, Min(1)] private float laneSpacing = 125;
        [SerializeField] private float hitLineY = -115;
        [SerializeField] private Vector2 noteDimensions = new Vector2(76, 22);
        private readonly int[] pitches = { 60, 62, 64, 65, 67 };
        private readonly string[] labels = { "C4 / A", "D4 / S", "E4 / D", "F4 / F", "G4 / G" };
        private readonly List<Image> noteImages = new List<Image>();
        private Lesson lesson;
        private RectTransform root, noteRoot;
        private Text title, status, feedback, gesture, pauseLabel;
        private Image progress;
        private Font font;
        private double flashUntil;
        private bool desktopControls;
        private readonly Color green = new Color32(125, 161, 114, 255);
        private readonly Color yellow = new Color32(219, 185, 59, 255);
        public double ApproachSeconds => approachSeconds;

        public void Initialize(RhythmDemoController controller, bool useDesktopControls = true)
        {
            desktopControls = useDesktopControls;
            if (approachSeconds <= 0 || !Numbers.Finite(approachSeconds) || laneSpacing <= 0 || fallDistance <= 0 || noteDimensions.x <= 0 || noteDimensions.y <= 0)
                throw new ArgumentException("Panel dimensions and approach time must be positive.");
            root = (RectTransform)transform;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Box("Background", root, Vector2.zero, new Vector2(900, 760), new Color32(34, 34, 34, 255));
            title = Label("Title", new Vector2(0, 335), new Vector2(850, 45), 28);
            status = Label("Status", new Vector2(0, 292), new Vector2(850, 35), 20);
            for (int i = 0; i < pitches.Length; i++)
            {
                float x = (i - 2) * laneSpacing;
                Box("Lane", root, new Vector2(x, hitLineY + fallDistance / 2), new Vector2(laneSpacing - 5, fallDistance + 55), new Color32(44, 44, 44, 255));
                int pitch = pitches[i];
                var keyLabel = Button(desktopControls ? labels[i] : $"MIDI {pitch}", new Vector2(x, hitLineY - 43), new Vector2(laneSpacing - 8, 45), () => controller.PlayNote(pitch));
                keyLabel.GetComponentInParent<Button>().interactable = controller.AllowSimulatedNotes;
            }
            Box("Hit line", root, new Vector2(0, hitLineY), new Vector2(laneSpacing * 5, 4), green);
            noteRoot = new GameObject("Scheduled notes", typeof(RectTransform)).GetComponent<RectTransform>();
            noteRoot.SetParent(root, false);
            feedback = Label("Feedback", new Vector2(0, -219), new Vector2(850, 72), 20);
            progress = Box("Progress", root, new Vector2(0, -270), new Vector2(700, 8), green);
            Button("Start", new Vector2(-220, -312), new Vector2(190, 45), controller.StartLesson);
            pauseLabel = Button("Pause", new Vector2(0, -312), new Vector2(190, 45), controller.TogglePause);
            Button("Restart", new Vector2(220, -312), new Vector2(190, 45), controller.RestartLesson);
            gesture = Label("Gesture simulation", new Vector2(0, -355), new Vector2(870, 30), 15);
        }
        public void Reset(Lesson source)
        {
            lesson = source.Snapshot();
            foreach (var note in lesson.notes)
                if (Array.IndexOf(pitches, note.pitch) < 0) throw new ArgumentException("The demo panel supports only C4, D4, E4, F4, G4.");
            foreach (var image in noteImages) { image.gameObject.SetActive(false); Destroy(image.gameObject); }
            noteImages.Clear(); flashUntil = 0;
            title.text = lesson.title;
            foreach (var note in lesson.notes)
            {
                int lane = Array.IndexOf(pitches, note.pitch);
                var image = Box("MIDI " + note.pitch, noteRoot, new Vector2((lane - 2) * laneSpacing, 0), noteDimensions, green);
                image.gameObject.SetActive(false); noteImages.Add(image);
            }
            progress.rectTransform.localScale = new Vector3(0, 1, 1);
        }
        public void Render(double elapsedSeconds, IPerformanceEvaluator evaluator)
        {
            for (int i = 0; i < lesson.notes.Count; i++)
            {
                double hit = lesson.notes[i].startBeat * 60 / lesson.tempo;
                bool visible = !evaluator.IsResolved(i) && hit - elapsedSeconds <= approachSeconds;
                var image = noteImages[i]; image.gameObject.SetActive(visible);
                if (!visible) continue;
                var position = image.rectTransform.anchoredPosition;
                position.y = (float)NoteTrajectory.Position(hit, elapsedSeconds, approachSeconds, fallDistance, hitLineY);
                image.rectTransform.anchoredPosition = position;
            }
        }
        public void ApplyFeedback(PerformanceResult result)
        {
            feedback.color = result.IsCorrect ? green : yellow;
            flashUntil = Time.realtimeSinceStartupAsDouble + 0.3;
        }
        public void SetStatus(SessionState state, double elapsed, double end, string message, bool rightTracked)
        {
            string time = elapsed < 0 ? $"Ready in {-elapsed:0.0}s" : $"Beat {elapsed * lesson.tempo / 60:0.00}";
            status.text = $"{state}  |  {lesson.tempo:0} BPM  |  {time}" + (desktopControls ? "  |  Space: start/pause, R: restart" : "");
            feedback.text = message;
            if (Time.realtimeSinceStartupAsDouble > flashUntil) feedback.color = Color.white;
            float fraction = state == SessionState.Complete ? 1 : end > 0 ? Mathf.Clamp01((float)(elapsed / end)) : 0;
            progress.rectTransform.localScale = new Vector3(fraction, 1, 1);
            pauseLabel.text = state == SessionState.Paused ? "Resume" : "Pause";
            gesture.text = $"SIMULATED right hand: {(rightTracked ? "available" : "lost")} | P: hold pinch to start | T: toggle tracking";
        }
        public void SetInputDescription(string message) { gesture.text = message; }
        private Image Box(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false); image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size; image.color = color; image.raycastTarget = false; return image;
        }
        private Text Label(string name, Vector2 position, Vector2 size, int fontSize)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(root, false); text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
            text.font = font; text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false; return text;
        }
        private Text Button(string caption, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var image = Box(caption, root, position, size, new Color32(64, 75, 61, 255)); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            var label = Label(caption + " label", position, size, 20); label.text = caption;
            label.transform.SetParent(image.transform, true); return label;
        }
    }
}
