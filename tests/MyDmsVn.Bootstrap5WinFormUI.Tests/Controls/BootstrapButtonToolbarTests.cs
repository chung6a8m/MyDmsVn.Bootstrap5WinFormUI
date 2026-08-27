using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapButtonToolbarTests
{
    [Test]
    public void DefaultsMatchPhase7Contract()
    {
        using var toolbar = new BootstrapButtonToolbar();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(toolbar.Orientation, Is.EqualTo(Orientation.Horizontal));
            Assert.That(toolbar.GroupSpacing, Is.EqualTo(8));
            Assert.That(toolbar.Alignment, Is.EqualTo(BootstrapToolbarAlignment.Left));
            Assert.That(toolbar.AutoSize, Is.True);
            Assert.That(toolbar.TabStop, Is.False);
        }));
    }

    [Test]
    public void LeftAlignmentUsesConfiguredGroupSpacing()
    {
        using var toolbar = CreateFixedToolbar(new Size(400, 60));
        toolbar.GroupSpacing = 12;
        toolbar.Alignment = BootstrapToolbarAlignment.Left;
        using var first = CreateFixedGroup(new Size(100, 32));
        using var second = CreateFixedGroup(new Size(80, 32));
        toolbar.Controls.Add(first);
        toolbar.Controls.Add(second);

        toolbar.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Left, Is.EqualTo(0));
            Assert.That(second.Left, Is.EqualTo(112));
        }));
    }

    [Test]
    public void CenterAlignmentCentersCombinedGroups()
    {
        using var toolbar = CreateFixedToolbar(new Size(400, 60));
        toolbar.GroupSpacing = 20;
        toolbar.Alignment = BootstrapToolbarAlignment.Center;
        using var first = CreateFixedGroup(new Size(100, 32));
        using var second = CreateFixedGroup(new Size(80, 32));
        toolbar.Controls.Add(first);
        toolbar.Controls.Add(second);

        toolbar.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Left, Is.EqualTo(100));
            Assert.That(second.Left, Is.EqualTo(220));
        }));
    }

    [Test]
    public void RightAlignmentAnchorsCombinedGroupsToTrailingEdge()
    {
        using var toolbar = CreateFixedToolbar(new Size(400, 60));
        toolbar.GroupSpacing = 20;
        toolbar.Alignment = BootstrapToolbarAlignment.Right;
        using var first = CreateFixedGroup(new Size(100, 32));
        using var second = CreateFixedGroup(new Size(80, 32));
        toolbar.Controls.Add(first);
        toolbar.Controls.Add(second);

        toolbar.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Left, Is.EqualTo(200));
            Assert.That(second.Left, Is.EqualTo(320));
        }));
    }

    [Test]
    public void SpaceBetweenAnchorsFirstAndLastGroupsToEdges()
    {
        using var toolbar = CreateFixedToolbar(new Size(400, 60));
        toolbar.GroupSpacing = 8;
        toolbar.Alignment = BootstrapToolbarAlignment.SpaceBetween;
        using var first = CreateFixedGroup(new Size(100, 32));
        using var second = CreateFixedGroup(new Size(80, 32));
        toolbar.Controls.Add(first);
        toolbar.Controls.Add(second);

        toolbar.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Left, Is.EqualTo(0));
            Assert.That(second.Right, Is.EqualTo(toolbar.ClientSize.Width));
        }));
    }

    [Test]
    public void VerticalOrientationUsesGroupSpacingOnVerticalAxis()
    {
        using var toolbar = CreateFixedToolbar(new Size(160, 200));
        toolbar.Orientation = Orientation.Vertical;
        toolbar.GroupSpacing = 10;
        toolbar.Alignment = BootstrapToolbarAlignment.Left;
        using var first = CreateFixedGroup(new Size(100, 30));
        using var second = CreateFixedGroup(new Size(80, 40));
        toolbar.Controls.Add(first);
        toolbar.Controls.Add(second);

        toolbar.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Top, Is.EqualTo(0));
            Assert.That(second.Top, Is.EqualTo(40));
        }));
    }

    [Test]
    public void ToolbarDoesNotChangeButtonSelectionPolicy()
    {
        using var toolbar = new BootstrapButtonToolbar();
        using var group = new BootstrapButtonGroup
        {
            SelectionMode = BootstrapButtonSelectionMode.None
        };
        using var button = new BootstrapButton
        {
            Text = "Pinned",
            Selected = true
        };
        group.Controls.Add(button);
        toolbar.Controls.Add(group);

        button.PerformClick();

        Assert.That(button.Selected, Is.True);
    }

    [Test]
    public void GroupSpacingRejectsNegativeValues()
    {
        using var toolbar = new BootstrapButtonToolbar();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => toolbar.GroupSpacing = -1));
        Assert.DoesNotThrow((Action)(() => toolbar.GroupSpacing = 0));
    }

    private static BootstrapButtonToolbar CreateFixedToolbar(Size size)
    {
        return new BootstrapButtonToolbar
        {
            AutoSize = false,
            Size = size,
            Padding = Padding.Empty
        };
    }

    private static BootstrapButtonGroup CreateFixedGroup(Size size)
    {
        return new BootstrapButtonGroup
        {
            AutoSize = false,
            Size = size
        };
    }
}
