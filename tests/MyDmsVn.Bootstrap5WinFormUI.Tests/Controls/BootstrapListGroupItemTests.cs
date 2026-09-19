using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListGroupItemTests
{
    [Test]
    public void DefaultsArePresentationalAndNeutral()
    {
        using var item = new SelectabilityProbeItem();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item, Is.InstanceOf<Panel>());
            Assert.That(item.Active, Is.False);
            Assert.That(item.Actionable, Is.False);
            Assert.That(item.Variant, Is.Null);
            Assert.That(item.UseThemeFont, Is.True);
            Assert.That(item.TabStop, Is.False);
            Assert.That(item.HasSelectableStyle, Is.False);
            Assert.That(item.CanSelect, Is.False);
            Assert.That(item.AccessibleRole, Is.EqualTo(AccessibleRole.ListItem));
        }));
    }

    [Test]
    public void ActionableSynchronizesTabStopAndRealSelectability()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var item = new SelectabilityProbeItem { Size = new System.Drawing.Size(160, 40) };
        form.Controls.Add(item);
        form.Show();

        item.Actionable = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.TabStop, Is.True);
            Assert.That(item.HasSelectableStyle, Is.True);
            Assert.That(item.CanSelect, Is.True);
            Assert.That(item.Focus(), Is.True);
        }));

        item.Actionable = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.TabStop, Is.False);
            Assert.That(item.HasSelectableStyle, Is.False);
            Assert.That(item.CanSelect, Is.False);
        }));
    }

    [Test]
    public void InvalidContextualVariantIsRejected()
    {
        using var item = new BootstrapListGroupItem();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => item.Variant = (BootstrapVariant)99));
        Assert.DoesNotThrow((Action)(() => item.Variant = null));
    }

    [Test]
    public void EventArgsRejectNullAndPreserveItemIdentity()
    {
        using var item = new BootstrapListGroupItem();

        Assert.Throws<ArgumentNullException>((Action)(() => _ = new BootstrapListGroupItemEventArgs(null!)));
        Assert.That(new BootstrapListGroupItemEventArgs(item).Item, Is.SameAs(item));
    }

    [Test]
    public void UsesOwnerPaintingWithoutBreakingRuntimeSelectability()
    {
        using var item = new SelectabilityProbeItem { Actionable = true };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.HasStyle(ControlStyles.UserPaint), Is.True);
            Assert.That(item.HasStyle(ControlStyles.AllPaintingInWmPaint), Is.True);
            Assert.That(item.HasStyle(ControlStyles.OptimizedDoubleBuffer), Is.True);
            Assert.That(item.HasStyle(ControlStyles.ResizeRedraw), Is.True);
            Assert.That(item.HasStyle(ControlStyles.Selectable), Is.True);
        }));
    }

    [Test]
    public void PreferredSizeTracksTextAndHostedChildBounds()
    {
        using var item = new BootstrapListGroupItem { Text = "Short" };
        var shortSize = item.GetPreferredSize(Size.Empty);
        item.Text = "A substantially longer list group item";
        var longSize = item.GetPreferredSize(Size.Empty);
        using var child = new Label { Bounds = new Rectangle(12, 8, 180, 36) };
        item.Controls.Add(child);
        var richSize = item.GetPreferredSize(Size.Empty);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(longSize.Width, Is.GreaterThan(shortSize.Width));
            Assert.That(richSize.Width, Is.GreaterThanOrEqualTo(child.Right));
            Assert.That(richSize.Height, Is.GreaterThanOrEqualTo(child.Bottom));
            Assert.That(richSize.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(richSize.Height, Is.GreaterThanOrEqualTo(0));
        }));
    }

    [Test]
    public void ThemeFontCanBeDisabledAndThemeSubscriptionIsReleased()
    {
        var baseline = GetThemeSubscriptionCount();
        using var callerFont = new Font("Segoe UI", 10f, FontStyle.Italic);
        var item = new BootstrapListGroupItem
        {
            UseThemeFont = false,
            Font = callerFont
        };
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline + 1));

        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Assert.That(item.Font, Is.SameAs(callerFont));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }

        item.Dispose();
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline));
    }

    [Test]
    public void TinyItemPaintsWithoutInvalidGeometryAndExposesNoRadiusProperty()
    {
        using var item = new BootstrapListGroupItem { Size = new Size(1, 1) };
        using var bitmap = new Bitmap(1, 1);

        Assert.DoesNotThrow((Action)(() => item.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size))));
        Assert.That(typeof(BootstrapListGroupItem).GetProperty("BorderRadius", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Null);
    }

    private static int GetThemeSubscriptionCount()
    {
        var field = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        var handler = (Delegate?)field?.GetValue(null);
        return handler?.GetInvocationList().Length ?? 0;
    }

    private sealed class SelectabilityProbeItem : BootstrapListGroupItem
    {
        public bool HasSelectableStyle => GetStyle(ControlStyles.Selectable);

        public bool HasStyle(ControlStyles style) => GetStyle(style);
    }
}
