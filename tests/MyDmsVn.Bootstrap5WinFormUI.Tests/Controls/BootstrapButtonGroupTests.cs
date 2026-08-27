using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapButtonGroupTests
{
    [Test]
    public void DefaultsMatchPhase7Contract()
    {
        using var group = new BootstrapButtonGroup();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group.Orientation, Is.EqualTo(Orientation.Horizontal));
            Assert.That(group.SelectionMode, Is.EqualTo(BootstrapButtonSelectionMode.None));
            Assert.That(group.EqualWidth, Is.False);
            Assert.That(group.BorderRadius, Is.EqualTo(-1));
            Assert.That(group.AutoSize, Is.True);
            Assert.That(group.TabStop, Is.False);
        }));
    }

    [Test]
    public void SingleSelectionMovesSelectionToClickedButton()
    {
        using var group = new BootstrapButtonGroup
        {
            SelectionMode = BootstrapButtonSelectionMode.Single
        };
        using var first = new BootstrapButton { Text = "One" };
        using var second = new BootstrapButton { Text = "Two" };
        using var third = new BootstrapButton { Text = "Three" };
        group.Controls.Add(first);
        group.Controls.Add(second);
        group.Controls.Add(third);

        second.PerformClick();
        third.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Selected, Is.False);
            Assert.That(second.Selected, Is.False);
            Assert.That(third.Selected, Is.True);
        }));
    }

    [Test]
    public void MultipleSelectionTogglesButtonsIndependently()
    {
        using var group = new BootstrapButtonGroup
        {
            SelectionMode = BootstrapButtonSelectionMode.Multiple
        };
        using var first = new BootstrapButton { Text = "One" };
        using var second = new BootstrapButton { Text = "Two" };
        group.Controls.Add(first);
        group.Controls.Add(second);

        first.PerformClick();
        second.PerformClick();
        first.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Selected, Is.False);
            Assert.That(second.Selected, Is.True);
        }));
    }

    [Test]
    public void NoneSelectionModeLeavesButtonStateUntouched()
    {
        using var group = new BootstrapButtonGroup
        {
            SelectionMode = BootstrapButtonSelectionMode.None
        };
        using var first = new BootstrapButton { Text = "One", Selected = true };
        using var second = new BootstrapButton { Text = "Two" };
        group.Controls.Add(first);
        group.Controls.Add(second);

        second.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Selected, Is.True);
            Assert.That(second.Selected, Is.False);
        }));
    }

    [Test]
    public void HorizontalGroupAppliesOuterCornersOnly()
    {
        using var group = new BootstrapButtonGroup
        {
            BorderRadius = 10,
            Orientation = Orientation.Horizontal
        };
        using var first = new BootstrapButton { Text = "One" };
        using var middle = new BootstrapButton { Text = "Two" };
        using var last = new BootstrapButton { Text = "Three" };
        group.Controls.Add(first);
        group.Controls.Add(middle);
        group.Controls.Add(last);

        group.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.GroupCornerRadius, Is.EqualTo(new CornerRadius(10f, 0f, 0f, 10f)));
            Assert.That(middle.GroupCornerRadius, Is.EqualTo(CornerRadius.Empty));
            Assert.That(last.GroupCornerRadius, Is.EqualTo(new CornerRadius(0f, 10f, 10f, 0f)));
        }));
    }

    [Test]
    public void VerticalGroupAppliesOuterCornersOnly()
    {
        using var group = new BootstrapButtonGroup
        {
            BorderRadius = 8,
            Orientation = Orientation.Vertical
        };
        using var first = new BootstrapButton { Text = "One" };
        using var middle = new BootstrapButton { Text = "Two" };
        using var last = new BootstrapButton { Text = "Three" };
        group.Controls.Add(first);
        group.Controls.Add(middle);
        group.Controls.Add(last);

        group.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.GroupCornerRadius, Is.EqualTo(new CornerRadius(8f, 8f, 0f, 0f)));
            Assert.That(middle.GroupCornerRadius, Is.EqualTo(CornerRadius.Empty));
            Assert.That(last.GroupCornerRadius, Is.EqualTo(new CornerRadius(0f, 0f, 8f, 8f)));
        }));
    }

    [Test]
    public void VerticalGroupAlwaysUsesTheWidestPreferredButtonWidth()
    {
        using var group = new BootstrapButtonGroup
        {
            Orientation = Orientation.Vertical,
            EqualWidth = false
        };
        using var shortButton = new BootstrapButton { Text = "A" };
        using var longButton = new BootstrapButton { Text = "A much longer action" };
        group.Controls.Add(shortButton);
        group.Controls.Add(longButton);

        group.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(shortButton.Width, Is.EqualTo(longButton.Width));
            Assert.That(shortButton.Width, Is.EqualTo(longButton.GetPreferredSize(Size.Empty).Width));
        }));
    }

    [Test]
    public void EqualWidthUsesTheWidestPreferredButton()
    {
        using var group = new BootstrapButtonGroup
        {
            EqualWidth = true
        };
        using var shortButton = new BootstrapButton { Text = "A" };
        using var longButton = new BootstrapButton { Text = "A much longer action" };
        group.Controls.Add(shortButton);
        group.Controls.Add(longButton);

        group.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(shortButton.Width, Is.EqualTo(longButton.Width));
            Assert.That(shortButton.Width, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void RemovingButtonRestoresStandaloneCornerBehavior()
    {
        using var group = new BootstrapButtonGroup { BorderRadius = 12 };
        using var first = new BootstrapButton { Text = "One" };
        using var second = new BootstrapButton { Text = "Two" };
        group.Controls.Add(first);
        group.Controls.Add(second);
        group.PerformLayout();

        group.Controls.Remove(first);

        Assert.That(first.GroupCornerRadius, Is.Null);
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var group = new BootstrapButtonGroup();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => group.BorderRadius = -2));
        Assert.DoesNotThrow((Action)(() => group.BorderRadius = -1));
        Assert.DoesNotThrow((Action)(() => group.BorderRadius = 0));
    }
}
