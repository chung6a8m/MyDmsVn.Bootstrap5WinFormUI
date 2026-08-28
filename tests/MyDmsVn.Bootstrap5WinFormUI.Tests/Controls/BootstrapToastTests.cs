using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastTests
{
    [Test]
    public void PublicDefaultsAndMetadataMatchStage8Contract()
    {
        using var toast = new BootstrapToast();
        var defaultProperty = (DefaultPropertyAttribute?)Attribute.GetCustomAttribute(typeof(BootstrapToast), typeof(DefaultPropertyAttribute));
        var defaultEvent = (DefaultEventAttribute?)Attribute.GetCustomAttribute(typeof(BootstrapToast), typeof(DefaultEventAttribute));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(defaultProperty?.Name, Is.EqualTo(nameof(Control.Text)));
            Assert.That(defaultEvent?.Name, Is.EqualTo(nameof(BootstrapToast.Dismissed)));
            Assert.That(toast.Title, Is.EqualTo(string.Empty));
            Assert.That(toast.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(toast.Icon, Is.Null);
            Assert.That(toast.IconRenderer, Is.Not.Null);
            Assert.That(toast.Dismissible, Is.True);
            Assert.That(toast.AutoHide, Is.True);
            Assert.That(toast.AutoHideDelay, Is.EqualTo(5000));
            Assert.That(toast.AnimationDuration, Is.EqualTo(200));
            Assert.That(toast.TabStop, Is.False);
            Assert.That(toast.AccessibleRole, Is.EqualTo(AccessibleRole.Alert));
            Assert.That(toast.AccessibleDescription, Is.EqualTo("Transient notification."));
            Assert.That(toast.Size, Is.EqualTo(new Size(320, 96)));
        }));
    }

    [Test]
    public void PublicPropertiesNormalizeAndRejectInvalidValues()
    {
        using var toast = new BootstrapToast();

        toast.Title = null!;
        Assert.That(toast.Title, Is.EqualTo(string.Empty));

        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => toast.IconRenderer = null!));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => toast.Variant = (BootstrapVariant)999));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => toast.AutoHideDelay = 0));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => toast.AutoHideDelay = -1));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => toast.AnimationDuration = 0));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => toast.AnimationDuration = -1));
        }));
    }

    [Test]
    public void DismissDetachedVisibleToastHidesAndRaisesOncePerVisibleCycle()
    {
        using var toast = new BootstrapToast();
        var count = 0;
        toast.Dismissed += (_, _) => count++;

        toast.Visible = true;
        toast.Dismiss();
        toast.Dismiss();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.Visible, Is.False);
            Assert.That(count, Is.EqualTo(1));
        }));

        toast.Visible = true;
        toast.Dismiss();
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void DirectVisibilityChangeDoesNotRaiseDismissed()
    {
        using var toast = new BootstrapToast();
        var count = 0;
        toast.Dismissed += (_, _) => count++;

        toast.Visible = true;
        toast.Visible = false;

        Assert.That(count, Is.Zero);
    }

    [Test]
    public void CloseButtonIsAccessibleAndRoutesThroughDismiss()
    {
        using var toast = new BootstrapToast();
        var closeButton = toast.Controls.OfType<Button>().Single();
        var count = 0;
        toast.Dismissed += (_, _) => count++;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(closeButton.Visible, Is.True);
            Assert.That(closeButton.TabStop, Is.True);
            Assert.That(closeButton.AccessibleName, Is.EqualTo("Dismiss notification"));
            Assert.That(closeButton.AccessibleDescription, Is.EqualTo("Dismisses this notification."));
        }));

        toast.Visible = true;
        closeButton.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toast.Visible, Is.False);
            Assert.That(count, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DismissibleAndEnabledStateGovernCloseVisibilityAndTabStop()
    {
        using var toast = new BootstrapToast();
        var closeButton = toast.Controls.OfType<Button>().Single();

        toast.Dismissible = false;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(closeButton.Visible, Is.False);
            Assert.That(closeButton.TabStop, Is.False);
        }));

        toast.Dismissible = true;
        toast.Enabled = false;
        Assert.That(closeButton.TabStop, Is.False);
    }

    [Test]
    public void ConstructionAndDetachedVisibilityDoNotCreateAutoHideTimer()
    {
        var created = 0;
        using var toast = new BootstrapToast(() =>
        {
            created++;
            return new TestToastAutoHideTimer();
        });

        toast.Visible = true;
        Application.DoEvents();

        Assert.That(created, Is.Zero);
    }

    [Test]
    public void CallerOwnedFontIsNotDisposedWithToast()
    {
        using var callerFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f, FontStyle.Italic);
        var toast = new BootstrapToast
        {
            Font = callerFont,
            Title = "Saved",
            Text = "Body"
        };

        toast.Dispose();

        Assert.DoesNotThrow((Action)(() =>
        {
            var size = callerFont.GetHeight();
            Assert.That(size, Is.GreaterThan(0f));
        }));
    }

    private sealed class TestToastAutoHideTimer : IBootstrapToastAutoHideTimer
    {
        public int Interval { get; set; }
        public bool Enabled { get; private set; }
        public event EventHandler? Tick;
        public void Start() => Enabled = true;
        public void Stop() => Enabled = false;
        public void Dispose() => Enabled = false;
    }
}
