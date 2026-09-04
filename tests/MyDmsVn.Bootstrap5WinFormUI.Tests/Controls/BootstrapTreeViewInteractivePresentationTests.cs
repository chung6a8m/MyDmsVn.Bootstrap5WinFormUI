using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTreeViewInteractivePresentationTests
{
    [Test]
    public void HoverTransition_MovesFrameworkHotPresentationAndInvalidatesOldAndNewRows()
    {
        using var treeView = CreateProbeTree();
        var first = treeView.Nodes[0];
        var second = treeView.Nodes[1];

        treeView.ResetInvalidations();
        treeView.RaiseMouseMove(GetLabelPoint(first));
        using var firstHot = RenderNode(treeView, first);
        Assert.That(ContainsColor(firstHot, GetRowBounds(treeView, first), BootstrapThemeManager.CurrentTheme.Colors.Hover), Is.True);

        treeView.ResetInvalidations();
        treeView.RaiseMouseMove(GetLabelPoint(second));
        using var firstCold = RenderNode(treeView, first);
        using var secondHot = RenderNode(treeView, second);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsColor(firstCold, GetRowBounds(treeView, first), BootstrapThemeManager.CurrentTheme.Colors.Hover), Is.False);
            Assert.That(ContainsColor(secondHot, GetRowBounds(treeView, second), BootstrapThemeManager.CurrentTheme.Colors.Hover), Is.True);
            Assert.That(treeView.InvalidatedRects.Exists(rect => rect.IntersectsWith(GetRowBounds(treeView, first))), Is.True);
            Assert.That(treeView.InvalidatedRects.Exists(rect => rect.IntersectsWith(GetRowBounds(treeView, second))), Is.True);
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FrameworkHover_DoesNotMutateSelectionOrInheritedHotTracking(bool hotTracking)
    {
        using var treeView = CreateProbeTree();
        var selected = treeView.Nodes[0];
        var hovered = treeView.Nodes[1];
        treeView.SelectedNode = selected;
        treeView.HotTracking = hotTracking;

        treeView.RaiseMouseMove(GetLabelPoint(hovered));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.SelectedNode, Is.SameAs(selected));
            Assert.That(treeView.HotTracking, Is.EqualTo(hotTracking));
        }));
    }

    [Test]
    public void MouseLeave_ClearsFrameworkHotPresentationAndInvalidatesPreviousRow()
    {
        using var treeView = CreateProbeTree();
        var node = treeView.Nodes[0];
        treeView.RaiseMouseMove(GetLabelPoint(node));
        treeView.ResetInvalidations();

        treeView.RaiseMouseLeave();
        using var bitmap = RenderNode(treeView, node);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsColor(bitmap, GetRowBounds(treeView, node), BootstrapThemeManager.CurrentTheme.Colors.Hover), Is.False);
            Assert.That(treeView.InvalidatedRects.Exists(rect => rect.IntersectsWith(GetRowBounds(treeView, node))), Is.True);
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FocusEnterLeave_InvalidatesOnlySelectedRowRegardlessOfHideSelection(bool hideSelection)
    {
        using var treeView = CreateProbeTree();
        var selected = treeView.Nodes[1];
        treeView.SelectedNode = selected;
        treeView.HideSelection = hideSelection;
        var expected = GetRowBounds(treeView, selected);

        treeView.ResetInvalidations();
        treeView.RaiseGotFocus();
        var gotFocusRects = treeView.InvalidatedRects.ToArray();

        treeView.ResetInvalidations();
        treeView.RaiseLostFocus();
        var lostFocusRects = treeView.InvalidatedRects.ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Array.Exists(gotFocusRects, rect => rect.IntersectsWith(expected)), Is.True);
            Assert.That(Array.Exists(lostFocusRects, rect => rect.IntersectsWith(expected)), Is.True);
            Assert.That(Array.TrueForAll(gotFocusRects, rect => rect.IsEmpty || rect.IntersectsWith(expected)), Is.True);
            Assert.That(Array.TrueForAll(lostFocusRects, rect => rect.IsEmpty || rect.IntersectsWith(expected)), Is.True);
        }));
    }

    [Test]
    public void ItemDragEvent_RemainsSingleAfterFrameworkHoverAndSelection()
    {
        using var treeView = CreateProbeTree();
        var selected = treeView.Nodes[0];
        treeView.SelectedNode = selected;
        treeView.RaiseMouseMove(GetLabelPoint(treeView.Nodes[1]));
        var count = 0;
        TreeNode? draggedNode = null;
        treeView.ItemDrag += (_, e) =>
        {
            count++;
            draggedNode = e.Item as TreeNode;
        };

        treeView.RaiseItemDrag(selected);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(draggedNode, Is.SameAs(selected));
            Assert.That(treeView.SelectedNode, Is.SameAs(selected));
        }));
    }

    private static ProbeBootstrapTreeView CreateProbeTree()
    {
        var treeView = new ProbeBootstrapTreeView
        {
            Size = new Size(320, 160),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            HotTracking = false,
        };
        treeView.Nodes.Add(new TreeNode("First node"));
        treeView.Nodes.Add(new TreeNode("Second node"));
        _ = treeView.Handle;
        return treeView;
    }

    private static Point GetLabelPoint(TreeNode node)
    {
        return new Point(node.Bounds.Left + 2, node.Bounds.Top + Math.Max(1, node.Bounds.Height / 2));
    }

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node)
    {
        var rowBounds = GetRowBounds(treeView, node);
        var bitmap = new Bitmap(treeView.ClientSize.Width, treeView.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, node.Bounds, (TreeNodeStates)0);
        return bitmap;
    }

    private static Rectangle GetRowBounds(TreeView treeView, TreeNode node)
    {
        return Rectangle.Intersect(
            treeView.ClientRectangle,
            new Rectangle(treeView.ClientRectangle.Left, node.Bounds.Top, treeView.ClientRectangle.Width, treeView.ItemHeight));
    }

    private static bool ContainsColor(Bitmap bitmap, Rectangle bounds, Color expected)
    {
        for (var y = Math.Max(0, bounds.Top); y < Math.Min(bitmap.Height, bounds.Bottom); y++)
        {
            for (var x = Math.Max(0, bounds.Left); x < Math.Min(bitmap.Width, bounds.Right); x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (Math.Abs(pixel.R - expected.R) <= 4 &&
                    Math.Abs(pixel.G - expected.G) <= 4 &&
                    Math.Abs(pixel.B - expected.B) <= 4)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private sealed class ProbeBootstrapTreeView : BootstrapTreeView
    {
        internal List<Rectangle> InvalidatedRects { get; } = new List<Rectangle>();

        internal void RaiseMouseMove(Point point)
        {
            base.OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
        }

        internal void RaiseMouseLeave()
        {
            base.OnMouseLeave(EventArgs.Empty);
        }

        internal void RaiseGotFocus()
        {
            base.OnGotFocus(EventArgs.Empty);
        }

        internal void RaiseLostFocus()
        {
            base.OnLostFocus(EventArgs.Empty);
        }

        internal void RaiseItemDrag(TreeNode node)
        {
            base.OnItemDrag(new ItemDragEventArgs(MouseButtons.Left, node));
        }

        internal void ResetInvalidations()
        {
            InvalidatedRects.Clear();
        }

        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            InvalidatedRects.Add(e.InvalidRect);
            base.OnInvalidated(e);
        }
    }
}
