using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTreeViewTests
{
    [Test]
    public void Constructor_PreservesNativeTreeViewContractAndBootstrapDefaults()
    {
        using var treeView = new BootstrapTreeView();

        Assert.That(treeView, Is.InstanceOf<TreeView>());
        Assert.That(treeView.BorderStyle, Is.EqualTo(BorderStyle.None));
        Assert.That(treeView.DrawMode, Is.EqualTo(TreeViewDrawMode.OwnerDrawAll));
        Assert.That(treeView.HideSelection, Is.False);
        Assert.That(treeView.Variant, Is.EqualTo(BootstrapVariant.Primary));
    }

    [Test]
    public void NativeMembers_RemainInheritedWithoutShadowWrappers()
    {
        foreach (var propertyName in new[]
                 {
                     nameof(TreeView.Nodes),
                     nameof(TreeView.SelectedNode),
                     nameof(TreeView.CheckBoxes),
                     nameof(TreeView.ImageList),
                     nameof(TreeView.LabelEdit),
                 })
        {
            var declaredMember = typeof(BootstrapTreeView).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.That(declaredMember, Is.Null, $"{propertyName} must remain inherited from TreeView.");
        }

        using var treeView = new BootstrapTreeView();
        using var imageList = new ImageList();
        var root = new TreeNode("Root");

        treeView.Nodes.Add(root);
        treeView.SelectedNode = root;
        treeView.CheckBoxes = true;
        treeView.ImageList = imageList;
        treeView.LabelEdit = true;

        Assert.That(treeView.Nodes[0], Is.SameAs(root));
        Assert.That(treeView.SelectedNode, Is.SameAs(root));
        Assert.That(treeView.CheckBoxes, Is.True);
        Assert.That(treeView.ImageList, Is.SameAs(imageList));
        Assert.That(treeView.LabelEdit, Is.True);
    }

    [Test]
    public void NativePresentationProperties_RoundTrip()
    {
        using var treeView = new BootstrapTreeView
        {
            FullRowSelect = true,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            Indent = 28,
            ItemHeight = 24,
        };

        Assert.That(treeView.FullRowSelect, Is.True);
        Assert.That(treeView.ShowLines, Is.False);
        Assert.That(treeView.ShowPlusMinus, Is.False);
        Assert.That(treeView.ShowRootLines, Is.False);
        Assert.That(treeView.Indent, Is.EqualTo(28));
        Assert.That(treeView.ItemHeight, Is.EqualTo(24));
    }

    [Test]
    public void NativeHitTestParity_LtrCheckboxImageAndExpander_UsesFrameworkLayoutCenters()
    {
        using var imageList = CreateImageList(new Size(16, 16), 1);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(280, 120),
            CheckBoxes = true,
            ImageList = imageList,
            ShowPlusMinus = true,
            ShowRootLines = true,
            Indent = 19,
            ItemHeight = 24,
        };
        var root = new TreeNode("Root node") { ImageIndex = 0, SelectedImageIndex = 0, Checked = true };
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        root.Expand();
        _ = treeView.Handle;
        var nativeStateImageSlotWidth = GetNativeHitRegionWidth(treeView, root, TreeViewHitTestLocations.StateImage);

        var layout = CalculateNativeAnchoredLayout(
            treeView,
            root,
            dpi: 96,
            rightToLeft: false,
            nativeStateImageSlotWidth,
            hasStateImage: true,
            nodeImageSize: imageList.ImageSize);

        AssertNativeHit(treeView, root, layout.ExpanderBounds, TreeViewHitTestLocations.PlusMinus);
        AssertNativeHit(treeView, root, layout.StateImageBounds, TreeViewHitTestLocations.StateImage);
        AssertNativeHit(treeView, root, layout.NodeImageBounds, TreeViewHitTestLocations.Image);
    }

    [Test]
    public void NativeHitTestParity_RtlEffective144Scale_StateImageImageAndExpander_UsesFrameworkLayoutCenters()
    {
        const int dpi = 144;
        var scaledImageSize = DpiScaler.Scale(new Size(16, 16), dpi);
        using var imageList = CreateImageList(scaledImageSize, 1);
        using var stateImageList = CreateImageList(scaledImageSize, 2);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(DpiScaler.Scale(320, dpi), DpiScaler.Scale(140, dpi)),
            ImageList = imageList,
            StateImageList = stateImageList,
            ShowPlusMinus = true,
            ShowRootLines = true,
            Indent = DpiScaler.Scale(19, dpi),
            ItemHeight = DpiScaler.Scale(24, dpi),
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true,
        };
        var root = new TreeNode("RTL root node")
        {
            ImageIndex = 0,
            SelectedImageIndex = 0,
            StateImageIndex = 0,
        };
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        root.Expand();
        _ = treeView.Handle;
        var nativeStateImageSlotWidth = GetNativeHitRegionWidth(treeView, root, TreeViewHitTestLocations.StateImage);

        var layout = CalculateNativeAnchoredLayout(
            treeView,
            root,
            dpi,
            rightToLeft: true,
            nativeStateImageSlotWidth,
            hasStateImage: true,
            nodeImageSize: imageList.ImageSize);

        AssertNativeHit(treeView, root, layout.ExpanderBounds, TreeViewHitTestLocations.PlusMinus);
        AssertNativeHit(treeView, root, layout.StateImageBounds, TreeViewHitTestLocations.StateImage);
        AssertNativeHit(treeView, root, layout.NodeImageBounds, TreeViewHitTestLocations.Image);
    }

    [Test]
    public void NativeHitTestParity_DeepScrolledNode_RemainsNativeAnchored()
    {
        using var imageList = CreateImageList(new Size(16, 16), 1);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(150, 160),
            CheckBoxes = true,
            ImageList = imageList,
            ShowPlusMinus = true,
            ShowRootLines = true,
            Indent = 19,
            ItemHeight = 24,
            Scrollable = true,
        };
        var root = new TreeNode("Root") { ImageIndex = 0, SelectedImageIndex = 0 };
        var current = root;
        for (var index = 0; index < 8; index++)
        {
            var child = new TreeNode("Nested node " + index) { ImageIndex = 0, SelectedImageIndex = 0, Checked = true };
            current.Nodes.Add(child);
            current = child;
        }

        current.Nodes.Add(new TreeNode("Leaf"));
        treeView.Nodes.Add(root);
        root.ExpandAll();
        _ = treeView.Handle;
        var nativeStateImageSlotWidth = GetNativeHitRegionWidth(treeView, root, TreeViewHitTestLocations.StateImage);
        current.EnsureVisible();

        var layout = CalculateNativeAnchoredLayout(
            treeView,
            current,
            dpi: 96,
            rightToLeft: false,
            nativeStateImageSlotWidth,
            hasStateImage: true,
            nodeImageSize: imageList.ImageSize);

        AssertNativeHit(treeView, current, layout.ExpanderBounds, TreeViewHitTestLocations.PlusMinus);
        AssertNativeHit(treeView, current, layout.StateImageBounds, TreeViewHitTestLocations.StateImage);
        AssertNativeHit(treeView, current, layout.NodeImageBounds, TreeViewHitTestLocations.Image);
    }

    private static BootstrapTreeViewNodeLayout CalculateNativeAnchoredLayout(
        BootstrapTreeView treeView,
        TreeNode node,
        int dpi,
        bool rightToLeft,
        int nativeStateImageSlotWidth,
        bool hasStateImage,
        Size nodeImageSize)
    {
        var nativeBounds = node.Bounds;
        var rowBounds = new Rectangle(0, nativeBounds.Top, treeView.ClientSize.Width, treeView.ItemHeight);
        return BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            treeView.ClientRectangle,
            rowBounds,
            nativeBounds,
            node.Level,
            dpi,
            rightToLeft,
            treeView.FullRowSelect && !treeView.ShowLines,
            node.Nodes.Count > 0 && treeView.ShowPlusMinus && (node.Level > 0 || treeView.ShowRootLines),
            hasStateImage,
            nativeStateImageSlotWidth,
            treeView.ImageList is not null && node.ImageIndex >= 0,
            nodeImageSize));
    }

    private static int GetNativeHitRegionWidth(
        TreeView treeView,
        TreeNode expectedNode,
        TreeViewHitTestLocations expectedLocation)
    {
        var nativeBounds = expectedNode.Bounds;
        var y = nativeBounds.Top + (nativeBounds.Height / 2);
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
        return last - first + 1;
    }

    private static void AssertNativeHit(
        TreeView treeView,
        TreeNode expectedNode,
        Rectangle bounds,
        TreeViewHitTestLocations expectedLocation)
    {
        Assert.That(bounds.IsEmpty, Is.False, $"Expected a non-empty {expectedLocation} rectangle.");
        var point = new Point(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
        var hit = treeView.HitTest(point);

        Assert.Multiple((System.Action)(() =>
        {
            Assert.That(hit.Node, Is.SameAs(expectedNode), $"Native hit node at {point} for {expectedLocation}.");
            Assert.That(hit.Location & expectedLocation, Is.EqualTo(expectedLocation), $"Native hit location at {point}.");
        }));
    }

    private static ImageList CreateImageList(Size imageSize, int count)
    {
        var imageList = new ImageList { ImageSize = imageSize };
        for (var index = 0; index < count; index++)
        {
            imageList.Images.Add(new Bitmap(imageSize.Width, imageSize.Height));
        }

        return imageList;
    }
}
