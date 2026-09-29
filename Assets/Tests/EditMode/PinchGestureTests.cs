using NUnit.Framework;
using XRMidi;

public sealed class PinchGestureTests
{
    [Test]
    public void HeldPinchEmitsOneStartAndOneEnd()
    {
        var state = new PinchGestureState(Hand.Left, 0.025, 0.04);
        int starts = 0, ends = 0;
        state.Started += e => { Assert.That(e.Hand, Is.EqualTo(Hand.Left)); starts++; };
        state.Ended += e => { Assert.That(e.Cancelled, Is.False); ends++; };
        state.Update(true, 0.05);
        for (int i = 0; i < 100; i++) state.Update(true, 0.02);
        Assert.That(starts, Is.EqualTo(1)); Assert.That(state.IsPinching, Is.True);
        state.Update(true, 0.04); state.Update(true, 0.05);
        Assert.That(ends, Is.EqualTo(1)); Assert.That(state.IsPinching, Is.False);
    }
    [Test]
    public void HysteresisDoesNotChatterAtPressThreshold()
    {
        var s = new PinchGestureState(Hand.Right, 0.025, 0.04);
        int starts = 0, ends = 0; s.Started += _ => starts++; s.Ended += _ => ends++;
        s.Update(true, 0.05); s.Update(true, 0.025);
        for (int i = 0; i < 20; i++) { s.Update(true, 0.026); s.Update(true, 0.024); }
        Assert.That(starts, Is.EqualTo(1)); Assert.That(ends, Is.Zero);
    }
    [Test]
    public void TrackingLossCancelsAndReacquisitionMustSeeOpenHand()
    {
        var s = new PinchGestureState(Hand.Right, 0.025, 0.04);
        int starts = 0, cancelled = 0; s.Started += _ => starts++; s.Ended += e => { if (e.Cancelled) cancelled++; };
        s.Update(true, 0.02); Assert.That(starts, Is.Zero);
        s.Update(true, 0.05); s.Update(true, 0.02); s.Update(false, 0); s.Cancel();
        Assert.That(cancelled, Is.EqualTo(1)); Assert.That(s.IsTracked, Is.False);
        s.Update(true, 0.02); Assert.That(starts, Is.EqualTo(1));
        s.Update(true, 0.05); s.Update(true, 0.02); Assert.That(starts, Is.EqualTo(2));
    }
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(-0.1)]
    public void InvalidDistanceCancels(double distance)
    {
        var s = new PinchGestureState(Hand.Left, 0.025, 0.04);
        s.Update(true, 0.05); s.Update(true, 0.02); s.Update(true, distance);
        Assert.That(s.IsPinching, Is.False); Assert.That(s.IsTracked, Is.False);
    }
    [Test]
    public void HandsAreIndependent()
    {
        var left = new PinchGestureState(Hand.Left, 0.025, 0.04); var right = new PinchGestureState(Hand.Right, 0.025, 0.04);
        left.Update(true, 0.05); right.Update(true, 0.05); left.Update(true, 0.02);
        Assert.That(right.IsPinching, Is.False); right.Update(true, 0.02); left.Cancel();
        Assert.That(right.IsPinching, Is.True);
    }
    [TestCase(0, 0.04)]
    [TestCase(0.04, 0.025)]
    [TestCase(0.04, 0.04)]
    public void InvalidThresholdsAreRejected(double press, double release)
        => Assert.Throws<System.ArgumentException>(() => new PinchGestureState(Hand.Left, press, release));
}
