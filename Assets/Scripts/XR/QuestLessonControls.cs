using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XRMidi
{
    // Head-gaze dwell is an explicit fallback independent of hand tracking.
    // GraphicRaycaster targets the same ordinary Buttons used by mouse input.
    public sealed class QuestLessonControls : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private GraphicRaycaster raycaster;
        [SerializeField] private EventSystem events;
        [SerializeField] private RhythmDemoController lesson;
        [SerializeField] private Text hint;
        [SerializeField] private Text autoLabel;
        [SerializeField] private Button autoButton;
        [SerializeField, Min(0.1f)] private float dwellSeconds = 0.8f;
        private DwellSelection dwell;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private bool focused = true, suspended;
        private bool ordered;
        public void Configure(Camera camera, GraphicRaycaster caster, EventSystem eventSystem,
            RhythmDemoController controller, Text status, Text label, Button button)
        { view = camera; raycaster = caster; events = eventSystem; lesson = controller; hint = status; autoLabel = label; autoButton = button; }
        private void Awake() { dwell = new DwellSelection(dwellSeconds); }
        private void Start() { autoButton.onClick.AddListener(lesson.ToggleAutomaticNotes); }
        private void Update()
        {
            autoLabel.text = lesson.AllowSimulatedNotes ? "Auto notes: " + (lesson.AutomaticNotes ? "ON (test)" : "OFF (manual)") : lesson.ExternalMidiLabel;
            autoButton.interactable = lesson.AllowSimulatedNotes && (lesson.State == SessionState.Ready || lesson.State == SessionState.Complete);
            if (!focused || suspended) { dwell.Reset(); hint.text = "Paused while application is not focused"; return; }
            var pointer = new PointerEventData(events) { position = view.pixelRect.center };
            hits.Clear(); raycaster.Raycast(pointer, hits);
            Button target = null;
            foreach (var hit in hits)
            {
                var candidate = hit.gameObject.GetComponentInParent<Button>();
                if (candidate != null && candidate.IsActive() && candidate.IsInteractable()) { target = candidate; break; }
            }
            bool activate = dwell.Update(target == null ? 0 : target.GetInstanceID(), Time.unscaledDeltaTime);
            hint.text = target == null ? "Look at a button for 0.8 seconds to select. Pinch starts a Ready lesson."
                : $"Look: {target.name}  {dwell.Progress:P0} — look away before selecting again";
            if (activate && target != null) target.onClick.Invoke();
        }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) dwell?.Reset(); }
        private void LateUpdate()
        {
            if (ordered) return;
            // The reused lesson panel creates its background in Start. Place
            // scene-authored controls above it only after all Start calls finish.
            autoButton.transform.SetAsLastSibling(); hint.transform.SetAsLastSibling(); ordered = true;
        }
        private void OnApplicationPause(bool value) { suspended = value; if (value) dwell?.Reset(); }
        private void OnDisable() { dwell?.Reset(); }
        private void OnDestroy() { if (autoButton != null && lesson != null) autoButton.onClick.RemoveListener(lesson.ToggleAutomaticNotes); }
    }
}
