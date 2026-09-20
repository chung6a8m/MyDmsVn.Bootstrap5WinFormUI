using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListGroupItemTests
{
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int MkLButton = 0x0001;

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
    public void DisablingThemeFontDetachesTheOwnedFontBeforeDisposal()
    {
        using var item = new BootstrapListGroupItem { Text = "Profile" };
        var ownedThemeFont = item.Font;

        item.UseThemeFont = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.Font, Is.Not.SameAs(ownedThemeFont));
            Assert.DoesNotThrow((Action)(() => _ = item.GetPreferredSize(Size.Empty)));
        }));
    }

    [Test]
    public void TinyItemPaintsWithoutInvalidGeometryAndExposesNoRadiusProperty()
    {
        using var item = new BootstrapListGroupItem { Size = new Size(1, 1) };
        using var bitmap = new Bitmap(1, 1);

        Assert.DoesNotThrow((Action)(() => item.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size))));
        Assert.That(typeof(BootstrapListGroupItem).GetProperty("BorderRadius", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Null);
    }

    [Test]
    public void DirectMouseEnterAndSpaceActivateExactlyOnceWithoutChangingActive()
    {
        using var item = new InteractionProbeItem { Actionable = true, Size = new Size(160, 40) };
        var clicks = 0;
        item.Click += (_, _) => clicks++;

        item.RaiseMouseClick();
        item.RaiseKeyDown(Keys.Enter);
        item.RaiseKeyDown(Keys.Space);
        item.RaiseKeyUp(Keys.Space);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(clicks, Is.EqualTo(3));
            Assert.That(item.Active, Is.False);
        }));
    }

    [Test]
    public void DisabledOrNonActionableItemsDoNotActivate()
    {
        using var item = new InteractionProbeItem { Size = new Size(160, 40) };
        var clicks = 0;
        item.Click += (_, _) => clicks++;
        item.RaiseMouseClick();
        item.RaiseKeyDown(Keys.Enter);
        item.Actionable = true;
        item.Enabled = false;
        item.RaiseMouseClick();
        item.RaiseKeyDown(Keys.Enter);

        Assert.That(clicks, Is.Zero);
    }

    [Test]
    public void NativeMouseActivationFiresExactlyOnceOnlyForActionableItems()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Size = new Size(180, 100)
        };
        using var actionable = new BootstrapListGroupItem
        {
            Actionable = true,
            Bounds = new Rectangle(0, 0, 160, 40)
        };
        using var presentational = new BootstrapListGroupItem
        {
            Bounds = new Rectangle(0, 50, 160, 40)
        };
        group.Controls.Add(actionable);
        group.Controls.Add(presentational);
        form.Controls.Add(group);
        var actionableClicks = 0;
        var presentationalClicks = 0;
        var groupClicks = 0;
        actionable.Click += (_, _) => actionableClicks++;
        presentational.Click += (_, _) => presentationalClicks++;
        group.ItemClick += (_, _) => groupClicks++;
        form.Show();

        SendNativeClick(actionable);
        SendNativeClick(presentational);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actionableClicks, Is.EqualTo(1));
            Assert.That(presentationalClicks, Is.Zero);
            Assert.That(groupClicks, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ExactDecorativeTypesForwardButInteractiveAndUnknownControlsDoNot()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var group = new BootstrapListGroup();
        using var item = new BootstrapListGroupItem { Actionable = true };
        var label = new Label { Size = new Size(40, 20) };
        var badge = new BootstrapBadge { Text = "New", Size = new Size(40, 20) };
        var button = new Button();
        var link = new LinkLabelProbe();
        var customLabel = new CustomLabelProbe();
        var unknown = new UnknownMouseProbe();
        item.Controls.Add(label);
        item.Controls.Add(badge);
        item.Controls.Add(button);
        item.Controls.Add(link);
        item.Controls.Add(customLabel);
        item.Controls.Add(unknown);
        group.Controls.Add(item);
        form.Controls.Add(group);
        var itemClicks = 0;
        var groupClicks = 0;
        item.Click += (_, _) => itemClicks++;
        group.ItemClick += (_, _) => groupClicks++;
        form.Show();

        SendNativeClick(label);
        SendNativeClick(badge);
        button.PerformClick();
        link.RaiseMouseClick();
        customLabel.RaiseMouseClick();
        unknown.RaiseMouseClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(itemClicks, Is.EqualTo(2));
            Assert.That(groupClicks, Is.EqualTo(2));
        }));
    }

    [Test]
    public void DynamicDecorativeDescendantsAreUnsubscribedOnRemoval()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var item = new BootstrapListGroupItem { Actionable = true };
        using var panel = new Panel();
        using var label = new Label { Size = new Size(40, 20) };
        form.Controls.Add(item);
        item.Controls.Add(panel);
        panel.Controls.Add(label);
        var clicks = 0;
        item.Click += (_, _) => clicks++;
        form.Show();
        SendNativeClick(label);
        panel.Controls.Remove(label);
        SendNativeClick(label);

        Assert.That(clicks, Is.EqualTo(1));
    }

    [Test]
    public void AccessibleDefaultsTrackTextAndActionableRole()
    {
        using var item = new BootstrapListGroupItem { Text = "Profile" };
        Assert.That(item.AccessibleName, Is.EqualTo("Profile"));
        Assert.That(item.AccessibleRole, Is.EqualTo(AccessibleRole.ListItem));

        item.Actionable = true;
        Assert.That(item.AccessibleRole, Is.EqualTo(AccessibleRole.PushButton));
        item.AccessibleName = "Custom";
        item.Text = "Security";
        Assert.That(item.AccessibleName, Is.EqualTo("Custom"));
    }

    [Test]
    public void ThemeSwitchPreservesAllPublicListGroupStateAndGroupDisposalUnsubscribes()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        var baseline = GetThemeSubscriptionCount();
        var group = new BootstrapListGroup
        {
            Flush = true,
            Orientation = Orientation.Horizontal,
            BorderRadius = 9
        };
        var item = new BootstrapListGroupItem
        {
            Active = true,
            Actionable = true,
            Variant = BootstrapVariant.Warning,
            Enabled = false
        };
        group.Controls.Add(item);
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline + 2));

        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(group.Flush, Is.True);
                Assert.That(group.Orientation, Is.EqualTo(Orientation.Horizontal));
                Assert.That(group.BorderRadius, Is.EqualTo(9));
                Assert.That(item.Active, Is.True);
                Assert.That(item.Actionable, Is.True);
                Assert.That(item.Variant, Is.EqualTo(BootstrapVariant.Warning));
                Assert.That(item.Enabled, Is.False);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
            group.Dispose();
        }

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline));
    }

    [Test]
    public void PlainRichContentInheritsResolvedForegroundWithoutOverridingExplicitColor()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            var darkTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            BootstrapThemeManager.CurrentTheme = darkTheme;
            using var item = new BootstrapListGroupItem();
            var inheritedLabel = new Label();
            var explicitLabel = new Label { ForeColor = Color.Magenta };
            item.Controls.Add(inheritedLabel);
            item.Controls.Add(explicitLabel);
            var neutralForeground = BootstrapListGroupRenderLogic.ResolvePalette(
                darkTheme.Colors,
                null,
                BootstrapListGroupVisualState.Neutral).Foreground;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(item.ForeColor, Is.EqualTo(neutralForeground));
                Assert.That(inheritedLabel.ForeColor, Is.EqualTo(neutralForeground));
                Assert.That(explicitLabel.ForeColor, Is.EqualTo(Color.Magenta));
            }));

            var lightTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            BootstrapThemeManager.CurrentTheme = lightTheme;
            var lightNeutralForeground = BootstrapListGroupRenderLogic.ResolvePalette(
                lightTheme.Colors,
                null,
                BootstrapListGroupVisualState.Neutral).Foreground;
            Assert.That(inheritedLabel.ForeColor, Is.EqualTo(lightNeutralForeground));

            item.Active = true;
            var activeForeground = BootstrapListGroupRenderLogic.ResolvePalette(
                lightTheme.Colors,
                null,
                BootstrapListGroupVisualState.Active).Foreground;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(item.ForeColor, Is.EqualTo(activeForeground));
                Assert.That(inheritedLabel.ForeColor, Is.EqualTo(activeForeground));
                Assert.That(activeForeground, Is.Not.EqualTo(lightNeutralForeground));
                Assert.That(explicitLabel.ForeColor, Is.EqualTo(Color.Magenta));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
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

    private sealed class InteractionProbeItem : BootstrapListGroupItem
    {
        public void RaiseMouseClick()
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 4, 4, 0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 4, 4, 0));
        }

        public void RaiseKeyDown(Keys key) => OnKeyDown(new KeyEventArgs(key));

        public void RaiseKeyUp(Keys key) => OnKeyUp(new KeyEventArgs(key));
    }

    private sealed class CustomLabelProbe : Label
    {
        public void RaiseMouseClick()
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0));
        }
    }

    private sealed class LinkLabelProbe : LinkLabel
    {
        public void RaiseMouseClick()
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0));
        }
    }

    private sealed class UnknownMouseProbe : Control
    {
        public void RaiseMouseClick()
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0));
        }
    }

    private static void SendNativeClick(Control control)
    {
        var coordinates = new IntPtr(4 | (4 << 16));
        SendMessage(control.Handle, WmLButtonDown, new IntPtr(MkLButton), coordinates);
        SendMessage(control.Handle, WmLButtonUp, IntPtr.Zero, coordinates);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
