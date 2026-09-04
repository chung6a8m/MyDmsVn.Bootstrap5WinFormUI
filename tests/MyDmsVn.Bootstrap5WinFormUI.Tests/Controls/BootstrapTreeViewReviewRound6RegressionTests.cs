using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRound6RegressionTests
{
    [Test]
    public void OwnerDraw_PartiallyClippedNodeImageKeepsUnclippedNativeImageHeight()
    {
        using var images = CreateSolidImageList(new Size(16, 16), Color.Lime);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(80, 48),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            ImageList = images,
        };
        var node = new TreeNode("Image") { ImageIndex = 0 };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var actualLabel = node.Bounds;
        var shiftedLabel = new Rectangle(10, actualLabel.Top, Math.Max(40, actualLabel.Width), actualLabel.Height);
        var rowBounds = new Rectangle(0, actualLabel.Top, treeView.ClientSize.Width, treeView.ItemHeight);
        using var bitmap = RenderNode(treeView, node, rowBounds, shiftedLabel);
        var visibleImageBand = new Rectangle(0, rowBounds.Top, 7, rowBounds.Height);
        var painted = GetPaintedBounds(bitmap, visibleImageBand);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(painted.IsEmpty, Is.False, "Expected the partially visible node image to be painted.");
            Assert.That(
                painted.Height,
                Is.EqualTo(images.ImageSize.Height),
                "Horizontal viewport clipping must reveal a slice of the original native-sized image, not rescale the image into the clipped width.");
        }));
    }

    [Test]
    public void OwnerDraw_PartiallyClippedStateImageShowsSourceSliceInsteadOfStretchingWholeImage()
    {
        using var stateImages = CreateSplitImageList(new Size(16, 16), Color.Red, Color.Lime);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(80, 48),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            StateImageList = stateImages,
        };
        var node = new TreeNode("State") { StateImageIndex = 0 };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var actualLabel = node.Bounds;
        var shiftedLabel = new Rectangle(6, actualLabel.Top, Math.Max(40, actualLabel.Width), actualLabel.Height);
        var rowBounds = new Rectangle(0, actualLabel.Top, treeView.ClientSize.Width, treeView.ItemHeight);
        using var bitmap = RenderNode(treeView, node, rowBounds, shiftedLabel);
        var sample = bitmap.GetPixel(0, rowBounds.Top + (rowBounds.Height / 2));

        Assert.That(
            IsColorNear(sample, Color.Lime),
            Is.True,
            "A custom state image crossing the left viewport edge must retain its full native destination geometry so the visible pixels are the right-hand source slice; stretching the whole source into the clipped rectangle incorrectly exposes the red left half.");
    }

    [Test]
    public void Layout_PartiallyClippedExpanderKeepsFullLogicalGlyphBounds()
    {
        var clientBounds = new Rectangle(0, 0, 80, 24);
        var layout = BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            clientBounds,
            clientBounds,
            new Rectangle(12, 0, 60, 24),
            nodeLevel: 0,
            dpi: 96,
            rightToLeft: false,
            effectiveFullRowSelection: false,
            hasExpander: true,
            hasStateImage: false,
            nativeStateImageSlotWidth: 0,
            hasNodeImage: false,
            nodeImageSize: Size.Empty));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.ExpanderBounds.Width, Is.EqualTo(9));
            Assert.That(layout.ExpanderBounds.Height, Is.EqualTo(9));
            Assert.That(layout.ExpanderBounds.Left, Is.LessThan(0));
            Assert.That(Rectangle.Intersect(layout.ExpanderBounds, clientBounds).IsEmpty, Is.False);
        }));
    }

    private static Bitmap RenderNode(
        BootstrapTreeView treeView,
        TreeNode node,
        Rectangle rowBounds,
        Rectangle nativeLabelBounds)
    {
        var bitmap = new Bitmap(treeView.ClientSize.Width, treeView.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, nativeLabelBounds, (TreeNodeStates)0);
        return bitmap;
    }

    private static Rectangle GetPaintedBounds(Bitmap bitmap, Rectangle bounds)
    {
        var clipped = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), bounds);
        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;
        for (var y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (var x = clipped.Left; x < clipped.Right; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == Color.Magenta.ToArgb())
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return left == int.MaxValue
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static bool IsColorNear(Color actual, Color expected)
    {
        return Math.Abs(actual.R - expected.R) <= 16 &&
               Math.Abs(actual.G - expected.G) <= 16 &&
               Math.Abs(actual.B - expected.B) <= 16;
    }

    private static ImageList CreateSolidImageList(Size imageSize, Color color)
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

    private static ImageList CreateSplitImageList(Size imageSize, Color leftColor, Color rightColor)
    {
        var imageList = new ImageList
        {
            ImageSize = imageSize,
            ColorDepth = ColorDepth.Depth32Bit,
        };
        var sourceImages = new List<Bitmap>();
        var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(leftColor);
            using var brush = new SolidBrush(rightColor);
            graphics.FillRectangle(
                brush,
                imageSize.Width / 2,
                0,
                imageSize.Width - (imageSize.Width / 2),
                imageSize.Height);
        }

        sourceImages.Add(bitmap);
        imageList.Images.Add(bitmap);
        _ = imageList.Handle;
        foreach (var source in sourceImages)
        {
            source.Dispose();
        }

        return imageList;
    }
}
