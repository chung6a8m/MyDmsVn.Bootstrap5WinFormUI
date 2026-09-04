using System;
using System.Collections.Generic;
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
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRegressionTests
{
    [Test]
    public void DisabledFrameworkCheckbox_PreservesCheckedStatePresentation()
    {
        using var treeView = CreateTree(checkBoxes: true);
        var uncheckedNode = new TreeNode("Unchecked") { Checked = false };
        var checkedNode = new TreeNode("Checked") { Checked = true };
        treeView.Nodes.Add(uncheckedNode);
        treeView.Nodes.Add(checkedNode);
        _ = treeView.Handle;
        treeView.Enabled = false;

        using var uncheckedBitmap = RenderNode(treeView, uncheckedNode);
        using var checkedBitmap = RenderNode(treeView, checkedNode);
        var uncheckedBounds = GetNativeHitBounds(treeView, uncheckedNode, TreeViewHitTestLocations.StateImage);
        var checkedBounds = GetNativeHitBounds(treeView, checkedNode, TreeViewHitTestLocations.StateImage);

        Assert.That(
            ContainsDifferentPixels(uncheckedBitmap, uncheckedBounds, checkedBitmap, checkedBounds),
            Is.True,
            "A disabled checked node must remain visually distinguishable from a disabled unchecked node.");
    }

    [Test]
    public void RightToLeftWithoutRightToLeftLayout_KeepsCollapsedExpanderPointingTowardNativeContent()
    {
        using var treeView = CreateTree();
        treeView.RightToLeft = RightToLeft.Yes;
        treeView.RightToLeftLayout = false;
        treeView.ShowPlusMinus = true;
        treeView.ShowRootLines = true;

        var root = new TreeNode("RTL reading only");
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        _ = treeView.Handle;
        root.Collapse();

        var layout = CalculateLayout(treeView, root, hasExpander: true, hasStateImage: false, nativeStateImageSlotWidth: 0);
        var expectedGlyph = BootstrapTreeViewLayout.CalculateExpanderGlyph(
            layout.ExpanderBounds,
            expanded: false,
            rightToLeft: false);
        var incorrectlyMirroredGlyph = BootstrapTreeViewLayout.CalculateExpanderGlyph(
            layout.ExpanderBounds,
            expanded: false,
            rightToLeft: true);

        using var bitmap = RenderNode(treeView, root);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                IsPaintedAt(bitmap, expectedGlyph.Tip, Color.Magenta),
                Is.True,
                "RightToLeft changes text reading direction, but native structural mirroring requires RightToLeftLayout=true.");
            Assert.That(
                IsPaintedAt(bitmap, incorrectlyMirroredGlyph.Tip, Color.Magenta),
                Is.False,
                "The collapsed expander must not point left when only RTL text reading is enabled.");
        }));
    }

    [Test]
    public void CustomStateImage_UsesNativeStateImageHitWidthInsteadOfFrameworkCheckboxSize()
    {
        using var stateImages = CreateImageList(new Size(32, 32), Color.Lime);
        using var treeView = CreateTree(stateImageList: stateImages);
        var node = new TreeNode("Custom state") { StateImageIndex = 0 };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var nativeHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
        using var bitmap = RenderNode(treeView, node);
        var paintedWidth = GetHorizontalColorSpan(bitmap, nativeHit, Color.Lime);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeHit.Width, Is.GreaterThan(DpiScaler.Scale(13, treeView.DeviceDpi)));
            Assert.That(
                paintedWidth,
                Is.EqualTo(nativeHit.Width),
                "Caller-supplied state images should fill the native state-image geometry rather than the 13px framework checkbox glyph size.");
        }));
    }

    private static BootstrapTreeView CreateTree(
        bool checkBoxes = false,
        ImageList? stateImageList = null)
    {
        return new BootstrapTreeView
        {
            Size = new Size(320, 160),
            ItemHeight = 24,
            Indent = 19,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            CheckBoxes = checkBoxes,
            StateImageList = stateImageList,
        };
    }

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node)
    {
        var labelBounds = node.Bounds;
        Assert.That(labelBounds.IsEmpty, Is.False);
        var rowBounds = new Rectangle(0, labelBounds.Top, treeView.ClientSize.Width, treeView.ItemHeight);
        var bitmap = new Bitmap(treeView.ClientSize.Width, treeView.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, (TreeNodeStates)0);
        return bitmap;
    }

    private static BootstrapTreeViewNodeLayout CalculateLayout(
        BootstrapTreeView treeView,
        TreeNode node,
        bool hasExpander,
        bool hasStateImage,
        int nativeStateImageSlotWidth)
    {
        var labelBounds = node.Bounds;
        var rowBounds = new Rectangle(0, labelBounds.Top, treeView.ClientSize.Width, treeView.ItemHeight);
        return BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            treeView.ClientRectangle,
            rowBounds,
            labelBounds,
            node.Level,
            treeView.DeviceDpi,
            rightToLeft: false,
            effectiveFullRowSelection: treeView.FullRowSelect && !treeView.ShowLines,
            hasExpander,
            hasStateImage,
            nativeStateImageSlotWidth,
            hasNodeImage: false,
            nodeImageSize: Size.Empty));
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

    private static bool IsPaintedAt(Bitmap bitmap, Point point, Color background)
    {
        if (point.X < 0 || point.X >= bitmap.Width || point.Y < 0 || point.Y >= bitmap.Height)
        {
            return false;
        }

        return bitmap.GetPixel(point.X, point.Y).ToArgb() != background.ToArgb();
    }

    private static int GetHorizontalColorSpan(Bitmap bitmap, Rectangle bounds, Color expected)
    {
        var first = -1;
        var last = -1;
        for (var y = Math.Max(0, bounds.Top); y < Math.Min(bitmap.Height, bounds.Bottom); y++)
        {
            for (var x = Math.Max(0, bounds.Left); x < Math.Min(bitmap.Width, bounds.Right); x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (Math.Abs(pixel.R - expected.R) > 4 ||
                    Math.Abs(pixel.G - expected.G) > 4 ||
                    Math.Abs(pixel.B - expected.B) > 4)
                {
                    continue;
                }

                if (first < 0 || x < first)
                {
                    first = x;
                }

                if (x > last)
                {
                    last = x;
                }
            }
        }

        return first < 0 ? 0 : last - first + 1;
    }

    private static bool ContainsDifferentPixels(
        Bitmap first,
        Rectangle firstBounds,
        Bitmap second,
        Rectangle secondBounds)
    {
        var width = Math.Min(firstBounds.Width, secondBounds.Width);
        var height = Math.Min(firstBounds.Height, secondBounds.Height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (first.GetPixel(firstBounds.Left + x, firstBounds.Top + y).ToArgb() !=
                    second.GetPixel(secondBounds.Left + x, secondBounds.Top + y).ToArgb())
                {
                    return true;
                }
            }
        }

        return false;
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
