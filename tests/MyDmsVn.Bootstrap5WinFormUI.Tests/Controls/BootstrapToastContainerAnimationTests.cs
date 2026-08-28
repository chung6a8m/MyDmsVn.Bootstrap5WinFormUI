using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastContainerAnimationTests
{
    [Test]
    public void EnterStartsOutsideAnchorAndUsesEaseOutUntilTarget()
    {
        var harness = new BootstrapToastAnimationHarness();
        using var container = new BootstrapToastContainer(harness.Create)
        {
            Size = new Size(400, 300),
            Placement = BootstrapToastPlacement.TopRight
        };
        var toast = CreateToast(200);

        container.ShowToast(toast);

        Assert.That(harness.Records.Count, Is.EqualTo(1));
        var enter = harness.Records[0];
        var targetX = container.ClientRectangle.Right - toast.Width;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(enter.Animation.Easing(0.5), Is.EqualTo(BootstrapEasing.EaseOut(0.5)).Within(0.000001));
            Assert.That(toast.Left, Is.EqualTo(targetX + 16));
            Assert.That(toast.Top, Is.EqualTo(0));
        }));

        enter.Advance(100);
        Assert.That(toast.Left, Is.EqualTo(targetX + 4));

        enter.Advance(100);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.Left, Is.EqualTo(targetX));
            Assert.That(enter.Scheduler.IsRunning, Is.False);
            Assert.That(toast.IsFullyVisible, Is.True);
        }));
    }

    [Test]
    public void DismissWhileEnteringBeginsExitFromCurrentVisualBounds()
    {
        var harness = new BootstrapToastAnimationHarness();
        using var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = CreateToast(200);
        var dismissed = 0;
        toast.Dismissed += (_, _) => dismissed++;

        container.ShowToast(toast);
        var enter = harness.Records[0];
        enter.Advance(50);
        var current = toast.Bounds;

        toast.Dismiss();

        Assert.That(harness.Records.Count, Is.EqualTo(2));
        var exit = harness.Records[1];
        Assert.Multiple((Action)(() =>
        {
            Assert.That(enter.Scheduler.IsRunning, Is.False);
            Assert.That(exit.Animation.Easing(0.5), Is.EqualTo(BootstrapEasing.EaseIn(0.5)).Within(0.000001));
            Assert.That(toast.Bounds, Is.EqualTo(current));
            Assert.That(toast.IsDisposed, Is.False);
            Assert.That(dismissed, Is.EqualTo(1));
        }));

        exit.Advance(200);
        Assert.That(toast.IsDisposed, Is.True);
    }

    [Test]
    public void ExitCompletionDisposesBeforePromotingOldestQueuedToast()
    {
        var harness = new BootstrapToastAnimationHarness();
        using var container = new BootstrapToastContainer(harness.Create)
        {
            Size = new Size(400, 300),
            MaximumVisibleToasts = 1
        };
        var first = CreateToast(200);
        var second = CreateToast(200);

        container.ShowToast(first);
        harness.Records[0].Advance(200);
        container.ShowToast(second);
        Assert.That(second.Visible, Is.False);

        first.Dismiss();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.IsDisposed, Is.False);
            Assert.That(second.Visible, Is.False);
        }));

        harness.Records[1].Advance(200);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(second.Visible, Is.True);
            Assert.That(harness.Records.Count, Is.EqualTo(3));
        }));
    }

    [Test]
    public void ReducedMotionCompletesEnterAndExitSynchronouslyWithoutFrames()
    {
        var harness = new BootstrapToastAnimationHarness { ReducedMotion = true };
        using var container = new BootstrapToastContainer(harness.Create) { Size = new Size(400, 300) };
        var toast = CreateToast(200);
        var dismissed = 0;
        toast.Dismissed += (_, _) => dismissed++;

        container.ShowToast(toast);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.IsFullyVisible, Is.True);
            Assert.That(harness.Records.Count, Is.EqualTo(1));
            Assert.That(harness.Records[0].Scheduler.StartCount, Is.Zero);
        }));

        toast.Dismiss();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.IsDisposed, Is.True);
            Assert.That(dismissed, Is.EqualTo(1));
            Assert.That(harness.Records.Count, Is.EqualTo(2));
            Assert.That(harness.Records[1].Scheduler.StartCount, Is.Zero);
        }));
    }

    [Test]
    public void HiddenContainerDefersFramesAndDisposeStopsActiveTransition()
    {
        var harness = new BootstrapToastAnimationHarness();
        var container = new BootstrapToastContainer(harness.Create)
        {
            Size = new Size(400, 300),
            Visible = false
        };
        var toast = CreateToast(200);

        container.ShowToast(toast);
        var enter = harness.Records[0];
        Assert.That(enter.Scheduler.StartCount, Is.Zero);

        container.Visible = true;
        Assert.That(enter.Scheduler.StartCount, Is.EqualTo(1));

        container.Dispose();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(enter.Scheduler.IsRunning, Is.False);
            Assert.That(toast.IsDisposed, Is.True);
        }));
    }

    private static BootstrapToast CreateToast(int width)
    {
        return new BootstrapToast
        {
            Width = width,
            Text = "Toast body",
            AutoHide = false,
            AnimationDuration = 200
        };
    }
}
