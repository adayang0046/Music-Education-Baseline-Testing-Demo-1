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
        [SerializeField] private bool desktopControls = true;
        [SerializeField] private MonoBehaviour gestureSource;
        [SerializeField] private bool automaticNotes;
        [SerializeField] private string externalMidiLabel;
        private readonly KeyCode[] keys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G };
        private readonly int[] pitches = { 60, 62, 64, 65, 67 };
        private LessonSession session;
        private SimulatedMidiInput midi;
        private MainThreadMidiInput inputDispatch;
        private SimulatedGestureProvider gestures;
        private IGestureProvider activeGestures;
        private GestureLessonBinding gestureBinding;
        private readonly LessonNoteSimulator noteSimulator = new LessonNoteSimulator();
        private readonly DemoFeedback feedback = new DemoFeedback();
        private bool initialized;
        public bool CanReceivePhysicalMidi => initialized && isActiveAndEnabled && session.State == SessionState.Playing;
        public string MidiLessonStatus => !initialized ? "Not initialized" : !isActiveAndEnabled ? "Disabled" : session.State.ToString();
        public SessionState State => session?.State ?? SessionState.Ready;
        public bool AutomaticNotes => automaticNotes;
        public bool AllowSimulatedNotes => string.IsNullOrEmpty(externalMidiLabel);
        public string ExternalMidiLabel => externalMidiLabel;
        public double LessonElapsedSeconds => session?.Clock.ElapsedSeconds ?? 0;

        public void Configure(TextAsset asset, FallingNotePanel display) { lessonJson = asset; panel = display; }
        public void ConfigureQuest(MonoBehaviour source)
        { gestureSource = source; desktopControls = false; automaticNotes = true; }
        public void ConfigureExternalMidi(string label) { externalMidiLabel = label; automaticNotes = false; }
        private void Start()
        {
            try
            {
                if (lessonJson == null || panel == null) throw new InvalidOperationException("Assign the lesson JSON and panel.");
                var repository = new JsonLessonRepository(lessonJson.text);
                var lesson = repository.Load();
                panel.Initialize(this, desktopControls); panel.Reset(lesson);
                var clock = new MusicalClock(() => Time.realtimeSinceStartupAsDouble);
                midi = new SimulatedMidiInput(clock);
                inputDispatch = new MainThreadMidiInput(midi);
                gestures = new SimulatedGestureProvider();
                if (!desktopControls && gestureSource == null) throw new InvalidOperationException("Assign a real gesture provider for the Quest lesson.");
                activeGestures = gestureSource == null ? gestures : gestureSource as IGestureProvider;
                if (activeGestures == null) throw new InvalidOperationException("Gesture source must implement IGestureProvider.");
                session = new LessonSession(repository, inputDispatch, clock, new OnsetEvaluator(timingWindows), panel.ApproachSeconds);
                session.Started += OnStarted; session.Completed += OnCompleted;
                session.Evaluator.Evaluated += OnEvaluated;
                if (pinchStartsLesson) gestureBinding = new GestureLessonBinding(activeGestures,
                    () => isActiveAndEnabled ? session.State : SessionState.Paused, StartLesson);
                initialized = true;
                RefreshPanel();
            }
            catch (Exception e) { Debug.LogError("Rhythm demo initialization: " + e.Message, this); enabled = false; }
        }
        private void OnStarted(Lesson lesson) { inputDispatch.SetAccepting(true); noteSimulator.Reset(lesson); panel.Reset(lesson); feedback.Reset(); }
        private void OnCompleted(PerformanceSummary summary) { feedback.Complete(summary); }
        private void OnEvaluated(PerformanceResult result) { feedback.Show(result); panel.ApplyFeedback(result); }
        public void ToggleAutomaticNotes()
        {
            if (AllowSimulatedNotes && (State == SessionState.Ready || State == SessionState.Complete)) automaticNotes = !automaticNotes;
        }
        public void StartLesson()
        {
            if (!initialized || session.State == SessionState.Playing || session.State == SessionState.Paused) return;
            RestartLesson();
        }
        public void RestartLesson()
        {
            if (!initialized) return;
            inputDispatch.SetAccepting(false);
            try { session.Restart(); }
            catch (Exception e) { feedback.SetError("Cannot start: " + e.Message); Debug.LogError(e.Message, this); }
        }
        public void TogglePause()
        {
            if (!initialized) return;
            if (session.State == SessionState.Playing) session.Pause(); else session.Resume();
            inputDispatch.SetAccepting(session.State == SessionState.Playing);
        }
        public void PlayNote(int pitch)
        {
            if (!initialized || !AllowSimulatedNotes) return;
            midi.Send(pitch, true); midi.Send(pitch, false, 0);
        }
        // Called by a main-thread hardware adapter; receipt time uses the same
        // lesson clock as simulation. Ignore input outside an active lesson.
        public bool ReceivePhysicalMidi(int pitch, bool on, int velocity, int channel)
        {
            if (!CanReceivePhysicalMidi) return false;
            midi.Send(pitch, on, velocity, channel);
            return true;
        }
        private void Update()
        {
            if (!initialized || !desktopControls) return;
#if ENABLE_LEGACY_INPUT_MANAGER
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
#endif
        }
        private void LateUpdate()
        {
            if (!initialized) return;
            // UI clicks and keyboard input are processed before this frame's miss sweep.
            if (automaticNotes && session.State == SessionState.Playing)
                noteSimulator.Advance(session.Clock.ElapsedSeconds, midi.SendScheduled);
            inputDispatch.Drain(); session.Tick();
            if (session.State != SessionState.Playing) inputDispatch.SetAccepting(false);
            RefreshPanel();
        }
        private void RefreshPanel()
        {
            if (session.State != SessionState.Ready) panel.Render(session.Clock.ElapsedSeconds, session.Evaluator);
            panel.SetStatus(session.State, session.Clock.ElapsedSeconds, session.Evaluator.EndSeconds,
                !desktopControls && session.State == SessionState.Ready
                    ? "Open hands, then pinch to start. Or look at Start." : feedback.Text, activeGestures.RightTracked);
            if (!desktopControls)
                panel.SetInputDescription($"{(AllowSimulatedNotes ? "SIMULATED MIDI: " + (automaticNotes ? "AUTO NOTES ON" : "manual note buttons") : externalMidiLabel)} | Hands L:{activeGestures.LeftTracked} R:{activeGestures.RightTracked} | Pinch starts Ready lesson");
        }
        public void PauseLesson()
        {
            if (!initialized) return;
            session.Pause(); inputDispatch.SetAccepting(false);
        }
        private void OnApplicationFocus(bool focused)
        {
            if (focused || !initialized) return;
            session.Pause(); inputDispatch.SetAccepting(false);
        }
        private void OnApplicationPause(bool paused) { if (paused) OnApplicationFocus(false); }
        private void OnDisable()
        {
            if (!initialized) return;
            session.Pause(); inputDispatch.SetAccepting(false);
            gestures.SetTracked(Hand.Left, false); gestures.SetTracked(Hand.Right, false);
        }
        private void OnEnable()
        {
            if (!initialized) return;
            gestures.SetTracked(Hand.Left, true); gestures.SetTracked(Hand.Right, true);
        }
        private void OnDestroy()
        {
            inputDispatch?.Dispose();
            if (session == null) return;
            session.Started -= OnStarted; session.Completed -= OnCompleted;
            session.Evaluator.Evaluated -= OnEvaluated; session.Dispose();
            gestureBinding?.Dispose();
        }
    }
}
