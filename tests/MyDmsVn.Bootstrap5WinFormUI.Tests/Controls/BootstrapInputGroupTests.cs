using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapInputGroupTests
{
    [Test]
    public void DefaultsExposeOnlyGroupSizeAndRemainNonFocusable()
    {
        using var group = new BootstrapInputGroup();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(Enum.GetNames(typeof(BootstrapInputGroupSize)), Is.EqualTo(new[] { "Small", "Default", "Large" }));
            Assert.That(group.InputGroupSize, Is.EqualTo(BootstrapInputGroupSize.Default));
            Assert.That(group.TabStop, Is.False);
            Assert.That(group.BackColor, Is.EqualTo(Color.Transparent));
        }));
    }

    [Test]
    public void SupportedChildrenShareOneRowAndStretchInputsUseRemainingWidth()
    {
        using var group = new BootstrapInputGroup { Size = new Size(400, 40) };
        using var prefix = new BootstrapInputGroupText { Text = "@" };
        using var input = new BootstrapTextBox();
        using var button = new BootstrapButton { Text = "Search" };
        group.Controls.AddRange(new Control[] { prefix, input, button });

        group.PerformLayout();

        Assert.That(group.Controls.Cast<Control>().All(control => control.Height == prefix.Height), Is.True);
        Assert.That(input.Width, Is.GreaterThan(prefix.Width));
        Assert.That(input.Right <= group.ClientSize.Width, Is.True);
        Assert.That(button.Right <= group.ClientSize.Width, Is.True);
    }

    [Test]
    public void UnsupportedAdmissionIsAtomicAndKeepsPreviousParent()
    {
        using var previous = new Panel();
        using var unsupported = new TextBox();
        previous.Controls.Add(unsupported);
        using var group = new BootstrapInputGroup();

        Assert.Throws<NotSupportedException>((Action)(() => group.Controls.Add(unsupported)));

        Assert.That(unsupported.Parent, Is.SameAs(previous));
        Assert.That(group.Controls, Is.Empty);
    }

    [Test]
    public void MultipleSelectIsRejectedBeforeCollectionMutation()
    {
        using var group = new BootstrapInputGroup();
        using var select = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Multiple };

        Assert.Throws<NotSupportedException>((Action)(() => group.Controls.Add(select)));

        Assert.That(select.Parent, Is.Null);
        Assert.That(group.Controls, Is.Empty);
    }

    [Test]
    public void RemovalClearsConnectedOverrides()
    {
        using var group = new BootstrapInputGroup();
        using var input = new BootstrapTextBox();
        group.Controls.Add(input);
        var connected = (IBootstrapConnectedControl)input;
        Assert.That(connected.ConnectedSizeOverride, Is.Not.Null);

        group.Controls.Remove(input);

        Assert.That(connected.ConnectedSizeOverride, Is.Null);
        Assert.That(connected.ConnectedCornerRadius, Is.Null);
    }

    [Test]
    public void SetChildIndexChangesCanonicalVisualOrder()
    {
        using var group = new BootstrapInputGroup { Size = new Size(300, 40) };
        using var first = new BootstrapInputGroupText { Text = "A" };
        using var second = new BootstrapInputGroupText { Text = "B" };
        using var third = new BootstrapInputGroupText { Text = "C" };
        group.Controls.AddRange(new Control[] { first, second, third });
        group.Controls.SetChildIndex(third, 0);
        group.PerformLayout();

        Assert.That(third.Left, Is.EqualTo(0));
        Assert.That(first.Left, Is.GreaterThan(third.Left));
        Assert.That(second.Left, Is.GreaterThan(first.Left));
    }

    [Test]
    public void RightToLeftMirrorsPlacementButNotControlOrder()
    {
        using var group = new BootstrapInputGroup { Size = new Size(250, 40), RightToLeft = RightToLeft.Yes };
        using var first = new BootstrapInputGroupText { Text = "A" };
        using var second = new BootstrapTextBox();
        group.Controls.AddRange(new Control[] { first, second });
        group.PerformLayout();

        Assert.That(first.Right, Is.EqualTo(group.ClientSize.Width));
        Assert.That(group.Controls[0], Is.SameAs(first));
    }
}
