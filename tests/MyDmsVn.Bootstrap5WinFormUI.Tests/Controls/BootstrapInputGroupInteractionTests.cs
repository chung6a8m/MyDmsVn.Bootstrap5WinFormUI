using System;
using System.Drawing;
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

    private static Message CreateKeyMessage(Keys key)
    {
        return Message.Create(IntPtr.Zero, 0x0104, (IntPtr)(int)key, IntPtr.Zero);
    }
}
