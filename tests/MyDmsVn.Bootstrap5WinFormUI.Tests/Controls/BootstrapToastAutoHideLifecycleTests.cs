using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastAutoHideLifecycleTests
{
    [Test]
    public void TimerStartsOnlyAfterEnterCompletesAndNeverWhileQueued()
    {
        var harness = new BootstrapToastAnimationHarness();
        var timers = new List<ManualToastAutoHideTimer>();
        using var container = new BootstrapToastContainer(harness.Create)
        {
            Size = new Size(400, 300),
            MaximumVisibleToasts = 1
        };
        var first = CreateAutoHideToast(timers);
        var second = CreateAutoHideToast(timers);

        container.ShowToast(first);
        Assert.That(timers, Is.Empty, "entering toast must not start auto-hide");
        harness.Records[0].Advance(200);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(timers.Count, Is.EqualTo(1));
            Assert.That(timers[0].Enabled, Is.True);
            Assert.That(timers[0].Interval, Is.EqualTo(5000));
        }));

        container.ShowToast(second);
        Assert.That(timers.Count, Is.EqualTo(1), "queued toast must not age its auto-hide delay");

        first.Dismiss();
        harness.Records[1].Advance(200);
        Assert.That(timers.Count, Is.EqualTo(1), "promoted toast must still wait for enter completion");
        harness.Records[2].Advance(200);
        Assert.That(timers.Count, Is.EqualTo(2));
    }

    [Test]
    public void ManualDismissCleansTimerBeforeDismissedObserversRun()
    {
        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        var timers = new List<ManualToastAutoHideTimer>();
        using var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = CreateAutoHideToast(timers);
        bool? disposedSeen = null;
        bool? enabledSeen = null;
        int? subscribersSeen = null;
        toast.Dismissed += (_, _) =>
        {
            disposedSeen = timers[0].IsDisposed;
            enabledSeen = timers[0].Enabled;
            subscribersSeen = timers[0].SubscriberCount;
        };

        container.ShowToast(toast);
        toast.Dismiss();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(disposedSeen, Is.True);
            Assert.That(enabledSeen, Is.False);
            Assert.That(subscribersSeen, Is.Zero);
        }));
    }

    [Test]
    public void PropertyChangesRestartOnlySemanticAutoHideDelay()
    {
        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        var timers = new List<ManualToastAutoHideTimer>();
        using var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = CreateAutoHideToast(timers);

        container.ShowToast(toast);
        var first = timers[0];

        toast.AutoHide = false;
        Assert.That(first.IsDisposed, Is.True);

        toast.AutoHide = true;
        var second = timers[1];
        Assert.That(second.Interval, Is.EqualTo(5000));

        toast.AutoHideDelay = 1500;
        var third = timers[2];
        Assert.Multiple((Action)(() =>
        {
            Assert.That(second.IsDisposed, Is.True);
            Assert.That(third.Interval, Is.EqualTo(1500));
            Assert.That(third.Enabled, Is.True);
        }));

        toast.AnimationDuration = 725;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(timers.Count, Is.EqualTo(3));
            Assert.That(third.Interval, Is.EqualTo(1500));
            Assert.That(third.IsDisposed, Is.False);
        }));
    }

    [Test]
    public void StaleTimerTickIsIgnoredButCurrentTimerDismissesOnce()
    {
        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        var timers = new List<ManualToastAutoHideTimer>();
        using var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = CreateAutoHideToast(timers);
        var dismissed = 0;
        toast.Dismissed += (_, _) => dismissed++;

        container.ShowToast(toast);
        var stale = timers[0];
        toast.AutoHideDelay = 1200;
        var current = timers[1];

        stale.Fire();
        Assert.That(dismissed, Is.Zero);

        current.Fire();
        Assert.That(dismissed, Is.EqualTo(1));
    }

    [Test]
    public void ReducedMotionStillWaitsForSemanticTimerTick()
    {
        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        var timers = new List<ManualToastAutoHideTimer>();
        using var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = CreateAutoHideToast(timers);
        var dismissed = 0;
        toast.Dismissed += (_, _) => dismissed++;

        container.ShowToast(toast);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.IsFullyVisible, Is.True);
            Assert.That(timers.Count, Is.EqualTo(1));
            Assert.That(dismissed, Is.Zero);
        }));

        timers[0].Fire();
        Assert.That(dismissed, Is.EqualTo(1));
    }

    [Test]
    public void HidingHostAndDisposalStopAndDisposeActiveTimers()
    {
        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        var timers = new List<ManualToastAutoHideTimer>();
        var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = CreateAutoHideToast(timers);

        container.ShowToast(toast);
        var first = timers[0];
        container.Visible = false;
        Assert.That(first.IsDisposed, Is.True);

        container.Visible = true;
        Assert.That(timers.Count, Is.EqualTo(2));
        var second = timers[1];

        container.Dispose();
        Assert.That(second.IsDisposed, Is.True);
        Assert.DoesNotThrow((Action)(() => second.Fire()));
    }

    private static BootstrapToast CreateAutoHideToast(List<ManualToastAutoHideTimer> timers)
    {
        return new BootstrapToast(() =>
        {
            var timer = new ManualToastAutoHideTimer();
            timers.Add(timer);
            return timer;
        })
        {
            Width = 240,
            Text = "Auto-hide toast",
            AutoHide = true,
            AutoHideDelay = 5000,
            AnimationDuration = 200
        };
    }
}
