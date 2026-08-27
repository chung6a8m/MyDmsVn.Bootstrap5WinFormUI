using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Animation;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Animation;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapLoopAnimationTests
{
    [Test]
    public void ConstructorRejectsNonPositiveDuration()
    {
        Action zeroDuration = () => { using var animation = new BootstrapLoopAnimation(TimeSpan.Zero); };
        Action negativeDuration = () => { using var animation = new BootstrapLoopAnimation(TimeSpan.FromMilliseconds(-1)); };

        Assert.That(zeroDuration, Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(negativeDuration, Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void InitialStateIsStoppedAtZero()
    {
        using var animation = CreateAnimation(out _, out _);

        Assert.That(animation.Progress, Is.EqualTo(0.0));
        Assert.That(animation.IsRunning, Is.False);
    }

    [Test]
    public void StartPublishesCycleProgress()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(250));
        scheduler.FireFrame();

        Assert.That(animation.IsRunning, Is.True);
        Assert.That(animation.Progress, Is.EqualTo(0.25).Within(0.000001));
    }

    [Test]
    public void ProgressWrapsAtCycleBoundaryAndAcrossMultipleCycles()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);

        animation.Start();
        clock.Advance(TimeSpan.FromSeconds(1));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.0).Within(0.000001));

        clock.Advance(TimeSpan.FromMilliseconds(2250));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.25).Within(0.000001));
    }

    [Test]
    public void CustomEasingIsAppliedAndClamped()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler, progress => progress * progress);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(500));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.25).Within(0.000001));

        using var clamped = CreateAnimation(out var clampedClock, out var clampedScheduler, _ => -1.0);
        clamped.Start();
        clampedClock.Advance(TimeSpan.FromMilliseconds(250));
        clampedScheduler.FireFrame();
        Assert.That(clamped.Progress, Is.EqualTo(0.0));
    }

    [Test]
    public void StopFreezesAndStartResumesCyclePosition()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(300));
        scheduler.FireFrame();
        animation.Stop();

        clock.Advance(TimeSpan.FromMilliseconds(500));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.3).Within(0.000001));

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(200));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    public void RestartResetsCycleToZero()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(600));
        scheduler.FireFrame();
        animation.Restart();

        Assert.That(animation.Progress, Is.EqualTo(0.0));
        Assert.That(animation.IsRunning, Is.True);
        Assert.That(clock.Elapsed, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void RepeatedStartAndStopAreIdempotent()
    {
        using var animation = CreateAnimation(out _, out var scheduler);

        animation.Start();
        animation.Start();
        Assert.That(scheduler.StartCount, Is.EqualTo(1));

        animation.Stop();
        animation.Stop();
        Assert.That(scheduler.StopCount, Is.EqualTo(1));
    }

    [Test]
    public void ReducedMotionStaysAtZeroWithoutScheduling()
    {
        using var animation = CreateAnimation(out _, out var scheduler, reducedMotion: true);

        animation.Start();

        Assert.That(animation.Progress, Is.EqualTo(0.0));
        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.StartCount, Is.Zero);
    }

    [Test]
    public void HiddenOwnerPausesAndShowResumesWithoutCountingHiddenTime()
    {
        using var owner = new Control();
        using var animation = CreateAnimation(out var clock, out var scheduler, owner: owner);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(250));
        scheduler.FireFrame();
        owner.Visible = false;

        Assert.That(animation.IsRunning, Is.False);
        clock.Advance(TimeSpan.FromMilliseconds(500));
        owner.Visible = true;

        Assert.That(animation.IsRunning, Is.True);
        Assert.That(clock.Elapsed, Is.EqualTo(TimeSpan.Zero));

        clock.Advance(TimeSpan.FromMilliseconds(250));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    public void DisposedOwnerStopsAndPreventsRestart()
    {
        var owner = new Control();
        using var animation = CreateAnimation(out _, out var scheduler, owner: owner);

        animation.Start();
        owner.Dispose();

        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.IsRunning, Is.False);

        animation.Restart();
        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.IsRunning, Is.False);
    }

    [Test]
    public void ProgressHandlerMayDisposeReentrantly()
    {
        var animation = CreateAnimation(out var clock, out var scheduler);
        animation.ProgressChanged += (_, _) => animation.Dispose();

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        Action fireFrame = scheduler.FireFrame;

        Assert.DoesNotThrow(fireFrame);
        Assert.That(animation.IsRunning, Is.False);
        animation.Dispose();
    }

    [Test]
    public void DisposeIsIdempotentAndOperationsAfterDisposeThrow()
    {
        var animation = CreateAnimation(out _, out _);
        animation.Dispose();
        Action dispose = animation.Dispose;
        Action start = animation.Start;
        Action stop = animation.Stop;
        Action restart = animation.Restart;

        Assert.DoesNotThrow(dispose);
        Assert.That(start, Throws.TypeOf<ObjectDisposedException>());
        Assert.That(stop, Throws.TypeOf<ObjectDisposedException>());
        Assert.That(restart, Throws.TypeOf<ObjectDisposedException>());
    }

    private static BootstrapLoopAnimation CreateAnimation(
        out ManualAnimationClock clock,
        out ManualAnimationFrameScheduler scheduler,
        Func<double, double>? easing = null,
        Control? owner = null,
        bool reducedMotion = false)
    {
        clock = new ManualAnimationClock();
        scheduler = new ManualAnimationFrameScheduler();
        return new BootstrapLoopAnimation(
            TimeSpan.FromSeconds(1),
            easing ?? BootstrapEasing.Linear,
            owner,
            clock,
            scheduler,
            () => reducedMotion);
    }
}
