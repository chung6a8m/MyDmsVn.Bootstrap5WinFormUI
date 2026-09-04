using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTreeViewImageResolutionTests
{
    [Test]
    public void OwnerDraw_NodeWithoutOverrideUsesTreeViewFallbackImageIndex()
    {
        using var images = CreateImageList(
            new Size(16, 16),
            Color.Red,
            Color.Lime);
        using var treeView = CreateTree(images);
        treeView.ImageKey = string.Empty;
        treeView.ImageIndex = 1;
        var node = new TreeNode("Fallback")
        {
            ImageKey = string.Empty,
            ImageIndex = -1,
        };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        using var bitmap = RenderNode(treeView, node, (TreeNodeStates)0);

        Assert.That(
            ContainsColor(
                bitmap,
                GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.Image),
                Color.Lime),
            Is.True);
    }

    [Test]
    public void OwnerDraw_SelectedImageIndexUsesNodeOverrideAndTreeViewFallback()
    {
        using var images = CreateImageList(
            new Size(16, 16),
            Color.Red,
            Color.Lime,
            Color.Blue);
        using var treeView = CreateTree(images);
        treeView.SelectedImageKey = string.Empty;
        treeView.SelectedImageIndex = 2;

        var nodeOverride = new TreeNode("Node override")
        {
            ImageIndex = 0,
            SelectedImageKey = string.Empty,
            SelectedImageIndex = 1,
        };
        var treeFallback = new TreeNode("Tree fallback")
        {
            ImageIndex = 0,
            SelectedImageKey = string.Empty,
            SelectedImageIndex = -1,
        };
        treeView.Nodes.Add(nodeOverride);
        treeView.Nodes.Add(treeFallback);
        _ = treeView.Handle;

        using var overrideBitmap = RenderNode(treeView, nodeOverride, TreeNodeStates.Selected);
        using var fallbackBitmap = RenderNode(treeView, treeFallback, TreeNodeStates.Selected);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                ContainsColor(
                    overrideBitmap,
                    GetNativeHitBounds(treeView, nodeOverride, TreeViewHitTestLocations.Image),
                    Color.Lime),
                Is.True);
            Assert.That(
                ContainsColor(
                    fallbackBitmap,
                    GetNativeHitBounds(treeView, treeFallback, TreeViewHitTestLocations.Image),
                    Color.Blue),
                Is.True);
        }));
    }

    private static BootstrapTreeView CreateTree(ImageList images)
    {
        return new BootstrapTreeView
        {
            Size = new Size(320, 120),
            ItemHeight = 24,
            Indent = 19,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            ImageList = images,
        };
    }

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node, TreeNodeStates state)
    {
        var labelBounds = node.Bounds;
        Assert.That(labelBounds.IsEmpty, Is.False);
        var rowBounds = new Rectangle(0, labelBounds.Top, treeView.ClientSize.Width, treeView.ItemHeight);
        var bitmap = new Bitmap(treeView.ClientSize.Width, treeView.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, state);
        return bitmap;
    }

    private static Rectangle GetNativeHitBounds(
        TreeView treeView,
        TreeNode expectedNode,
        TreeViewHitTestLocations expectedLocation)
    {
        var bounds = expectedNode.Bounds;
        var y = bounds.Top + (bounds.Height / 2);
        var first = -1;
        var last = -1;
        for (var x = treeView.ClientRectangle.Left; x < treeView.ClientRectangle.Right; x++)
        {
            var hit = treeView.HitTest(x, y);
            if (hit.Node != expectedNode || (hit.Location & expectedLocation) != expectedLocation)
            {
                continue;
            }

            if (first < 0)
            {
                first = x;
            }

            last = x;
        }

        Assert.That(first, Is.GreaterThanOrEqualTo(0), $"Expected native {expectedLocation} hit geometry.");
        return Rectangle.FromLTRB(first, bounds.Top, last + 1, bounds.Bottom);
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
}
