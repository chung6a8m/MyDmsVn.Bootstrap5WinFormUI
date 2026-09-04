using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
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
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmLButtonDoubleClick = 0x0203;
    private const int MkLButton = 0x0001;

    [Test]
    public void Constructor_PreservesNativeTreeViewContractAndBootstrapDefaults()
    {
        using var native = new TreeView();
        using var treeView = new BootstrapTreeView();

        Assert.That(treeView, Is.InstanceOf<TreeView>());
        Assert.That(treeView.BorderStyle, Is.EqualTo(native.BorderStyle));
        Assert.That(treeView.DrawMode, Is.EqualTo(TreeViewDrawMode.OwnerDrawAll));
        Assert.That(treeView.HideSelection, Is.True);
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
    public void ExpanderVisibility_UsesNativeShowPlusMinusShowRootLinesAndLeafContract()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapTreeView.ShouldDrawExpander(0, 1, showPlusMinus: true, showRootLines: true), Is.True);
            Assert.That(BootstrapTreeView.ShouldDrawExpander(0, 1, showPlusMinus: true, showRootLines: false), Is.False);
            Assert.That(BootstrapTreeView.ShouldDrawExpander(1, 1, showPlusMinus: true, showRootLines: false), Is.True);
            Assert.That(BootstrapTreeView.ShouldDrawExpander(1, 1, showPlusMinus: false, showRootLines: true), Is.False);
            Assert.That(BootstrapTreeView.ShouldDrawExpander(1, 0, showPlusMinus: true, showRootLines: true), Is.False);
        }));
    }

    [Test]
    public void RenderNodeForTesting_NodeWithChildrenPaintsFrameworkExpander()
    {
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(220, 48),
            ShowLines = false,
            ShowPlusMinus = true,
            ShowRootLines = true,
            ItemHeight = 24,
        };
        var root = new TreeNode("Root");
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        var rowBounds = new Rectangle(0, 0, 220, 24);
        var labelBounds = new Rectangle(48, 0, 120, 24);
        var layout = BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            treeView.ClientRectangle,
            rowBounds,
            labelBounds,
            root.Level,
            96,
            rightToLeft: false,
            effectiveFullRowSelection: false,
            hasExpander: true,
            hasStateImage: false,
            nativeStateImageSlotWidth: 0,
            hasNodeImage: false,
            nodeImageSize: Size.Empty));

        using var bitmap = new Bitmap(220, 48);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);

        treeView.RenderNodeForTesting(graphics, root, rowBounds, labelBounds, (TreeNodeStates)0);

        Assert.That(ContainsPaint(bitmap, layout.ExpanderBounds, Color.Magenta), Is.True);
    }

    [Test]
    public void RenderNodeForTesting_LeafWithShowLinesPaintsNativeHierarchyConnector()
    {
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(220, 72),
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            ItemHeight = 24,
            Indent = 19,
        };
        var root = new TreeNode("Root");
        var leaf = new TreeNode("Leaf");
        root.Nodes.Add(leaf);
        treeView.Nodes.Add(root);
        var rowBounds = new Rectangle(0, 24, 220, 24);
        var labelBounds = new Rectangle(67, 24, 120, 24);
        var layout = BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            treeView.ClientRectangle,
            rowBounds,
            labelBounds,
            leaf.Level,
            96,
            rightToLeft: false,
            effectiveFullRowSelection: false,
            hasExpander: false,
            hasStateImage: false,
            nativeStateImageSlotWidth: 0,
            hasNodeImage: false,
            nodeImageSize: Size.Empty));

        using var bitmap = new Bitmap(220, 72);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);

        treeView.RenderNodeForTesting(graphics, leaf, rowBounds, labelBounds, (TreeNodeStates)0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.ExpanderBounds.IsEmpty, Is.True);
            Assert.That(ContainsPaint(bitmap, layout.ExpanderSlotBounds, Color.Magenta), Is.True);
        }));
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
    public void NativeHitTestParity_NonRootExpanderRemainsInteractiveWhenRootLinesAreHidden()
    {
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(260, 120),
            ShowPlusMinus = true,
            ShowRootLines = false,
            Indent = 19,
            ItemHeight = 24,
        };
        var root = new TreeNode("Root");
        var branch = new TreeNode("Branch");
        branch.Nodes.Add(new TreeNode("Leaf"));
        root.Nodes.Add(branch);
        treeView.Nodes.Add(root);
        root.Expand();
        _ = treeView.Handle;

        var layout = CalculateNativeAnchoredLayout(
            treeView,
            branch,
            dpi: 96,
            rightToLeft: false,
            nativeStateImageSlotWidth: 0,
            hasStateImage: false,
            nodeImageSize: Size.Empty);

        AssertNativeHit(treeView, branch, layout.ExpanderBounds, TreeViewHitTestLocations.PlusMinus);
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

    [TestCase(false)]
    [TestCase(true)]
    public void NativePlusMinusMouseSequence_MatchesPlainTreeViewWithoutFrameworkSynthesis(bool doubleClick)
    {
        using var native = CreateInteractionTree(new TreeView());
        using var bootstrap = CreateInteractionTree(new BootstrapTreeView());

        var nativeResult = CapturePlusMinusMouseSequence(native, doubleClick);
        var bootstrapResult = CapturePlusMinusMouseSequence(bootstrap, doubleClick);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrapResult.BeforeExpand, Is.EqualTo(nativeResult.BeforeExpand));
            Assert.That(bootstrapResult.AfterExpand, Is.EqualTo(nativeResult.AfterExpand));
            Assert.That(bootstrapResult.BeforeCollapse, Is.EqualTo(nativeResult.BeforeCollapse));
            Assert.That(bootstrapResult.AfterCollapse, Is.EqualTo(nativeResult.AfterCollapse));
            Assert.That(bootstrapResult.RootExpanded, Is.EqualTo(nativeResult.RootExpanded));
            Assert.That(nativeResult.TotalTransitions, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void ProgrammaticExpandCollapseSequence_MatchesPlainTreeViewEventSemantics()
    {
        using var native = CreateProgrammaticTree(new TreeView());
        using var bootstrap = CreateProgrammaticTree(new BootstrapTreeView());

        var nativeResult = CaptureProgrammaticTransitions(native);
        var bootstrapResult = CaptureProgrammaticTransitions(bootstrap);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrapResult.BeforeExpand, Is.EqualTo(nativeResult.BeforeExpand));
            Assert.That(bootstrapResult.AfterExpand, Is.EqualTo(nativeResult.AfterExpand));
            Assert.That(bootstrapResult.BeforeCollapse, Is.EqualTo(nativeResult.BeforeCollapse));
            Assert.That(bootstrapResult.AfterCollapse, Is.EqualTo(nativeResult.AfterCollapse));
            Assert.That(bootstrapResult.RootExpanded, Is.EqualTo(nativeResult.RootExpanded));
            Assert.That(bootstrapResult.ChildExpanded, Is.EqualTo(nativeResult.ChildExpanded));
        }));
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
            BootstrapTreeView.ShouldDrawExpander(
                node.Level,
                node.Nodes.Count,
                treeView.ShowPlusMinus,
                treeView.ShowRootLines),
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

    private static Point GetNativeHitPoint(
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
        return new Point(first + ((last - first) / 2), y);
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

        Assert.Multiple((Action)(() =>
        {
            Assert.That(hit.Node, Is.SameAs(expectedNode), $"Native hit node at {point} for {expectedLocation}.");
            Assert.That(hit.Location & expectedLocation, Is.EqualTo(expectedLocation), $"Native hit location at {point}.");
        }));
    }

    private static T CreateInteractionTree<T>(T treeView)
        where T : TreeView
    {
        treeView.Size = new Size(240, 100);
        treeView.ShowPlusMinus = true;
        treeView.ShowRootLines = true;
        treeView.Indent = 19;
        treeView.ItemHeight = 24;
        var root = new TreeNode("Root");
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        _ = treeView.Handle;
        return treeView;
    }

    private static T CreateProgrammaticTree<T>(T treeView)
        where T : TreeView
    {
        treeView.Size = new Size(240, 120);
        var root = new TreeNode("Root");
        var child = new TreeNode("Child");
        child.Nodes.Add(new TreeNode("Grandchild"));
        root.Nodes.Add(child);
        treeView.Nodes.Add(root);
        _ = treeView.Handle;
        return treeView;
    }

    private static NativeTransitionSnapshot CapturePlusMinusMouseSequence(TreeView treeView, bool doubleClick)
    {
        var root = treeView.Nodes[0];
        var snapshot = new NativeTransitionSnapshot();
        treeView.BeforeExpand += (_, _) => snapshot.BeforeExpand++;
        treeView.AfterExpand += (_, _) => snapshot.AfterExpand++;
        treeView.BeforeCollapse += (_, _) => snapshot.BeforeCollapse++;
        treeView.AfterCollapse += (_, _) => snapshot.AfterCollapse++;

        var point = GetNativeHitPoint(treeView, root, TreeViewHitTestLocations.PlusMinus);
        SendMouseMessage(treeView.Handle, WmLButtonDown, point, buttonDown: true);
        SendMouseMessage(treeView.Handle, WmLButtonUp, point, buttonDown: false);
        if (doubleClick)
        {
            SendMouseMessage(treeView.Handle, WmLButtonDoubleClick, point, buttonDown: true);
            SendMouseMessage(treeView.Handle, WmLButtonUp, point, buttonDown: false);
        }

        snapshot.RootExpanded = root.IsExpanded;
        return snapshot;
    }

    private static NativeTransitionSnapshot CaptureProgrammaticTransitions(TreeView treeView)
    {
        var root = treeView.Nodes[0];
        var child = root.Nodes[0];
        var snapshot = new NativeTransitionSnapshot();
        treeView.BeforeExpand += (_, _) => snapshot.BeforeExpand++;
        treeView.AfterExpand += (_, _) => snapshot.AfterExpand++;
        treeView.BeforeCollapse += (_, _) => snapshot.BeforeCollapse++;
        treeView.AfterCollapse += (_, _) => snapshot.AfterCollapse++;

        root.Expand();
        treeView.Refresh();
        root.Collapse();
        treeView.Refresh();
        treeView.ExpandAll();
        treeView.Refresh();
        treeView.CollapseAll();
        treeView.Refresh();

        snapshot.RootExpanded = root.IsExpanded;
        snapshot.ChildExpanded = child.IsExpanded;
        return snapshot;
    }

    private static void SendMouseMessage(IntPtr handle, int message, Point point, bool buttonDown)
    {
        var lParam = new IntPtr((point.Y << 16) | (point.X & 0xFFFF));
        SendMessage(handle, message, buttonDown ? new IntPtr(MkLButton) : IntPtr.Zero, lParam);
    }

    private static bool ContainsPaint(Bitmap bitmap, Rectangle bounds, Color background)
    {
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (var x = bounds.Left; x < bounds.Right; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != background.ToArgb())
                {
                    return true;
                }
            }
        }

        return false;
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

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed class NativeTransitionSnapshot
    {
        internal int BeforeExpand { get; set; }

        internal int AfterExpand { get; set; }

        internal int BeforeCollapse { get; set; }

        internal int AfterCollapse { get; set; }

        internal bool RootExpanded { get; set; }

        internal bool ChildExpanded { get; set; }

        internal int TotalTransitions => BeforeExpand + BeforeCollapse;
    }
}
