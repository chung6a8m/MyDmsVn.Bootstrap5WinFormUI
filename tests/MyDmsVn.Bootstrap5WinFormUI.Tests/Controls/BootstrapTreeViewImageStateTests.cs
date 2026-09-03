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
public sealed class BootstrapTreeViewImageStateTests
{
    [Test]
    public void OwnerDraw_NodeImageKeyAndIndexOverrideTreeViewFallbackImage()
    {
        using var images = CreateImageList(new Size(16, 16),
            ("fallback", Color.Red),
            ("node", Color.Lime));
        using var treeView = CreateTree(imageList: images);
        treeView.ImageKey = "fallback";
        var keyed = new TreeNode("Keyed") { ImageKey = "node" };
        var indexed = new TreeNode("Indexed") { ImageIndex = 1 };
        treeView.Nodes.Add(keyed);
        treeView.Nodes.Add(indexed);
        _ = treeView.Handle;

        using var keyedBitmap = RenderNode(treeView, keyed, (TreeNodeStates)0);
        using var indexedBitmap = RenderNode(treeView, indexed, (TreeNodeStates)0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsColor(keyedBitmap, GetNativeHitBounds(treeView, keyed, TreeViewHitTestLocations.Image), Color.Lime), Is.True);
            Assert.That(ContainsColor(indexedBitmap, GetNativeHitBounds(treeView, indexed, TreeViewHitTestLocations.Image), Color.Lime), Is.True);
        }));
    }

    [Test]
    public void OwnerDraw_NodeWithoutImageOverrideUsesTreeViewFallbackImage()
    {
        using var images = CreateImageList(new Size(16, 16),
            ("fallback", Color.Red),
            ("other", Color.Lime));
        using var treeView = CreateTree(imageList: images);
        treeView.ImageKey = "fallback";
        var node = new TreeNode("Fallback") { ImageIndex = -1 };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        using var bitmap = RenderNode(treeView, node, (TreeNodeStates)0);

        Assert.That(
            ContainsColor(bitmap, GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.Image), Color.Red),
            Is.True);
    }

    [Test]
    public void OwnerDraw_SelectedImageKeyOverridesNormalImage()
    {
        using var images = CreateImageList(new Size(16, 16),
            ("normal", Color.Red),
            ("selected", Color.Lime));
        using var treeView = CreateTree(imageList: images);
        var node = new TreeNode("Selected")
        {
            ImageKey = "normal",
            SelectedImageKey = "selected",
        };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        using var bitmap = RenderNode(treeView, node, TreeNodeStates.Selected);

        Assert.That(
            ContainsColor(bitmap, GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.Image), Color.Lime),
            Is.True);
    }

    [Test]
    public void TreeViewDisposal_DoesNotDisposeCallerOwnedImageList()
    {
        using var images = CreateImageList(new Size(16, 16), ("normal", Color.Red));
        var treeView = CreateTree(imageList: images);
        treeView.Nodes.Add(new TreeNode("Node"));
        _ = treeView.Handle;

        treeView.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(images.Images.Count, Is.EqualTo(1));
            Assert.That(images.Images[0].Width, Is.EqualTo(16));
        }));
    }

    [Test]
    public void OwnerDraw_StateImageKeyAndIndexAreUsedWhenCheckBoxesAreDisabled()
    {
        using var stateImages = CreateImageList(new Size(16, 16),
            ("first", Color.Red),
            ("second", Color.Lime));
        using var treeView = CreateTree(stateImageList: stateImages);
        var keyed = new TreeNode("Keyed") { StateImageKey = "second" };
        var indexed = new TreeNode("Indexed") { StateImageIndex = 1 };
        treeView.Nodes.Add(keyed);
        treeView.Nodes.Add(indexed);
        _ = treeView.Handle;

        using var keyedBitmap = RenderNode(treeView, keyed, (TreeNodeStates)0);
        using var indexedBitmap = RenderNode(treeView, indexed, (TreeNodeStates)0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsColor(keyedBitmap, GetNativeHitBounds(treeView, keyed, TreeViewHitTestLocations.StateImage), Color.Lime), Is.True);
            Assert.That(ContainsColor(indexedBitmap, GetNativeHitBounds(treeView, indexed, TreeViewHitTestLocations.StateImage), Color.Lime), Is.True);
        }));
    }

    [Test]
    public void OwnerDraw_CheckBoxesWithoutStateImageListDrawFrameworkCheckboxFromCheckedState()
    {
        using var treeView = CreateTree(checkBoxes: true);
        var uncheckedNode = new TreeNode("Unchecked") { Checked = false };
        var checkedNode = new TreeNode("Checked") { Checked = true };
        treeView.Nodes.Add(uncheckedNode);
        treeView.Nodes.Add(checkedNode);
        _ = treeView.Handle;

        using var uncheckedBitmap = RenderNode(treeView, uncheckedNode, (TreeNodeStates)0);
        using var checkedBitmap = RenderNode(treeView, checkedNode, (TreeNodeStates)0);
        var uncheckedHit = GetNativeHitBounds(treeView, uncheckedNode, TreeViewHitTestLocations.StateImage);
        var checkedHit = GetNativeHitBounds(treeView, checkedNode, TreeViewHitTestLocations.StateImage);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsPaint(uncheckedBitmap, uncheckedHit), Is.True);
            Assert.That(ContainsPaint(checkedBitmap, checkedHit), Is.True);
            Assert.That(ContainsDifferentPixels(uncheckedBitmap, uncheckedHit, checkedBitmap, checkedHit), Is.True);
        }));
    }

    [TestCase(false, 0, 255, 0, 0)]
    [TestCase(true, 1, 0, 255, 0)]
    public void OwnerDraw_CheckBoxesWithStateImageListUseNativeUncheckedCheckedIndices(
        bool isChecked,
        int expectedIndex,
        int expectedR,
        int expectedG,
        int expectedB)
    {
        using var stateImages = CreateImageList(new Size(16, 16),
            ("unchecked", Color.Red),
            ("checked", Color.Lime));
        using var treeView = CreateTree(checkBoxes: true, stateImageList: stateImages);
        var node = new TreeNode("State") { Checked = isChecked };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        using var bitmap = RenderNode(treeView, node, (TreeNodeStates)0);
        var expected = Color.FromArgb(expectedR, expectedG, expectedB);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(expectedIndex, Is.EqualTo(isChecked ? 1 : 0));
            Assert.That(ContainsColor(bitmap, GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage), expected), Is.True);
        }));
    }

    [Test]
    public void NonDefaultStateImageListSize_DoesNotBecomeFrameworkStateSlotSize()
    {
        using var stateImages = CreateImageList(new Size(32, 24),
            ("unchecked", Color.Red),
            ("checked", Color.Lime));
        using var treeView = CreateTree(checkBoxes: true, stateImageList: stateImages);
        var node = new TreeNode("State") { Checked = true };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var nativeStateHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
        using var bitmap = RenderNode(treeView, node, (TreeNodeStates)0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeStateHit.Width, Is.Not.EqualTo(stateImages.ImageSize.Width));
            Assert.That(ContainsColor(bitmap, nativeStateHit, Color.Lime), Is.True);
        }));
    }

    [Test]
    public void TreeViewDisposal_DoesNotDisposeCallerOwnedStateImageList()
    {
        using var stateImages = CreateImageList(new Size(16, 16),
            ("unchecked", Color.Red),
            ("checked", Color.Lime));
        var treeView = CreateTree(checkBoxes: true, stateImageList: stateImages);
        treeView.Nodes.Add(new TreeNode("Node"));
        _ = treeView.Handle;

        treeView.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(stateImages.Images.Count, Is.EqualTo(2));
            Assert.That(stateImages.Images[1].Width, Is.EqualTo(16));
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OwnerDraw_ImageAndStatePixelsOverlapNativeHitRegionsInLtrAndRtl(bool rightToLeft)
    {
        using var images = CreateImageList(new Size(16, 16), ("node", Color.Blue));
        using var stateImages = CreateImageList(new Size(16, 16), ("state", Color.Lime));
        using var treeView = CreateTree(imageList: images, stateImageList: stateImages);
        treeView.RightToLeft = rightToLeft ? RightToLeft.Yes : RightToLeft.No;
        treeView.RightToLeftLayout = rightToLeft;
        var node = new TreeNode("Parity")
        {
            ImageKey = "node",
            StateImageKey = "state",
        };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var imageHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.Image);
        var stateHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
        using var bitmap = RenderNode(treeView, node, (TreeNodeStates)0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsColor(bitmap, imageHit, Color.Blue), Is.True);
            Assert.That(ContainsColor(bitmap, stateHit, Color.Lime), Is.True);
        }));
    }

    private static BootstrapTreeView CreateTree(
        bool checkBoxes = false,
        ImageList? imageList = null,
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
            ImageList = imageList,
            StateImageList = stateImageList,
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

    private static bool ContainsPaint(Bitmap bitmap, Rectangle bounds)
    {
        for (var y = Math.Max(0, bounds.Top); y < Math.Min(bitmap.Height, bounds.Bottom); y++)
        {
            for (var x = Math.Max(0, bounds.Left); x < Math.Min(bitmap.Width, bounds.Right); x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != Color.Magenta.ToArgb())
                {
                    return true;
                }
            }
        }

        return false;
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

    private static ImageList CreateImageList(Size imageSize, params (string Key, Color Color)[] values)
    {
        var imageList = new ImageList
        {
            ImageSize = imageSize,
            ColorDepth = ColorDepth.Depth32Bit,
        };
        var sourceImages = new List<Bitmap>();
        foreach (var value in values)
        {
            var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(value.Color);
            }

            sourceImages.Add(bitmap);
            imageList.Images.Add(value.Key, bitmap);
        }

        // Materialize the native image-list handle before releasing source bitmaps. This mirrors
        // the lifetime discipline used by the existing TreeView parity tests.
        _ = imageList.Handle;
        foreach (var bitmap in sourceImages)
        {
            bitmap.Dispose();
        }

        return imageList;
    }
}
