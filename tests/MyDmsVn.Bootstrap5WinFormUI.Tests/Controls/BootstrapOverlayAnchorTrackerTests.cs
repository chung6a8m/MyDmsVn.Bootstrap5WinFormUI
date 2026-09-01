using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapOverlayAnchorTrackerTests
{
    [Test]
    public void ReactsToTargetAncestorsScrollAndFormChanges()
    {
        using var form = new Form();
        using var scrollPanel = new TestScrollablePanel { AutoScroll = true };
        using var inner = new Panel();
        using var target = new Button();
        form.Controls.Add(scrollPanel);
        scrollPanel.Controls.Add(inner);
        inner.Controls.Add(target);
        var reposition = 0;
        var close = 0;
        using var tracker = new BootstrapOverlayAnchorTracker(target, () => reposition++, () => close++);

        target.Location = new Point(3, 4);
        target.Size = new Size(91, 31);
        inner.Location = new Point(5, 6);
        scrollPanel.RaiseScroll();
        form.Location = new Point(20, 30);
        form.Size = new Size(500, 400);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(reposition, Is.GreaterThanOrEqualTo(6));
            Assert.That(close, Is.Zero);
        }));
    }

    [Test]
    public void ParentChangeRebuildsChainAndInvisibleOrDisposedTargetRequestsClose()
    {
        using var firstParent = new Panel();
        using var secondParent = new Panel();
        var target = new Button();
        firstParent.Controls.Add(target);
        var reposition = 0;
        var close = 0;
        using var tracker = new BootstrapOverlayAnchorTracker(target, () => reposition++, () => close++);

        secondParent.Controls.Add(target);
        var afterReparent = reposition;
        firstParent.Location = new Point(7, 8);
        Assert.That(reposition, Is.EqualTo(afterReparent));
        secondParent.Location = new Point(9, 10);
        Assert.That(reposition, Is.GreaterThan(afterReparent));

        target.Visible = false;
        Assert.That(close, Is.EqualTo(1));
        target.Dispose();
        Assert.That(close, Is.EqualTo(2));
    }

    [Test]
    public void FormDeactivateRequestsClose()
    {
        using var form = new TestForm();
        using var target = new Button();
        form.Controls.Add(target);
        var reposition = 0;
        var close = 0;
        using var tracker = new BootstrapOverlayAnchorTracker(
            target,
            () => reposition++,
            () => close++);

        form.RaiseDeactivate();

        Assert.That(close, Is.EqualTo(1));
    }

    [Test]
    public void ReparentingToAnotherFormMovesDeactivateSubscription()
    {
        using var firstForm = new TestForm();
        using var secondForm = new TestForm();
        using var target = new Button();
        firstForm.Controls.Add(target);
        var close = 0;
        using var tracker = new BootstrapOverlayAnchorTracker(target, () => { }, () => close++);

        secondForm.Controls.Add(target);
        var afterReparent = close;
        firstForm.RaiseDeactivate();
        Assert.That(close, Is.EqualTo(afterReparent));

        secondForm.RaiseDeactivate();
        Assert.That(close, Is.EqualTo(afterReparent + 1));
    }

    [Test]
    public void DisposeRemovesAllReactions()
    {
        using var form = new TestForm();
        using var parent = new TestScrollablePanel();
        using var target = new Button();
        form.Controls.Add(parent);
        parent.Controls.Add(target);
        var reposition = 0;
        var close = 0;
        var tracker = new BootstrapOverlayAnchorTracker(target, () => reposition++, () => close++);
        tracker.Dispose();

        target.Location = new Point(1, 1);
        parent.Location = new Point(2, 2);
        parent.RaiseScroll();
        target.Visible = false;
        form.RaiseDeactivate();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(reposition, Is.Zero);
            Assert.That(close, Is.Zero);
        }));
    }

    private sealed class TestScrollablePanel : Panel
    {
        public void RaiseScroll()
        {
            OnScroll(new ScrollEventArgs(ScrollEventType.ThumbPosition, 1));
        }
    }

    private sealed class TestForm : Form
    {
        internal void RaiseDeactivate()
        {
            OnDeactivate(EventArgs.Empty);
        }
    }
}
