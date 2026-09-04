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
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRound5RegressionTests
{
    [TestCase(8, 8)]
    [TestCase(32, 8)]
    public void OwnerDraw_CustomStateImageFillsNativeNormalizedDisplaySlot(int sourceWidth, int sourceHeight)
    {
        using var stateImages = CreateImageList(new Size(sourceWidth, sourceHeight), Color.Lime);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(320, 120),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            StateImageList = stateImages,
        };
        var node = new TreeNode("State image") { StateImageIndex = 0 };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var nativeStateHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
        var expectedDisplayBounds = GetExpectedNativeStateDisplayBounds(treeView, node, nativeStateHit.Width);
        using var bitmap = RenderNode(treeView, node);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeStateHit.Width, Is.GreaterThan(0));
            Assert.That(expectedDisplayBounds.Width, Is.EqualTo(nativeStateHit.Width));
            Assert.That(IsColorNear(bitmap.GetPixel(expectedDisplayBounds.Left, expectedDisplayBounds.Top + (expectedDisplayBounds.Height / 2)), Color.Lime), Is.True,
                "The rendered state image should fill the native display slot horizontally.");
            Assert.That(IsColorNear(bitmap.GetPixel(expectedDisplayBounds.Right - 1, expectedDisplayBounds.Top + (expectedDisplayBounds.Height / 2)), Color.Lime), Is.True,
                "The rendered state image should fill the native display slot horizontally.");
            Assert.That(IsColorNear(bitmap.GetPixel(expectedDisplayBounds.Left + (expectedDisplayBounds.Width / 2), expectedDisplayBounds.Top), Color.Lime), Is.True,
                "The rendered state image should fill the native normalized square vertically instead of preserving caller aspect ratio.");
            Assert.That(IsColorNear(bitmap.GetPixel(expectedDisplayBounds.Left + (expectedDisplayBounds.Width / 2), expectedDisplayBounds.Bottom - 1), Color.Lime), Is.True,
                "The rendered state image should fill the native normalized square vertically instead of preserving caller aspect ratio.");
        }));
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    public void CheckBoxes_MissingRequiredCustomStateImageFallsBackToFrameworkCheckbox(int customImageCount, bool isChecked)
    {
        using var stateImages = new ImageList
        {
            ImageSize = new Size(16, 16),
            ColorDepth = ColorDepth.Depth32Bit,
        };
        var sourceImages = new List<Bitmap>();
        for (var index = 0; index < customImageCount; index++)
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
            }

            sourceImages.Add(bitmap);
            stateImages.Images.Add(bitmap);
        }

        _ = stateImages.Handle;
        foreach (var bitmap in sourceImages)
        {
            bitmap.Dispose();
        }

        using var treeView = new BootstrapTreeView
        {
            Size = new Size(320, 120),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            CheckBoxes = true,
            StateImageList = stateImages,
        };
        var node = new TreeNode("Checkbox") { Checked = isChecked };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var nativeStateHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
        using var bitmap = RenderNode(treeView, node);

        Assert.That(
            ContainsPaint(bitmap, nativeStateHit),
            Is.True,
            "When the custom StateImageList does not contain the required checkbox image, BootstrapTreeView must render framework checkbox art instead of leaving the native state slot blank.");
    }

    private static Rectangle GetExpectedNativeStateDisplayBounds(TreeView treeView, TreeNode node, int stateSlotWidth)
    {
        var size = Math.Min(stateSlotWidth, treeView.ItemHeight);
        return new Rectangle(
            GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage).Left,
            node.Bounds.Top + ((treeView.ItemHeight - size) / 2),
            size,
            size);
    }

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node)
    {
        var labelBounds = node.Bounds;
        Assert.That(labelBounds.IsEmpty, Is.False);
        var rowBounds = new Rectangle(
            treeView.ClientRectangle.Left,
            labelBounds.Top,
            treeView.ClientRectangle.Width,
            treeView.ItemHeight);
        var bitmap = new Bitmap(Math.Max(1, treeView.ClientSize.Width), Math.Max(1, treeView.ClientSize.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, (TreeNodeStates)0);
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

    private static bool ContainsPaint(Bitmap bitmap, Rectangle bounds)
    {
        var clipped = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), bounds);
        for (var y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (var x = clipped.Left; x < clipped.Right; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != Color.Magenta.ToArgb())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsColorNear(Color actual, Color expected)
    {
        return Math.Abs(actual.R - expected.R) <= 8 &&
               Math.Abs(actual.G - expected.G) <= 8 &&
               Math.Abs(actual.B - expected.B) <= 8;
    }

    private static ImageList CreateImageList(Size imageSize, Color color)
    {
        var imageList = new ImageList
        {
            ImageSize = imageSize,
            ColorDepth = ColorDepth.Depth32Bit,
        };
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(color);
        }

        imageList.Images.Add(bitmap);
        _ = imageList.Handle;
        return imageList;
    }
}
