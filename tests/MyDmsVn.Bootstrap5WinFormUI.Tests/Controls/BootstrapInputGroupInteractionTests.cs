using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapInputGroupInteractionTests
{
    [Test]
    public void GroupDoesNotInsertATabStopAndChildrenKeepNativeTraversalOrder()
    {
        using var form = new Form();
        using var before = new Button { TabIndex = 0 };
        using var group = new BootstrapInputGroup { TabIndex = 1 };
        using var input = new BootstrapTextBox { TabIndex = 0 };
        using var action = new BootstrapButton { TabIndex = 1 };
        using var after = new Button { TabIndex = 2 };
        group.Controls.AddRange(new Control[] { input, action });
        form.Controls.AddRange(new Control[] { before, group, after });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group.TabStop, Is.False);
            Assert.That(group.GetNextControl(input, true), Is.SameAs(action));
            Assert.That(group.GetNextControl(action, false), Is.SameAs(input));
            Assert.That(form.GetNextControl(group, true), Is.SameAs(input));
        }));
    }

    [Test]
    public void HidingMiddleChildRecomputesVisibleCornersWithoutLosingItsPosition()
    {
        using var group = new BootstrapInputGroup { Size = new Size(300, 40) };
        using var first = new BootstrapInputGroupText { Text = "A" };
        using var middle = new BootstrapInputGroupText { Text = "B" };
        using var last = new BootstrapInputGroupText { Text = "C" };
        group.Controls.AddRange(new Control[] { first, middle, last });

        middle.Visible = false;
        group.PerformLayout();
        var firstRadius = ((IBootstrapConnectedControl)first).ConnectedCornerRadius;
        var lastRadius = ((IBootstrapConnectedControl)last).ConnectedCornerRadius;
        Assert.That(firstRadius, Is.EqualTo(new CornerRadius(firstRadius!.Value.TopLeft, 0f, 0f, firstRadius.Value.BottomLeft)));
        Assert.That(lastRadius, Is.EqualTo(new CornerRadius(0f, lastRadius!.Value.TopRight, lastRadius.Value.BottomRight, 0f)));
        Assert.That(((IBootstrapConnectedControl)middle).ConnectedCornerRadius, Is.Null);

        middle.Visible = true;
        group.PerformLayout();
        Assert.That(first.Left, Is.LessThan(middle.Left));
        Assert.That(middle.Left, Is.LessThan(last.Left));
    }

    [Test]
    public void FocusAndAltDoNotMutateCanonicalLayoutOrder()
    {
        using var group = new BootstrapInputGroup { Size = new Size(300, 40) };
        using var first = new BootstrapTextBox();
        using var second = new BootstrapButton { Text = "Go" };
        group.Controls.AddRange(new Control[] { first, second });
        group.PerformLayout();
        var firstBounds = first.Bounds;
        var secondBounds = second.Bounds;

        first.Focus();
        var message = CreateKeyMessage(Keys.Menu);
        first.PreProcessMessage(ref message);
        group.PerformLayout();

        Assert.That(first.Bounds, Is.EqualTo(firstBounds));
        Assert.That(second.Bounds, Is.EqualTo(secondBounds));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ActiveButtonBorderRemainsVisibleAtOverlappedSeam(bool pressed)
    {
        using var group = new BootstrapInputGroup { Width = 220 };
        using var first = new TestBootstrapButton { Text = "First", Variant = BootstrapVariant.Secondary };
        using var active = new TestBootstrapButton { Text = "Active", Variant = BootstrapVariant.Danger };
        group.Controls.AddRange(new Control[] { first, active });
        group.PerformLayout();
        active.TriggerMouseEnter();
        if (pressed)
        {
            active.TriggerMouseDown();
        }

        using var firstBitmap = new Bitmap(first.Width, first.Height);
        using var activeBitmap = new Bitmap(active.Width, active.Height);
        using var groupBitmap = RenderChildrenInZOrder(group);
        first.DrawToBitmap(firstBitmap, first.ClientRectangle);
        active.DrawToBitmap(activeBitmap, active.ClientRectangle);
        var sampleY = active.Height / 2;
        var normalNeighborPixel = firstBitmap.GetPixel(first.Width - 1, sampleY).ToArgb();
        var activeSeamPixel = activeBitmap.GetPixel(0, sampleY).ToArgb();
        var composedSeamPixel = groupBitmap.GetPixel(active.Left, active.Top + sampleY).ToArgb();
        var distanceToActive = ColorDistance(composedSeamPixel, activeSeamPixel);
        var distanceToNormal = ColorDistance(composedSeamPixel, normalNeighborPixel);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(activeSeamPixel, Is.Not.EqualTo(normalNeighborPixel));
            Assert.That(distanceToActive, Is.LessThan(distanceToNormal));
        }));
    }

    [Test]
    public void ActiveStackingRestoresZOrderWithoutChangingCanonicalLayout()
    {
        using var group = new BootstrapInputGroup { Width = 220 };
        using var first = new TestBootstrapButton { Text = "First" };
        using var active = new TestBootstrapButton { Text = "Active" };
        group.Controls.AddRange(new Control[] { first, active });
        group.PerformLayout();
        var firstBounds = first.Bounds;
        var activeBounds = active.Bounds;

        active.TriggerMouseEnter();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group.Controls.GetChildIndex(active), Is.EqualTo(0));
            Assert.That(first.Bounds, Is.EqualTo(firstBounds));
            Assert.That(active.Bounds, Is.EqualTo(activeBounds));
        }));

        active.TriggerMouseLeave();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group.Controls.GetChildIndex(first), Is.EqualTo(0));
            Assert.That(first.Bounds, Is.EqualTo(firstBounds));
            Assert.That(active.Bounds, Is.EqualTo(activeBounds));
        }));
    }

    [Test]
    public void PressedChildOutranksHoveredChild()
    {
        using var group = new BootstrapInputGroup { Width = 220 };
        using var hovered = new TestBootstrapButton { Text = "Hovered" };
        using var pressed = new TestBootstrapButton { Text = "Pressed" };
        group.Controls.AddRange(new Control[] { hovered, pressed });

        hovered.TriggerMouseEnter();
        pressed.TriggerMouseEnter();
        pressed.TriggerMouseDown();

        Assert.That(group.Controls.GetChildIndex(pressed), Is.EqualTo(0));
    }

    [Test]
    public void FocusedChildOutranksHoveredChild()
    {
        using var form = new Form();
        using var group = new BootstrapInputGroup { Width = 220 };
        using var hovered = new TestBootstrapButton { Text = "Hovered" };
        using var focused = new TestBootstrapButton { Text = "Focused" };
        group.Controls.AddRange(new Control[] { hovered, focused });
        form.Controls.Add(group);
        form.Show();
        Assert.That(focused.Focus(), Is.True);
        hovered.TriggerMouseEnter();

        Assert.That(group.Controls.GetChildIndex(focused), Is.EqualTo(0));
    }

    [Test]
    public void NestedSplitRegionInteractionRaisesWholeSplitButton()
    {
        using var group = new BootstrapInputGroup { Width = 260 };
        using var first = new BootstrapButton { Text = "First" };
        using var split = new BootstrapSplitButton { Text = "Split" };
        group.Controls.AddRange(new Control[] { first, split });
        var primaryRegion = (BootstrapButton)split.Controls[0];

        RaiseMouseEnter(primaryRegion);

        Assert.That(group.Controls.GetChildIndex(split), Is.EqualTo(0));
    }

    private static Message CreateKeyMessage(Keys key)
    {
        return Message.Create(IntPtr.Zero, 0x0104, (IntPtr)(int)key, IntPtr.Zero);
    }

    private static int ColorDistance(int leftArgb, int rightArgb)
    {
        var left = Color.FromArgb(leftArgb);
        var right = Color.FromArgb(rightArgb);
        return Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B);
    }

    private static Bitmap RenderChildrenInZOrder(Control parent)
    {
        var result = new Bitmap(parent.Width, parent.Height);
        using var graphics = Graphics.FromImage(result);
        for (var index = parent.Controls.Count - 1; index >= 0; index--)
        {
            var child = parent.Controls[index];
            using var childBitmap = new Bitmap(child.Width, child.Height);
            child.DrawToBitmap(childBitmap, child.ClientRectangle);
            graphics.DrawImageUnscaled(childBitmap, child.Left, child.Top);
        }
        return result;
    }

    private static void RaiseMouseEnter(BootstrapButton button)
    {
        var method = typeof(BootstrapButton).GetMethod("OnMouseEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(button, new object[] { EventArgs.Empty });
    }

    private sealed class TestBootstrapButton : BootstrapButton
    {
        internal void TriggerMouseEnter() => OnMouseEnter(EventArgs.Empty);

        internal void TriggerMouseDown() => OnMouseDown(
            new MouseEventArgs(MouseButtons.Left, 1, Width / 2, Height / 2, 0));

        internal void TriggerMouseLeave() => OnMouseLeave(EventArgs.Empty);
    }
}
