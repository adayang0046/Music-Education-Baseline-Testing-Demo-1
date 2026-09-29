using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Hands;

namespace XRMidi
{
    // A test display only. No lesson scoring or physical key-contact inference.
    public sealed class QuestGestureDiagnostics : MonoBehaviour
    {
        [SerializeField] private QuestHandGestureProvider provider;
        [SerializeField] private Transform trackingSpace;
        [SerializeField] private Text statusText;
        [SerializeField] private Material jointMaterial;
        private readonly Transform[,] markers = new Transform[2, 26];
        private readonly Renderer[,] renderers = new Renderer[2, 26];
        private readonly int[] starts = new int[2], ends = new int[2], cancellations = new int[2];
        private MaterialPropertyBlock color;
        private string lastEvent = "Open your hands, then pinch thumb and index together.";
        public void Configure(QuestHandGestureProvider source, Transform origin, Text display, Material material)
        { provider = source; trackingSpace = origin; statusText = display; jointMaterial = material; }
        private void Awake()
        {
            color = new MaterialPropertyBlock();
            if (provider == null || trackingSpace == null || statusText == null || jointMaterial == null)
            { Debug.LogError("Assign provider, tracking space, status text and joint material.", this); enabled = false; return; }
            for (int h = 0; h < 2; h++)
                for (int i = 0; i < 26; i++)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.name = ((Hand)h) + " " + XRHandJointIDUtility.FromIndex(i);
                    marker.transform.SetParent(trackingSpace, false);
                    marker.transform.localScale = Vector3.one * 0.012f;
                    var collider = marker.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
                    markers[h, i] = marker.transform; renderers[h, i] = marker.GetComponent<Renderer>();
                    renderers[h, i].sharedMaterial = jointMaterial; marker.SetActive(false);
                }
        }
        private void OnEnable()
        {
            if (provider == null) return;
            provider.GestureStarted += OnStarted; provider.GestureEnded += OnEnded;
        }
        private void OnDisable()
        {
            if (provider != null) { provider.GestureStarted -= OnStarted; provider.GestureEnded -= OnEnded; }
            foreach (var marker in markers) if (marker != null) marker.gameObject.SetActive(false);
        }
        private void OnDestroy() { foreach (var marker in markers) if (marker != null) Destroy(marker.gameObject); }
        private void OnStarted(GestureEvent e)
        { starts[(int)e.Hand]++; lastEvent = e.Hand + " pinch START"; Debug.Log("[QuestGesture] " + lastEvent); }
        private void OnEnded(GestureEvent e)
        {
            if (e.Cancelled) cancellations[(int)e.Hand]++; else ends[(int)e.Hand]++;
            lastEvent = e.Hand + (e.Cancelled ? " pinch CANCEL (tracking/focus loss)" : " pinch END");
            Debug.Log("[QuestGesture] " + lastEvent);
        }
        private void LateUpdate()
        {
            for (int h = 0; h < 2; h++)
                for (int i = 0; i < 26; i++)
                {
                    bool available = provider.TryGetJointPose((Hand)h, XRHandJointIDUtility.FromIndex(i), out var pose);
                    markers[h, i].gameObject.SetActive(available);
                    if (!available) continue;
                    markers[h, i].localPosition = pose.position; markers[h, i].localRotation = pose.rotation;
                    color.SetColor("_Color", provider.IsPinching((Hand)h) ? Color.yellow : h == 0 ? Color.cyan : Color.green);
                    renderers[h, i].SetPropertyBlock(color);
                }
            statusText.text = "QUEST HAND TRACKING TEST\n" + provider.Status + "\n\n" + HandStatus(Hand.Left) + "\n\n" + HandStatus(Hand.Right) +
                "\n\n" + lastEvent + "\n\nCyan = left   Green = right   Yellow = pinch\nOpen hands before each test. Hold pinch: one start only.\nHide a pinching hand: expect cancellation.\nJoint estimates do not prove physical key contact.";
        }
        private string HandStatus(Hand hand)
        {
            int i = (int)hand; float distance = provider.PinchDistance(hand);
            bool tracked = hand == Hand.Left ? provider.LeftTracked : provider.RightTracked;
            string gap = float.IsNaN(distance) ? "tips unavailable" : $"{distance * 1000:0} mm thumb-index gap";
            return $"{hand}: {(tracked ? "TRACKED" : "NOT TRACKED")} | {(provider.IsPinching(hand) ? "PINCH" : "open / waiting")}\n" +
                $"{gap} | starts {starts[i]} | ends {ends[i]} | cancelled {cancellations[i]}";
        }
    }
}
