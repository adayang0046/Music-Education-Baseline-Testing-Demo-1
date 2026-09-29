using System;
using System.Collections.Generic;
using NUnit.Framework;
using XRMidi;

public sealed class QuestLessonLogicTests
{
    private sealed class Gestures : IGestureProvider
    {
        public bool LeftTracked { get; set; } = true;
        public bool RightTracked { get; set; } = true;
        public event Action<GestureEvent> GestureStarted;
        public event Action<GestureEvent> GestureEnded;
        public void Start(Hand hand) => GestureStarted?.Invoke(new GestureEvent(hand));
        public void End(Hand hand, bool cancelled = false) => GestureEnded?.Invoke(new GestureEvent(hand, cancelled));
    }
    [Test] public void PinchStartsOnlyReadyAndHeldGestureDoesNotRepeat()
    {
        var source = new Gestures(); int starts = 0;
        using (var binding = new GestureLessonBinding(source, () => SessionState.Ready, () => starts++))
        {
            source.Start(Hand.Left); source.Start(Hand.Left); Assert.That(starts, Is.EqualTo(1));
            source.End(Hand.Left); source.Start(Hand.Left); Assert.That(starts, Is.EqualTo(2));
        }
        source.End(Hand.Left); source.Start(Hand.Right); Assert.That(starts, Is.EqualTo(2));
    }
    [TestCase(SessionState.Playing)] [TestCase(SessionState.Paused)] [TestCase(SessionState.Complete)]
    public void NonReadyPinchIsConsumedWithoutDeferredStart(SessionState initial)
    {
        var source = new Gestures(); var state = initial; int starts = 0;
        using (var binding = new GestureLessonBinding(source, () => state, () => starts++))
        {
            source.Start(Hand.Right); state = SessionState.Ready; source.Start(Hand.Right);
            Assert.That(starts, Is.Zero);
            source.End(Hand.Right, true); Assert.That(starts, Is.Zero);
            source.Start(Hand.Right); Assert.That(starts, Is.EqualTo(1));
        }
    }
    [Test] public void UntrackedHandAndCancellationCannotStartLesson()
    {
        var source = new Gestures { LeftTracked = false }; int starts = 0;
        using (var binding = new GestureLessonBinding(source, () => SessionState.Ready, () => starts++))
        {
            source.Start(Hand.Left); source.End(Hand.Right, true); Assert.That(starts, Is.Zero);
            source.Start(Hand.Right); Assert.That(starts, Is.EqualTo(1));
        }
    }
    [Test] public void TwoHandStartsDoNotRestartPlayingLesson()
    {
        var source = new Gestures(); var state = SessionState.Ready; int starts = 0;
        using (var binding = new GestureLessonBinding(source, () => state, () => { starts++; state = SessionState.Playing; }))
        { source.Start(Hand.Left); source.Start(Hand.Right); Assert.That(starts, Is.EqualTo(1)); }
    }
    [Test] public void DwellRequiresContinuousLookAndLeavingAfterActivation()
    {
        var dwell = new DwellSelection(0.8);
        Assert.That(dwell.Update(1, 0.4), Is.False);
        Assert.That(dwell.Update(2, 0.4), Is.False);
        Assert.That(dwell.Update(2, 0.4), Is.True);
        Assert.That(dwell.Update(2, 5), Is.False);
        dwell.Update(0, 0); Assert.That(dwell.Update(2, 0.8), Is.True);
        dwell.Reset(); Assert.That(dwell.Progress, Is.Zero);
    }
    [Test] public void DwellRejectsInvalidTiming()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DwellSelection(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DwellSelection(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DwellSelection(1).Update(1, -1));
    }
    private static Lesson Lesson() => new Lesson { id = "test", title = "test", tempo = 60, beatsPerBar = 4, beatUnit = 4,
        notes = new List<LessonNote> { new LessonNote { pitch = 60, startBeat = 0, durationBeats = 1 }, new LessonNote { pitch = 62, startBeat = 1, durationBeats = 1 } } };
    [Test] public void AutomaticNotesRespectLeadInOrderingAndRestart()
    {
        var simulator = new LessonNoteSimulator(); var received = new List<NoteEvent>(); simulator.Reset(Lesson());
        simulator.Advance(-1, received.Add); Assert.That(received, Is.Empty);
        simulator.Advance(0, received.Add); Assert.That(received.Count, Is.EqualTo(1));
        simulator.Advance(0, received.Add); Assert.That(received.Count, Is.EqualTo(1));
        simulator.Advance(2, received.Add); Assert.That(received.Count, Is.EqualTo(4));
        Assert.That(received[1].IsNoteOn, Is.False); Assert.That(received[2].Pitch, Is.EqualTo(62));
        Assert.That(received[2].Timestamp, Is.EqualTo(1));
        simulator.Reset(Lesson()); simulator.Advance(0, received.Add); Assert.That(received.Count, Is.EqualTo(5));
    }
    [Test] public void ScheduledInputCanCompleteLessonThroughNormalInputBoundary()
    {
        var lesson = Lesson(); double now = 0; var clock = new MusicalClock(() => now); clock.Start(60, 2);
        var input = new SimulatedMidiInput(clock); var evaluator = new OnsetEvaluator(new TimingWindows()); evaluator.Reset(lesson);
        using (var dispatch = new MainThreadMidiInput(input))
        {
            dispatch.NoteReceived += evaluator.Submit; dispatch.SetAccepting(true);
            var simulator = new LessonNoteSimulator(); simulator.Reset(lesson);
            simulator.Advance(3, input.SendScheduled); dispatch.Drain(); evaluator.Advance(3);
            Assert.That(evaluator.Summary.Correct, Is.EqualTo(2));
        }
    }
    [Test] public void SimulatorRejectsInvalidLesson()
    {
        var lesson = Lesson(); lesson.tempo = 0;
        Assert.Throws<ArgumentException>(() => new LessonNoteSimulator().Reset(lesson));
    }
}
