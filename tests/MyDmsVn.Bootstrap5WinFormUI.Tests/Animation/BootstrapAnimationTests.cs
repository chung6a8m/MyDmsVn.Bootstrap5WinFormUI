using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Animation;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Animation;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapAnimationTests
{
    [Test]
    public void ConstructorRejectsNonPositiveDuration()
    {
        Action zeroDuration = () => { using var animation = new BootstrapAnimation(TimeSpan.Zero); };
        Action negativeDuration = () => { using var animation = new BootstrapAnimation(TimeSpan.FromMilliseconds(-1)); };

        Assert.That(zeroDuration, Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(negativeDuration, Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void InitialStateIsStoppedAtZero()
    {
        using var animation = CreateAnimation(out _, out _);

        Assert.That(animation.Progress, Is.EqualTo(0.0));
        Assert.That(animation.IsRunning, Is.False);
        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void StartPublishesElapsedTimeProgress()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);
        var changes = 0;
        animation.ProgressChanged += (_, _) => changes++;

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(250));
        scheduler.FireFrame();

        Assert.That(animation.IsRunning, Is.True);
        Assert.That(animation.Progress, Is.EqualTo(0.25).Within(0.000001));
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void CustomEasingIsAppliedAndClamped()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler, progress => progress * progress);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(500));
        scheduler.FireFrame();

        Assert.That(animation.Progress, Is.EqualTo(0.25).Within(0.000001));

        using var clamped = CreateAnimation(out var clampedClock, out var clampedScheduler, _ => 2.0);
        clamped.Start();
        clampedClock.Advance(TimeSpan.FromMilliseconds(100));
        clampedScheduler.FireFrame();
        Assert.That(clamped.Progress, Is.EqualTo(1.0));
    }

    [Test]
    public void NaturalCompletionStopsAndRaisesCompletedExactlyOnce()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);
        var completed = 0;
        animation.Completed += (_, _) => completed++;

        animation.Start();
        clock.Advance(TimeSpan.FromSeconds(1));
        scheduler.FireFrame();
        scheduler.FireFrame();

        Assert.That(animation.Progress, Is.EqualTo(1.0));
        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.IsRunning, Is.False);
        Assert.That(completed, Is.EqualTo(1));
    }

    [Test]
    public void StopFreezesAndStartResumesFromFrozenProgress()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(300));
        scheduler.FireFrame();
        animation.Stop();

        clock.Advance(TimeSpan.FromMilliseconds(500));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.3).Within(0.000001));
        Assert.That(animation.IsRunning, Is.False);

        animation.Start();
        clock.Advance(TimeSpan.FromMilliseconds(200));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.5).Within(0.000001));
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
    public void RestartAlwaysBeginsNewRunAtZero()
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
    public void StartingCompletedAnimationBeginsNewRunAtZero()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);

        animation.Start();
        clock.Advance(TimeSpan.FromSeconds(1));
        scheduler.FireFrame();
        animation.Start();

        Assert.That(animation.Progress, Is.EqualTo(0.0));
        Assert.That(animation.IsRunning, Is.True);
    }

    [Test]
    public void ReducedMotionCompletesImmediatelyWithoutScheduling()
    {
        using var animation = CreateAnimation(out _, out var scheduler, BootstrapEasing.Linear, reducedMotion: true);
        var completed = 0;
        animation.Completed += (_, _) => completed++;

        animation.Start();

        Assert.That(animation.Progress, Is.EqualTo(1.0));
        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.StartCount, Is.Zero);
        Assert.That(completed, Is.EqualTo(1));
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
        Assert.That(animation.Progress, Is.EqualTo(0.25).Within(0.000001));

        clock.Advance(TimeSpan.FromMilliseconds(500));
        owner.Visible = true;
        Assert.That(animation.IsRunning, Is.True);
        Assert.That(clock.Elapsed, Is.EqualTo(TimeSpan.Zero));

        clock.Advance(TimeSpan.FromMilliseconds(250));
        scheduler.FireFrame();
        Assert.That(animation.Progress, Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    public void DisposedOwnerStopsAnimationAndPreventsRestart()
    {
        var owner = new Control();
        using var animation = CreateAnimation(out _, out var scheduler, owner: owner);

        animation.Start();
        owner.Dispose();

        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.IsRunning, Is.False);

        animation.Start();
        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.IsRunning, Is.False);
    }

    [Test]
    public void AlreadyDisposedOwnerDoesNotStart()
    {
        var owner = new Control();
        owner.Dispose();
        using var animation = CreateAnimation(out _, out var scheduler, owner: owner);

        animation.Start();

        Assert.That(animation.IsRunning, Is.False);
        Assert.That(scheduler.StartCount, Is.Zero);
    }

    [Test]
    public void ProgressHandlerMayDisposeAnimationReentrantly()
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
    public void CompletionHandlerMayRestartAnimationReentrantly()
    {
        using var animation = CreateAnimation(out var clock, out var scheduler);
        var completed = 0;
        animation.Completed += (_, _) =>
        {
            completed++;
            if (completed == 1)
            {
                animation.Restart();
            }
        };

        animation.Start();
        clock.Advance(TimeSpan.FromSeconds(1));
        scheduler.FireFrame();

        Assert.That(completed, Is.EqualTo(1));
        Assert.That(animation.IsRunning, Is.True);
        Assert.That(animation.Progress, Is.EqualTo(0.0));
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

    private static BootstrapAnimation CreateAnimation(
        out ManualAnimationClock clock,
        out ManualAnimationFrameScheduler scheduler,
        Func<double, double>? easing = null,
        Control? owner = null,
        bool reducedMotion = false)
    {
        clock = new ManualAnimationClock();
        scheduler = new ManualAnimationFrameScheduler();
        return new BootstrapAnimation(
            TimeSpan.FromSeconds(1),
            easing ?? BootstrapEasing.Linear,
            owner,
            clock,
            scheduler,
            () => reducedMotion);
    }
}
