using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastContainerTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void DefaultsAndValidationMatchStage8Contract()
    {
        using var container = new BootstrapToastContainer();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(container.Placement, Is.EqualTo(BootstrapToastPlacement.TopRight));
            Assert.That(container.ToastSpacing, Is.EqualTo(8));
            Assert.That(container.MaximumVisibleToasts, Is.EqualTo(5));
            Assert.That(container.TabStop, Is.False);
            Assert.Throws<InvalidEnumArgumentException>((Action)(() => container.Placement = (BootstrapToastPlacement)999));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => container.ToastSpacing = -1));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => container.MaximumVisibleToasts = 0));
        }));
    }

    [Test]
    public void ShowToastTransfersOwnershipAndRejectsReuse()
    {
        using var firstContainer = new BootstrapToastContainer { Size = new Size(700, 500) };
        using var secondContainer = new BootstrapToastContainer { Size = new Size(700, 500) };
        var toast = new BootstrapToast { AutoHide = false };

        firstContainer.ShowToast(toast);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.Parent, Is.SameAs(firstContainer));
            Assert.That(toast.Visible, Is.True);
            Assert.That(toast.IsOwned, Is.True);
            Assert.Throws<InvalidOperationException>((Action)(() => firstContainer.ShowToast(toast)));
            Assert.Throws<InvalidOperationException>((Action)(() => secondContainer.ShowToast(toast)));
        }));
    }

    [Test]
    public void ShowToastRejectsNullDisposedAndAlreadyParentedToasts()
    {
        using var container = new BootstrapToastContainer();
        using var foreignParent = new Panel();
        var disposed = new BootstrapToast();
        disposed.Dispose();
        var parented = new BootstrapToast();
        foreignParent.Controls.Add(parented);

        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => container.ShowToast(null!)));
            Assert.Throws<ObjectDisposedException>((Action)(() => container.ShowToast(disposed)));
            Assert.Throws<InvalidOperationException>((Action)(() => container.ShowToast(parented)));
        }));
    }

    [Test]
    public void MaximumVisibleQueueIsFifoAndQueuedToastsStayHidden()
    {
        using var container = new BootstrapToastContainer
        {
            Size = new Size(700, 500),
            MaximumVisibleToasts = 2
        };
        var first = CreateManualToast("1");
        var second = CreateManualToast("2");
        var third = CreateManualToast("3");
        var fourth = CreateManualToast("4");

        container.ShowToast(first);
        container.ShowToast(second);
        container.ShowToast(third);
        container.ShowToast(fourth);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Visible, Is.True);
            Assert.That(second.Visible, Is.True);
            Assert.That(third.Visible, Is.False);
            Assert.That(fourth.Visible, Is.False);
        }));

        first.Dismiss();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(third.Visible, Is.True);
            Assert.That(fourth.Visible, Is.False);
        }));

        second.Dismiss();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(second.IsDisposed, Is.True);
            Assert.That(fourth.Visible, Is.True);
        }));
    }

    [TestCase(BootstrapToastPlacement.TopLeft)]
    [TestCase(BootstrapToastPlacement.TopRight)]
    [TestCase(BootstrapToastPlacement.BottomLeft)]
    [TestCase(BootstrapToastPlacement.BottomRight)]
    public void PlacementConsumesThePureStackLayout(BootstrapToastPlacement placement)
    {
        using var container = new BootstrapToastContainer
        {
            Size = new Size(700, 500),
            Placement = placement,
            ToastSpacing = 8
        };
        var first = CreateManualToast("first");
        var second = CreateManualToast("second");
        first.Width = 300;
        second.Width = 280;

        container.ShowToast(first);
        container.ShowToast(second);

        var expected = BootstrapToastLayoutLogic.CalculateStackBounds(
            container.ClientRectangle,
            new[] { first.Size, second.Size },
            placement,
            container.ToastSpacing,
            container.MaximumVisibleToasts,
            container.DeviceDpi > 0 ? container.DeviceDpi : 96);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Bounds, Is.EqualTo(expected[0]));
            Assert.That(second.Bounds, Is.EqualTo(expected[1]));
        }));
    }

    [Test]
    public void PlacementSpacingAndResizeReflowWithoutChangingOwnershipOrder()
    {
        using var container = new BootstrapToastContainer
        {
            Size = new Size(700, 500),
            Placement = BootstrapToastPlacement.TopLeft
        };
        var first = CreateManualToast("first");
        var second = CreateManualToast("second");
        container.ShowToast(first);
        container.ShowToast(second);

        container.Placement = BootstrapToastPlacement.BottomRight;
        container.ToastSpacing = 16;
        container.Size = new Size(800, 600);

        var expected = BootstrapToastLayoutLogic.CalculateStackBounds(
            container.ClientRectangle,
            new[] { first.Size, second.Size },
            container.Placement,
            container.ToastSpacing,
            container.MaximumVisibleToasts,
            container.DeviceDpi > 0 ? container.DeviceDpi : 96);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Bounds, Is.EqualTo(expected[0]));
            Assert.That(second.Bounds, Is.EqualTo(expected[1]));
            Assert.That(container.Controls.Cast<Control>().ToArray(), Is.EqualTo(new Control[] { first, second }));
        }));
    }

    [Test]
    public void QueuedDismissalRaisesOnceAndDisposesWithoutPromotionChurn()
    {
        using var container = new BootstrapToastContainer
        {
            Size = new Size(700, 500),
            MaximumVisibleToasts = 1
        };
        var visible = CreateManualToast("visible");
        var queued = CreateManualToast("queued");
        var dismissed = 0;
        queued.Dismissed += (_, _) => dismissed++;
        container.ShowToast(visible);
        container.ShowToast(queued);

        queued.Dismiss();
        queued.Dismiss();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dismissed, Is.EqualTo(1));
            Assert.That(queued.IsDisposed, Is.True);
            Assert.That(visible.Visible, Is.True);
            Assert.That(container.Controls.Count, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DismissAllDismissesSnapshotWithoutQueuedFlashes()
    {
        using var container = new BootstrapToastContainer
        {
            Size = new Size(700, 500),
            MaximumVisibleToasts = 1
        };
        var toasts = Enumerable.Range(1, 4).Select(index => CreateManualToast(index.ToString())).ToArray();
        var dismissed = new int[toasts.Length];
        for (var index = 0; index < toasts.Length; index++)
        {
            var captured = index;
            toasts[index].Dismissed += (_, _) => dismissed[captured]++;
            container.ShowToast(toasts[index]);
        }

        container.DismissAll();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(container.Controls.Count, Is.Zero);
            Assert.That(toasts.All(toast => toast.IsDisposed), Is.True);
            Assert.That(dismissed, Is.EqualTo(new[] { 1, 1, 1, 1 }));
        }));
    }

    [Test]
    public void ContainerDisposeDisposesOwnedToastsWithoutDismissedEvents()
    {
        var container = new BootstrapToastContainer
        {
            Size = new Size(700, 500),
            MaximumVisibleToasts = 1
        };
        var first = CreateManualToast("first");
        var second = CreateManualToast("second");
        var dismissed = 0;
        first.Dismissed += (_, _) => dismissed++;
        second.Dismissed += (_, _) => dismissed++;
        container.ShowToast(first);
        container.ShowToast(second);

        container.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(second.IsDisposed, Is.True);
            Assert.That(dismissed, Is.Zero);
        }));
    }

    [Test]
    public void ContainerPreservesCallerWidthAndRecomputesOwnedHeightForContent()
    {
        using var container = new BootstrapToastContainer { Size = new Size(700, 500) };
        var toast = CreateManualToast("short");
        toast.Width = 260;
        container.ShowToast(toast);
        var shortHeight = toast.Height;

        toast.Text = "A long body that wraps across several lines at this intentionally narrow width so the preferred height must grow.";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.Width, Is.EqualTo(260));
            Assert.That(toast.Height, Is.GreaterThan(shortHeight));
        }));
    }

    private static BootstrapToast CreateManualToast(string text)
    {
        return new BootstrapToast
        {
            Text = text,
            AutoHide = false,
            AnimationDuration = 200
        };
    }
}
