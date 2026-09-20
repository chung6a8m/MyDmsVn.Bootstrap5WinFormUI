using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListGroupTests
{
    [Test]
    public void DefaultsDefineACompositionOnlyListContract()
    {
        using var group = new BootstrapListGroup();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group, Is.InstanceOf<Panel>());
            Assert.That(group.Orientation, Is.EqualTo(Orientation.Vertical));
            Assert.That(group.Flush, Is.False);
            Assert.That(group.BorderRadius, Is.EqualTo(-1));
            Assert.That(group.AutoSize, Is.True);
            Assert.That(group.AutoSizeMode, Is.EqualTo(AutoSizeMode.GrowAndShrink));
            Assert.That(group.TabStop, Is.False);
            Assert.That(group.AccessibleRole, Is.EqualTo(AccessibleRole.List));
        }));
    }

    [Test]
    public void InvalidOrientationAndRadiusAreRejected()
    {
        using var group = new BootstrapListGroup();

        Assert.Throws<InvalidEnumArgumentException>((Action)(() => group.Orientation = (Orientation)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => group.BorderRadius = -2));
    }

    [Test]
    public void ItemsIsAFreshSnapshotOfDirectItemChildrenInControlIndexOrder()
    {
        using var group = new BootstrapListGroup();
        using var first = new BootstrapListGroupItem { Text = "First" };
        using var second = new BootstrapListGroupItem { Text = "Second" };
        using var ignored = new Label();
        group.Controls.Add(first);
        group.Controls.Add(ignored);
        group.Controls.Add(second);

        AssertItemsMatchControls(group);
        var beforeReorder = group.Items;

        group.Controls.SetChildIndex(second, 0);

        AssertItemsMatchControls(group);
        Assert.That(beforeReorder, Is.Not.SameAs(group.Items));
        Assert.That(beforeReorder.ToArray(), Is.Not.EqualTo(group.Items.ToArray()));

        first.BringToFront();
        AssertItemsMatchControls(group);
        first.SendToBack();
        AssertItemsMatchControls(group);
    }

    [Test]
    public void HelpersUseNormalControlOwnershipAndNeverDisposeRemovedItems()
    {
        using var group = new BootstrapListGroup();
        using var otherParent = new Panel();
        var created = group.AddItem("Created");
        var supplied = new BootstrapListGroupItem { Text = "Supplied" };
        group.AddItem(supplied);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(created.Parent, Is.SameAs(group));
            Assert.That(supplied.Parent, Is.SameAs(group));
            Assert.That(group.Items, Does.Contain(created));
            Assert.That(group.Items, Does.Contain(supplied));
        }));

        otherParent.Controls.Add(supplied);
        Assert.That(group.Items, Does.Not.Contain(supplied));
        group.AddItem(supplied);
        Assert.That(supplied.Parent, Is.SameAs(group));

        Assert.That(group.RemoveItem(supplied), Is.True);
        Assert.That(supplied.IsDisposed, Is.False);
        Assert.That(group.RemoveItem(supplied), Is.False);

        group.ClearItems();
        Assert.That(created.IsDisposed, Is.False);
        Assert.That(group.Items, Is.Empty);

        supplied.Dispose();
        created.Dispose();
    }

    [Test]
    public void RemoveAndReaddDoesNotDuplicateItemClickSubscription()
    {
        using var group = new BootstrapListGroup();
        using var item = new ClickProbeItem { Actionable = true };
        var count = 0;
        group.ItemClick += (_, e) =>
        {
            Assert.That(e.Item, Is.SameAs(item));
            count++;
        };

        group.Controls.Add(item);
        group.Controls.Remove(item);
        group.Controls.Add(item);
        item.RaiseClick();

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void VerticalLayoutUsesFreshVisibleControlOrderAndStableSeamOverlap()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Size = new Size(240, 200),
            Padding = new Padding(4),
            BorderRadius = 8
        };
        using var first = new FixedPreferredItem(100, 40);
        using var hidden = new FixedPreferredItem(100, 50) { Visible = false };
        using var last = new FixedPreferredItem(100, 30);
        group.Controls.Add(first);
        group.Controls.Add(hidden);
        group.Controls.Add(last);

        group.PerformLayout();
        var firstPass = new[] { first.Bounds, last.Bounds };
        group.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Bounds, Is.EqualTo(firstPass[0]));
            Assert.That(last.Bounds, Is.EqualTo(firstPass[1]));
            Assert.That(first.Left, Is.EqualTo(group.Padding.Left));
            Assert.That(first.Width, Is.EqualTo(group.ClientSize.Width - group.Padding.Horizontal));
            Assert.That(last.Top, Is.LessThan(first.Bottom));
            Assert.That(hidden.Bounds, Is.Not.EqualTo(first.Bounds));
            Assert.That(first.ConnectedCorners, Is.EqualTo(new CornerRadius(8, 8, 0, 0)));
            Assert.That(last.ConnectedCorners, Is.EqualTo(new CornerRadius(0, 0, 8, 8)));
        }));

        group.Controls.SetChildIndex(last, 0);
        group.PerformLayout();
        Assert.That(last.Top, Is.EqualTo(group.Padding.Top));
        Assert.That(group.Items[0], Is.SameAs(last));
        Assert.That(last.ConnectedCorners, Is.EqualTo(new CornerRadius(8, 8, 0, 0)));
    }

    [Test]
    public void HorizontalLayoutPreservesPreferredWidthsAndFlushIsNoOp()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Orientation = Orientation.Horizontal,
            Flush = true,
            BorderRadius = 7,
            Size = new Size(300, 100)
        };
        using var first = new FixedPreferredItem(80, 30);
        using var second = new FixedPreferredItem(120, 40);
        group.Controls.Add(first);
        group.Controls.Add(second);
        group.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Width, Is.EqualTo(80));
            Assert.That(second.Width, Is.EqualTo(120));
            Assert.That(second.Left, Is.LessThan(first.Right));
            Assert.That(group.Flush, Is.True);
            Assert.That(group.BorderRadius, Is.EqualTo(7));
            Assert.That(first.ConnectedCorners, Is.EqualTo(new CornerRadius(7, 0, 0, 7)));
            Assert.That(second.ConnectedCorners, Is.EqualTo(new CornerRadius(0, 7, 7, 0)));
        }));
    }

    [Test]
    public void AutoSizeAggregatesPreferredItemsWithoutNegativeBounds()
    {
        using var group = new BootstrapListGroup { Width = 220, Padding = new Padding(3) };
        using var first = new FixedPreferredItem(100, 30);
        using var second = new FixedPreferredItem(120, 40);
        group.Controls.Add(first);
        group.Controls.Add(second);
        group.PerformLayout();

        Assert.That(group.Height, Is.GreaterThan(0));
        Assert.That(group.Items.All(item => item.Left >= 0 && item.Top >= 0 && item.Width >= 0 && item.Height >= 0), Is.True);
        Assert.That(group.GetPreferredSize(Size.Empty).Height, Is.EqualTo(group.Height));
    }

    [Test]
    public void VerticalLayoutPreservesExplicitItemHeightWhenAutoSizeIsDisabled()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Size = new Size(240, 200)
        };
        using var item = new BootstrapListGroupItem
        {
            AutoSize = false,
            Size = new Size(180, 96),
            Text = "Short"
        };
        group.Controls.Add(item);
        group.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.Width, Is.EqualTo(group.ClientSize.Width));
            Assert.That(item.Height, Is.EqualTo(96));
        }));
    }

    [Test]
    public void HorizontalLayoutPreservesExplicitItemSizeWhenAutoSizeIsDisabled()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Orientation = Orientation.Horizontal,
            Size = new Size(480, 160)
        };
        using var item = new BootstrapListGroupItem
        {
            AutoSize = false,
            Size = new Size(180, 96),
            Text = "Short"
        };
        group.Controls.Add(item);
        group.PerformLayout();

        Assert.That(item.Size, Is.EqualTo(new Size(180, 96)));
    }

    [Test]
    public void RichChildRuntimeGeometryChangesRelayoutTheOwningGroup()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Size = new Size(240, 200)
        };
        using var item = new BootstrapListGroupItem();
        using var child = new Label { Bounds = new Rectangle(8, 6, 80, 20) };
        group.Controls.Add(item);
        group.PerformLayout();
        var baselineHeight = item.Height;

        item.Controls.Add(child);
        var addedHeight = item.Height;
        child.Height = 72;
        var grownHeight = item.Height;
        child.Visible = false;
        var hiddenHeight = item.Height;
        child.Visible = true;
        var restoredHeight = item.Height;
        item.Controls.Remove(child);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(addedHeight, Is.GreaterThanOrEqualTo(child.Top + 20));
            Assert.That(grownHeight, Is.GreaterThanOrEqualTo(child.Top + 72));
            Assert.That(grownHeight, Is.GreaterThan(addedHeight));
            Assert.That(hiddenHeight, Is.EqualTo(baselineHeight));
            Assert.That(restoredHeight, Is.EqualTo(grownHeight));
            Assert.That(item.Height, Is.EqualTo(baselineHeight));
        }));
    }

    [Test]
    public void RichChildIntrinsicPreferredSizeChangesRelayoutTheOwningGroup()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Orientation = Orientation.Horizontal,
            Size = new Size(640, 120)
        };
        using var item = new BootstrapListGroupItem();
        using var child = new IntrinsicPreferredSizeControl
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 24,
            Text = "Short"
        };
        item.Controls.Add(child);
        group.Controls.Add(item);
        group.PerformLayout();
        var baselineItemWidth = item.Width;

        child.Text = "A much longer rich-content label that requires additional horizontal space";

        Assert.That(item.Width, Is.GreaterThan(baselineItemWidth));
    }

    [Test]
    public void RichChildAnchorChangesRelayoutTheOwningGroup()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Orientation = Orientation.Horizontal,
            Size = new Size(480, 120)
        };
        using var item = new BootstrapListGroupItem();
        using var child = new Label
        {
            AutoSize = false,
            Bounds = new Rectangle(100, 8, 20, 20),
            Text = "Rich content"
        };
        item.Controls.Add(child);
        group.Controls.Add(item);
        group.PerformLayout();
        var leftAnchoredWidth = item.Width;

        child.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        Assert.That(item.Width, Is.LessThan(leftAnchoredWidth));
    }

    [Test]
    public void RichChildDockChangesRelayoutWithoutIncidentalBoundsEvents()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Orientation = Orientation.Horizontal,
            Size = new Size(480, 120)
        };
        using var item = new BootstrapListGroupItem();
        using var child = new Label
        {
            AutoSize = false,
            Bounds = new Rectangle(100, 8, 20, 20),
            Text = "Rich content"
        };
        item.Controls.Add(child);
        group.Controls.Add(item);
        group.PerformLayout();
        var undockedWidth = item.Width;

        item.SuspendLayout();
        try
        {
            child.Dock = DockStyle.Right;

            Assert.That(item.Width, Is.LessThan(undockedWidth));
        }
        finally
        {
            item.ResumeLayout(false);
        }
    }

    [Test]
    public void DockedRichChildHasStableBoundsAcrossRepeatedLayoutPasses()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Size = new Size(240, 200)
        };
        using var item = new BootstrapListGroupItem();
        var child = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Docked rich content"
        };
        item.Controls.Add(child);
        group.Controls.Add(item);
        group.PerformLayout();
        var stableBounds = item.Bounds;

        for (var pass = 0; pass < 5; pass++)
        {
            group.PerformLayout();
            Assert.That(item.Bounds, Is.EqualTo(stableBounds));
        }
    }

    [Test]
    public void StretchAnchoredRichChildHasStableBoundsAcrossRepeatedLayoutPasses()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Size = new Size(240, 200)
        };
        using var item = new BootstrapListGroupItem { Size = new Size(220, 100) };
        var child = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom,
            Bounds = new Rectangle(8, 6, 100, 20),
            Text = "Stretch anchored content"
        };
        item.Controls.Add(child);
        var childPreferredHeight = child.GetPreferredSize(Size.Empty).Height;
        group.Controls.Add(item);
        group.PerformLayout();
        var stableBounds = item.Bounds;

        for (var pass = 0; pass < 5; pass++)
        {
            group.PerformLayout();
            Assert.That(item.Bounds, Is.EqualTo(stableBounds));
            Assert.That(child.Height, Is.GreaterThanOrEqualTo(childPreferredHeight));
        }
    }

    [Test]
    public void FarEdgeHorizontalAnchorPreservesUsableChildWidth()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Orientation = Orientation.Horizontal,
            Size = new Size(400, 160)
        };
        using var item = new BootstrapListGroupItem { Size = new Size(220, 60) };
        var child = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Bounds = new Rectangle(10, 8, 80, 20),
            Text = "Anchored"
        };
        item.Controls.Add(child);
        var childPreferredWidth = child.GetPreferredSize(Size.Empty).Width;
        group.Controls.Add(item);
        group.PerformLayout();
        var stableBounds = item.Bounds;

        for (var pass = 0; pass < 5; pass++)
        {
            group.PerformLayout();
            Assert.That(item.Bounds, Is.EqualTo(stableBounds));
            Assert.That(child.Width, Is.GreaterThanOrEqualTo(childPreferredWidth));
        }
    }

    [Test]
    public void SingleFarEdgeAnchorsKeepChildVisibleAndUsable()
    {
        using var group = new BootstrapListGroup
        {
            AutoSize = false,
            Orientation = Orientation.Horizontal,
            Size = new Size(400, 160)
        };
        using var item = new BootstrapListGroupItem { Size = new Size(220, 100) };
        var child = new Label
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Bounds = new Rectangle(70, 20, 80, 30),
            Text = "Anchored"
        };
        item.Controls.Add(child);
        var childPreferred = child.GetPreferredSize(Size.Empty);
        group.Controls.Add(item);
        group.PerformLayout();
        var stableBounds = item.Bounds;

        for (var pass = 0; pass < 5; pass++)
        {
            group.PerformLayout();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(item.Bounds, Is.EqualTo(stableBounds));
                Assert.That(child.Left, Is.GreaterThanOrEqualTo(0));
                Assert.That(child.Top, Is.GreaterThanOrEqualTo(0));
                Assert.That(child.Width, Is.GreaterThanOrEqualTo(childPreferred.Width));
                Assert.That(child.Height, Is.GreaterThanOrEqualTo(childPreferred.Height));
            }));
        }
    }

    [Test]
    public void NavigationUsesCurrentControlOrderAndSkipsIneligibleItems()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var group = new BootstrapListGroup { AutoSize = false, Size = new Size(220, 180) };
        using var first = new BootstrapListGroupItem { Actionable = true };
        using var skipped = new BootstrapListGroupItem { Actionable = true, Enabled = false };
        using var last = new BootstrapListGroupItem { Actionable = true };
        group.Controls.Add(first);
        group.Controls.Add(skipped);
        group.Controls.Add(last);
        form.Controls.Add(group);
        form.Show();

        Assert.That(first.Focus(), Is.True);
        Assert.That(group.NavigateFrom(first, Keys.Down), Is.True);
        Assert.That(last.Focused, Is.True);
        group.Controls.SetChildIndex(last, 0);
        Assert.That(group.NavigateFrom(last, Keys.Home), Is.True);
        Assert.That(last.Focused, Is.True);
        Assert.That(group.NavigateFrom(last, Keys.End), Is.True);
        Assert.That(first.Focused, Is.True);

        group.Orientation = Orientation.Horizontal;
        Assert.That(last.Focus(), Is.True);
        Assert.That(group.NavigateFrom(last, Keys.Right), Is.True);
        Assert.That(first.Focused, Is.True);
        Assert.That(group.NavigateFrom(first, Keys.Tab), Is.False);
        Assert.That(first.Active, Is.False);
        Assert.That(last.Active, Is.False);
    }

    private static void AssertItemsMatchControls(BootstrapListGroup group)
    {
        var expected = group.Controls.Cast<Control>().OfType<BootstrapListGroupItem>().ToArray();
        Assert.That(group.Items, Is.EqualTo(expected));
    }

    private sealed class ClickProbeItem : BootstrapListGroupItem
    {
        public void RaiseClick() => OnClick(EventArgs.Empty);
    }

    private sealed class FixedPreferredItem : BootstrapListGroupItem
    {
        private readonly Size _preferred;

        public FixedPreferredItem(int width, int height) => _preferred = new Size(width, height);

        public override Size GetPreferredSize(Size proposedSize) => _preferred;
    }

    private sealed class IntrinsicPreferredSizeControl : Control
    {
        public override Size GetPreferredSize(Size proposedSize)
        {
            return string.IsNullOrEmpty(Text)
                ? Size.Empty
                : TextRenderer.MeasureText(
                    Text,
                    Font,
                    Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
    }
}
