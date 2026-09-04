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
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRound4RegressionTests
{
    [Test]
    public void Constructor_PreservesNativeBorderStyleDefault()
    {
        using var native = new TreeView();
        using var treeView = new BootstrapTreeView();

        Assert.That(
            treeView.BorderStyle,
            Is.EqualTo(native.BorderStyle),
            "BootstrapTreeView must preserve the inherited TreeView BorderStyle default.");
    }

    [Test]
    public void HotTracking_OnlyHighlightsNativeOnItemHitRegions()
    {
        using var treeView = new ProbeBootstrapTreeView
        {
            Size = new Size(320, 140),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = true,
            ShowRootLines = true,
            HotTracking = true,
        };
        var node = new TreeNode("Hot tracking node");
        node.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(node);
        _ = treeView.Handle;
        node.Collapse();

        var plusMinusPoint = GetHitPoint(treeView, node, TreeViewHitTestLocations.PlusMinus);
        var labelPoint = GetHitPoint(treeView, node, TreeViewHitTestLocations.Label);
        var hoverColor = BootstrapThemeManager.CurrentTheme.Colors.Hover;
        var rowBounds = GetRowBounds(treeView, node);

        treeView.RaiseMouseLeave();
        treeView.RaiseMouseMove(plusMinusPoint);
        using var plusMinusBitmap = RenderNode(treeView, node);

        treeView.RaiseMouseMove(labelPoint);
        using var labelBitmap = RenderNode(treeView, node);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                ContainsColor(plusMinusBitmap, rowBounds, hoverColor),
                Is.False,
                "The native PlusMinus hit region is not part of TVHT_ONITEM and must not receive hot presentation.");
            Assert.That(
                ContainsColor(labelBitmap, rowBounds, hoverColor),
                Is.True,
                "The native Label hit region is part of TVHT_ONITEM and should receive hot presentation.");
        }));
    }

    [Test]
    public void OwnerDraw_ExplicitNoImageDoesNotFallBackToTreeViewImages()
    {
        using var images = CreateImageList(new Size(16, 16), Color.Red);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(320, 120),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            ImageList = images,
            ImageIndex = 0,
            SelectedImageIndex = 0,
        };
        var node = new TreeNode("No image")
        {
            ImageIndex = -2,
            SelectedImageIndex = -2,
        };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        using var normalBitmap = RenderNode(treeView, node, (TreeNodeStates)0);
        using var selectedBitmap = RenderNode(treeView, node, TreeNodeStates.Selected);
        var rowBounds = GetRowBounds(treeView, node);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                ContainsColor(normalBitmap, rowBounds, Color.Red),
                Is.False,
                "TreeNode.ImageIndex = -2 explicitly means no image and must not fall back to TreeView.ImageIndex.");
            Assert.That(
                ContainsColor(selectedBitmap, rowBounds, Color.Red),
                Is.False,
                "TreeNode.SelectedImageIndex = -2 explicitly means no selected image and must not fall back to TreeView.SelectedImageIndex or normal image.");
        }));
    }

    private static Point GetHitPoint(
        TreeView treeView,
        TreeNode expectedNode,
        TreeViewHitTestLocations expectedLocation)
    {
        var bounds = expectedNode.Bounds;
        var y = bounds.Top + Math.Max(1, bounds.Height / 2);
        for (var x = treeView.ClientRectangle.Left; x < treeView.ClientRectangle.Right; x++)
        {
            var hit = treeView.HitTest(x, y);
            if (hit.Node == expectedNode && (hit.Location & expectedLocation) == expectedLocation)
            {
                return new Point(x, y);
            }
        }

        Assert.Fail($"Expected native {expectedLocation} hit geometry.");
        return Point.Empty;
    }

    private static Rectangle GetRowBounds(TreeView treeView, TreeNode node)
    {
        return Rectangle.Intersect(
            treeView.ClientRectangle,
            new Rectangle(treeView.ClientRectangle.Left, node.Bounds.Top, treeView.ClientRectangle.Width, treeView.ItemHeight));
    }

    private static Bitmap RenderNode(
        BootstrapTreeView treeView,
        TreeNode node,
        TreeNodeStates state = (TreeNodeStates)0)
    {
        var labelBounds = node.Bounds;
        Assert.That(labelBounds.IsEmpty, Is.False);
        var rowBounds = GetRowBounds(treeView, node);
        var bitmap = new Bitmap(Math.Max(1, treeView.ClientSize.Width), Math.Max(1, treeView.ClientSize.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, state);
        return bitmap;
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

    private static ImageList CreateImageList(Size imageSize, params Color[] colors)
    {
        var imageList = new ImageList
        {
            ImageSize = imageSize,
            ColorDepth = ColorDepth.Depth32Bit,
        };
        var sourceImages = new List<Bitmap>();
        foreach (var color in colors)
        {
            var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(color);
            }

            sourceImages.Add(bitmap);
            imageList.Images.Add(bitmap);
        }

        _ = imageList.Handle;
        foreach (var bitmap in sourceImages)
        {
            bitmap.Dispose();
        }

        return imageList;
    }

    private sealed class ProbeBootstrapTreeView : BootstrapTreeView
    {
        internal void RaiseMouseMove(Point point)
        {
            base.OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
        }

        internal void RaiseMouseLeave()
        {
            base.OnMouseLeave(EventArgs.Empty);
        }
    }
}
