using System;
using UnityEngine;

namespace XRMidi
{
    public sealed class RhythmDemoController : MonoBehaviour
    {
        [SerializeField] private TextAsset lessonJson;
        [SerializeField] private FallingNotePanel panel;
        [SerializeField] private TimingWindows timingWindows = new TimingWindows();
        [SerializeField] private bool pinchStartsLesson = true;
        private readonly KeyCode[] keys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G };
        private readonly int[] pitches = { 60, 62, 64, 65, 67 };
        private LessonSession session;
        private SimulatedMidiInput midi;
        private SimulatedGestureProvider gestures;
        private readonly DemoFeedback feedback = new DemoFeedback();
        private bool initialized;

        public void Configure(TextAsset asset, FallingNotePanel display) { lessonJson = asset; panel = display; }
        private void Start()
        {
            try
            {
                if (lessonJson == null || panel == null) throw new InvalidOperationException("Assign the lesson JSON and panel.");
                var repository = new JsonLessonRepository(lessonJson.text);
                var lesson = repository.Load();
                panel.Initialize(this); panel.Reset(lesson);
                var clock = new MusicalClock(() => Time.realtimeSinceStartupAsDouble);
                midi = new SimulatedMidiInput(clock);
                gestures = new SimulatedGestureProvider();
                session = new LessonSession(repository, midi, clock, new OnsetEvaluator(timingWindows), panel.ApproachSeconds);
                session.Started += OnStarted; session.Completed += OnCompleted;
                session.Evaluator.Evaluated += OnEvaluated;
                gestures.GestureStarted += OnGesture;
                initialized = true;
                RefreshPanel();
            }
            catch (Exception e) { Debug.LogError("Rhythm demo initialization: " + e.Message, this); enabled = false; }
        }
        private void OnStarted(Lesson lesson) { panel.Reset(lesson); feedback.Reset(); }
        private void OnCompleted(PerformanceSummary summary) { feedback.Complete(summary); }
        private void OnEvaluated(PerformanceResult result) { feedback.Show(result); panel.ApplyFeedback(result); }
        private void OnGesture(GestureEvent gesture)
        {
            if (pinchStartsLesson && session.State == SessionState.Ready) StartLesson();
        }
        public void StartLesson()
        {
            if (!initialized || session.State == SessionState.Playing || session.State == SessionState.Paused) return;
            RestartLesson();
        }
        public void RestartLesson()
        {
            if (!initialized) return;
            try { session.Restart(); }
            catch (Exception e) { feedback.SetError("Cannot start: " + e.Message); Debug.LogError(e.Message, this); }
        }
        public void TogglePause()
        {
            if (!initialized) return;
            if (session.State == SessionState.Playing) session.Pause(); else session.Resume();
        }
        public void PlayNote(int pitch)
        {
            if (!initialized) return;
            midi.Send(pitch, true); midi.Send(pitch, false, 0);
        }
        private void Update()
        {
            if (!initialized) return;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (session.State == SessionState.Ready || session.State == SessionState.Complete) StartLesson(); else TogglePause();
            }
            if (Input.GetKeyDown(KeyCode.R)) RestartLesson();
            if (Input.GetKeyDown(KeyCode.T)) gestures.SetTracked(Hand.Right, !gestures.RightTracked);
            gestures.SetPinch(Hand.Right, Input.GetKey(KeyCode.P));
            // All demo input callbacks happen on the Unity main thread before expiration.
            for (int i = 0; i < keys.Length; i++)
            {
                if (Input.GetKeyDown(keys[i])) midi.Send(pitches[i], true);
                if (Input.GetKeyUp(keys[i])) midi.Send(pitches[i], false, 0);
            }
        }
        private void LateUpdate()
        {
            if (!initialized) return;
            // UI clicks and keyboard input are processed before this frame's miss sweep.
            session.Tick(); RefreshPanel();
        }
        private void RefreshPanel()
        {
            if (session.State != SessionState.Ready) panel.Render(session.Clock.ElapsedSeconds, session.Evaluator);
            panel.SetStatus(session.State, session.Clock.ElapsedSeconds, session.Evaluator.EndSeconds,
                feedback.Text, gestures.RightTracked);
        }
        private void OnApplicationFocus(bool focused) { if (!focused && initialized) session.Pause(); }
        private void OnDisable()
        {
            if (!initialized) return;
            session.Pause(); gestures.SetTracked(Hand.Left, false); gestures.SetTracked(Hand.Right, false);
        }
        private void OnEnable()
        {
            if (!initialized) return;
            gestures.SetTracked(Hand.Left, true); gestures.SetTracked(Hand.Right, true);
        }
        private void OnDestroy()
        {
            if (session == null) return;
            session.Started -= OnStarted; session.Completed -= OnCompleted;
            session.Evaluator.Evaluated -= OnEvaluated; session.Dispose();
            if (gestures != null) gestures.GestureStarted -= OnGesture;
        }
    }
}
