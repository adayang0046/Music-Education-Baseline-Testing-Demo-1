using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XRMidi;
using SessionState = XRMidi.SessionState;

public sealed class RhythmCoreTests
{
    private sealed class Repository : ILessonRepository
    {
        public Lesson Lesson;
        public Lesson Load() => Lesson;
    }
    private sealed class InputSource : IMidiInput
    {
        public event Action<NoteEvent> NoteReceived;
        public void Send(int pitch, double seconds, bool on = true, int velocity = 100)
            => NoteReceived?.Invoke(new NoteEvent(pitch, on, velocity, seconds));
    }
    private static Lesson MakeLesson(params int[] pitches)
    {
        var lesson = new Lesson { id = "test", title = "Test", tempo = 60, beatsPerBar = 4,
            beatUnit = 4, notes = new List<LessonNote>() };
        for (int i = 0; i < pitches.Length; i++) lesson.notes.Add(new LessonNote { pitch = pitches[i], startBeat = i, durationBeats = 1 });
        return lesson;
    }
    private static OnsetEvaluator Evaluator(Lesson lesson)
    {
        var evaluator = new OnsetEvaluator(new TimingWindows()); evaluator.Reset(lesson); return evaluator;
    }

    [Test]
    public void ActualJsonLoadsFiveQuarterNotes()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Lessons/rhythm_five_notes.json");
        Assert.That(asset, Is.Not.Null);
        var lesson = new JsonLessonRepository(asset.text).Load();
        Assert.That(lesson.tempo, Is.EqualTo(60));
        Assert.That(lesson.notes.ConvertAll(n => n.pitch), Is.EqualTo(new[] { 60, 62, 64, 65, 67 }));
        for (int i = 0; i < 5; i++)
        { Assert.That(lesson.notes[i].startBeat, Is.EqualTo(i)); Assert.That(lesson.notes[i].durationBeats, Is.EqualTo(1)); }
    }
    [TestCase("")]
    [TestCase("{")]
    [TestCase("{}")]
    [TestCase("null")]
    public void InvalidJsonFails(string json) => Assert.Throws<ArgumentException>(() => new JsonLessonRepository(json).Load());

    [Test]
    public void ValidationRejectsNullNotesInvalidPitchAndNonfiniteTimes()
    {
        var lesson = MakeLesson(60); lesson.notes[0] = null;
        Assert.Throws<ArgumentException>(() => lesson.Validate());
        lesson = MakeLesson(128); Assert.Throws<ArgumentException>(() => lesson.Validate());
        lesson = MakeLesson(60); lesson.tempo = double.NaN;
        Assert.Throws<ArgumentException>(() => lesson.Validate());
        lesson = MakeLesson(60); lesson.notes[0].durationBeats = double.PositiveInfinity;
        Assert.Throws<ArgumentException>(() => lesson.Validate());
        lesson = MakeLesson(60); lesson.notes[0].startBeat = -1;
        Assert.Throws<ArgumentException>(() => lesson.Validate());
        lesson = MakeLesson(60); lesson.notes.Clear();
        Assert.Throws<ArgumentException>(() => lesson.Validate());
    }
    [Test]
    public void ClockConvertsQuarterNoteBeatsAndIncludesLeadIn()
    {
        double now = 50; var clock = new MusicalClock(() => now); clock.Start(120, 2);
        Assert.That(clock.ElapsedSeconds, Is.EqualTo(-2));
        Assert.That(clock.BeatsToSeconds(4), Is.EqualTo(2));
        Assert.That(clock.SecondsToBeats(2), Is.EqualTo(4));
        now += 3; Assert.That(clock.ElapsedSeconds, Is.EqualTo(1)); Assert.That(clock.Beat, Is.EqualTo(2));
    }
    [Test]
    public void PauseResumeAndRestartIgnorePausedWallTime()
    {
        double now = 0; var clock = new MusicalClock(() => now); clock.Start(60, 2);
        now = 1; clock.Pause(); now = 100;
        Assert.That(clock.ElapsedSeconds, Is.EqualTo(-1)); clock.Pause(); clock.Resume(); clock.Resume();
        now = 101; Assert.That(clock.ElapsedSeconds, Is.EqualTo(0));
        clock.Restart(); Assert.That(clock.ElapsedSeconds, Is.EqualTo(-2));
    }
    [TestCase(-2, 240)]
    [TestCase(-1, 120)]
    [TestCase(0, 0)]
    [TestCase(0.1, -12)]
    public void FallingNotePositionUsesAbsoluteTime(double elapsed, double expected)
        => Assert.That(NoteTrajectory.Position(0, elapsed, 2, 240, 0), Is.EqualTo(expected).Within(0.00001));

    [TestCase(0, Judgement.Perfect)]
    [TestCase(-0.05, Judgement.Perfect)]
    [TestCase(0.05, Judgement.Perfect)]
    [TestCase(-0.12, Judgement.Good)]
    [TestCase(0.12, Judgement.Good)]
    [TestCase(-0.2, Judgement.Early)]
    [TestCase(0.2, Judgement.Late)]
    public void InclusiveScoringWindows(double inputTime, Judgement expected)
    {
        var e = Evaluator(MakeLesson(60)); e.Submit(new NoteEvent(60, true, 100, inputTime));
        Assert.That(e.Results[0].Judgement, Is.EqualTo(expected));
        Assert.That(e.Results[0].TimingErrorMs, Is.EqualTo(inputTime * 1000).Within(0.00001));
        Assert.That(e.Summary.Correct, Is.EqualTo(1));
    }
    [Test]
    public void WrongPitchDoesNotConsumeTarget()
    {
        var e = Evaluator(MakeLesson(60)); e.Submit(new NoteEvent(62, true, 100, 0));
        Assert.That(e.Results[0].Judgement, Is.EqualTo(Judgement.WrongPitch));
        Assert.That(e.IsResolved(0), Is.False);
        e.Submit(new NoteEvent(60, true, 100, 0.05));
        Assert.That(e.Summary.Correct, Is.EqualTo(1)); Assert.That(e.Summary.Extras, Is.EqualTo(1));
    }
    [TestCase(-0.201)]
    [TestCase(0.201)]
    public void OutsideWindowInputIsExtraAndTargetMisses(double time)
    {
        var e = Evaluator(MakeLesson(60)); e.Submit(new NoteEvent(60, true, 100, time)); e.Advance(1);
        Assert.That(e.Summary.Correct, Is.Zero); Assert.That(e.Summary.Extras, Is.EqualTo(1));
        Assert.That(e.Summary.Missed, Is.EqualTo(1));
    }
    [Test]
    public void MissingNotesExpireOnlyAfterInclusiveWindow()
    {
        var e = Evaluator(MakeLesson(60)); e.Advance(0.2); Assert.That(e.Results.Count, Is.Zero);
        e.Advance(0.201); e.Advance(10);
        Assert.That(e.Results.Count, Is.EqualTo(1)); Assert.That(e.Results[0].Judgement, Is.EqualTo(Judgement.Miss));
        Assert.That(e.Results[0].Input, Is.Null);
    }
    [Test]
    public void ChordRequiresOneInputPerTargetAndRepeatCannotRescore()
    {
        var lesson = MakeLesson(60, 64); lesson.notes[1].startBeat = 0;
        var e = Evaluator(lesson); e.Submit(new NoteEvent(60, true, 100, 0));
        Assert.That(e.Summary.Correct, Is.EqualTo(1)); Assert.That(e.IsResolved(1), Is.False);
        e.Submit(new NoteEvent(60, true, 100, 0.01)); Assert.That(e.Summary.Correct, Is.EqualTo(1));
        e.Submit(new NoteEvent(64, true, 100, 0.02)); Assert.That(e.Summary.Correct, Is.EqualTo(2));
    }
    [Test]
    public void SamePitchTargetsConsumeOnlyOnePerInput()
    {
        var lesson = MakeLesson(60, 60); lesson.notes[1].startBeat = 0;
        var e = Evaluator(lesson); e.Submit(new NoteEvent(60, true, 100, 0));
        Assert.That(e.Summary.Correct, Is.EqualTo(1));
    }
    [Test]
    public void NoteOffAndVelocityZeroAreRecordedWithoutScoring()
    {
        var e = Evaluator(MakeLesson(60)); e.Submit(new NoteEvent(60, true, 0, 0)); e.Submit(new NoteEvent(60, false, 70, 0));
        Assert.That(e.InputLog.Count, Is.EqualTo(2)); Assert.That(e.InputLog[0].IsNoteOn, Is.False);
        Assert.That(e.Results.Count, Is.Zero);
    }
    [TestCase(-1)]
    [TestCase(128)]
    public void InvalidMidiIsRejected(int pitch) => Assert.Throws<ArgumentOutOfRangeException>(() => new NoteEvent(pitch, true, 100, 0));
    [Test]
    public void EvaluatorSnapshotsLessonAndWindows()
    {
        var lesson = MakeLesson(60); var windows = new TimingWindows();
        var e = new OnsetEvaluator(windows); e.Reset(lesson);
        lesson.notes[0].pitch = 65; windows.acceptedMs = 0;
        e.Submit(new NoteEvent(60, true, 100, 0.15)); Assert.That(e.Summary.Correct, Is.EqualTo(1));
    }
    [Test]
    public void SessionCompletesOnceAndRestartResetsEverything()
    {
        double now = 0; var clock = new MusicalClock(() => now); var input = new InputSource();
        var e = new OnsetEvaluator(new TimingWindows()); var repo = new Repository { Lesson = MakeLesson(60, 62, 64, 65, 67) };
        using (var session = new LessonSession(repo, input, clock, e, 2))
        {
            int completions = 0, starts = 0; session.Completed += _ => completions++; session.Started += _ => starts++;
            input.Send(60, 0); Assert.That(e.InputLog.Count, Is.Zero);
            session.Start();
            for (int i = 0; i < 5; i++) { now = 2 + i; input.Send(repo.Lesson.notes[i].pitch, i); session.Tick(); }
            now = 7.01; session.Tick(); session.Tick(); input.Send(60, 6);
            Assert.That(session.State, Is.EqualTo(SessionState.Complete)); Assert.That(completions, Is.EqualTo(1));
            Assert.That(e.Summary.Correct, Is.EqualTo(5)); Assert.That(e.InputLog.Count, Is.EqualTo(5));
            session.Restart(); Assert.That(starts, Is.EqualTo(2)); Assert.That(clock.ElapsedSeconds, Is.EqualTo(-2));
            Assert.That(e.Results.Count, Is.Zero); Assert.That(e.InputLog.Count, Is.Zero); Assert.That(e.IsResolved(0), Is.False);
            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
        }
    }
    [Test]
    public void PauseIgnoresInputAndResumeKeepsTiming()
    {
        double now = 0; var clock = new MusicalClock(() => now); var input = new InputSource();
        var e = new OnsetEvaluator(new TimingWindows());
        using (var s = new LessonSession(new Repository { Lesson = MakeLesson(60) }, input, clock, e, 2))
        {
            s.Start(); now = 1; s.Pause(); now = 100; input.Send(60, -1); s.Tick();
            Assert.That(e.InputLog.Count, Is.Zero); s.Resume(); now = 101;
            input.Send(60, clock.ElapsedSeconds); Assert.That(e.Results[0].Judgement, Is.EqualTo(Judgement.Perfect));
            s.Restart(); Assert.That(e.Results.Count, Is.Zero);
        }
    }
    [Test]
    public void SilentLessonCompletesWithAllMisses()
    {
        double now = 0; var e = new OnsetEvaluator(new TimingWindows());
        using (var s = new LessonSession(new Repository { Lesson = MakeLesson(60, 62) }, new InputSource(), new MusicalClock(() => now), e, 2))
        {
            s.Start(); now = 5; s.Tick(); Assert.That(s.State, Is.EqualTo(SessionState.Complete));
            Assert.That(e.Summary.Missed, Is.EqualTo(2)); Assert.That(e.Summary.AverageAbsoluteErrorMs, Is.Null);
        }
    }
    [Test]
    public void DisposeUnsubscribesInput()
    {
        var e = new OnsetEvaluator(new TimingWindows()); var input = new InputSource();
        var s = new LessonSession(new Repository { Lesson = MakeLesson(60) }, input, new MusicalClock(() => 0), e, 2);
        s.Start(); s.Dispose(); input.Send(60, 0); Assert.That(e.InputLog.Count, Is.Zero);
    }
    [Test]
    public void GestureEdgesAreUniqueAndTrackingLossCancelsPinch()
    {
        var g = new SimulatedGestureProvider(); int starts = 0, ends = 0; bool cancelled = false;
        g.GestureStarted += _ => starts++; g.GestureEnded += e => { ends++; cancelled = e.Cancelled; };
        g.SetPinch(Hand.Right, true); g.SetPinch(Hand.Right, true); Assert.That(starts, Is.EqualTo(1));
        g.SetTracked(Hand.Right, false); g.SetPinch(Hand.Right, true);
        Assert.That(ends, Is.EqualTo(1)); Assert.That(cancelled, Is.True); Assert.That(starts, Is.EqualTo(1));
        g.SetTracked(Hand.Right, true); g.SetPinch(Hand.Right, true); g.SetPinch(Hand.Right, false); g.SetPinch(Hand.Right, false);
        Assert.That(starts, Is.EqualTo(2)); Assert.That(ends, Is.EqualTo(2)); Assert.That(cancelled, Is.False);
    }
    [Test]
    public void AverageTimingErrorUsesOnlyAcceptedOnsets()
    {
        var e = Evaluator(MakeLesson(60, 62));
        e.Submit(new NoteEvent(60, true, 100, -0.04)); e.Submit(new NoteEvent(62, true, 100, 1.1));
        e.Submit(new NoteEvent(70, true, 100, 1.15));
        Assert.That(e.Summary.AverageAbsoluteErrorMs, Is.EqualTo(70).Within(0.00001));
    }
}
