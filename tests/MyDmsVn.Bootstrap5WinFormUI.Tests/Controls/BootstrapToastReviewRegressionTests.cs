using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastReviewRegressionTests
{
    [Test]
    public void DismissedHandlerMayDisposeContainerWithoutCreatingAnOrphanedExitAnimation()
    {
        var harness = new BootstrapToastAnimationHarness();
        var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = new BootstrapToast
        {
            Width = 240,
            Text = "Reentrant dismissal",
            AutoHide = false,
            AnimationDuration = 200
        };

        container.ShowToast(toast);
        harness.Records[0].Advance(200);
        toast.Dismissed += (_, _) => container.Dispose();

        Assert.DoesNotThrow((Action)toast.Dismiss);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(container.IsDisposed, Is.True);
            Assert.That(toast.IsDisposed, Is.True);
            Assert.That(harness.Records.Count, Is.EqualTo(1), "no exit animation should be created after the callback disposed the owner");
        }));
    }

    [Test]
    public void ShowToastFailureRollsBackOwnershipParentAndVisibility()
    {
        using var failingContainer = new BootstrapToastContainer((_, _, _) =>
            throw new InvalidOperationException("animation factory failure"))
        {
            Size = new Size(400, 300)
        };
        var toast = new BootstrapToast
        {
            Width = 240,
            Text = "Transactional ownership",
            AutoHide = false,
            Visible = true
        };

        Assert.Throws<InvalidOperationException>((Action)(() => failingContainer.ShowToast(toast)));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.IsOwned, Is.False, "failed ShowToast must leave ownership with the caller");
            Assert.That(toast.Parent, Is.Null);
            Assert.That(toast.IsDisposed, Is.False);
            Assert.That(toast.Visible, Is.True, "failed transfer should restore caller-visible state");
            Assert.That(failingContainer.Controls.Count, Is.Zero);
        }));

        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        using var recoveryContainer = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        Assert.DoesNotThrow((Action)(() => recoveryContainer.ShowToast(toast)));
        Assert.That(toast.IsOwned, Is.True);
    }

    [Test]
    public void HidingAndShowingHostResumesRemainingAutoHideDelayInsteadOfResettingIt()
    {
        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        var timers = new List<ManualToastAutoHideTimer>();
        using var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = new BootstrapToast(() =>
        {
            var timer = new ManualToastAutoHideTimer();
            timers.Add(timer);
            return timer;
        })
        {
            Width = 240,
            Text = "Pause lifetime",
            AutoHide = true,
            AutoHideDelay = 5000,
            AnimationDuration = 200
        };

        container.ShowToast(toast);
        Assert.That(timers[0].Interval, Is.EqualTo(5000));

        var elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < 75)
        {
            Thread.SpinWait(256);
        }

        container.Visible = false;
        container.Visible = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(timers.Count, Is.EqualTo(2));
            Assert.That(timers[0].IsDisposed, Is.True);
            Assert.That(timers[1].Interval, Is.LessThan(5000), "resuming must preserve elapsed visible lifetime rather than restart the full delay");
            Assert.That(timers[1].Interval, Is.GreaterThan(4000), "only the short visible interval before hiding should be consumed");
        }));
    }
}
