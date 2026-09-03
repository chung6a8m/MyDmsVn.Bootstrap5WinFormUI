using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTreeViewCheckboxInteractionTests
{
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int MkLButton = 0x0001;

    [Test]
    public void NativeCheckboxClick_MatchesPlainTreeViewAndRaisesCheckEventsExactlyOnce()
    {
        using var native = CreateCheckboxTree(new TreeView());
        using var bootstrap = CreateCheckboxTree(new BootstrapTreeView());

        var nativeResult = CaptureCheckboxClick(native);
        var bootstrapResult = CaptureCheckboxClick(bootstrap);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrapResult.Checked, Is.EqualTo(nativeResult.Checked));
            Assert.That(bootstrapResult.BeforeCheck, Is.EqualTo(nativeResult.BeforeCheck));
            Assert.That(bootstrapResult.AfterCheck, Is.EqualTo(nativeResult.AfterCheck));
            Assert.That(nativeResult.Checked, Is.True);
            Assert.That(nativeResult.BeforeCheck, Is.EqualTo(1));
            Assert.That(nativeResult.AfterCheck, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ChangingCheckBoxesAtRuntime_MatchesNativeHandleRecreationStateWithoutFrameworkRestore()
    {
        using var native = CreateExpandedTree(new TreeView());
        using var bootstrap = CreateExpandedTree(new BootstrapTreeView());

        var nativeBeforeHandle = native.Handle;
        var bootstrapBeforeHandle = bootstrap.Handle;
        native.CheckBoxes = true;
        bootstrap.CheckBoxes = true;
        var nativeAfterHandle = native.Handle;
        var bootstrapAfterHandle = bootstrap.Handle;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrap.Nodes.Count, Is.EqualTo(native.Nodes.Count));
            Assert.That(bootstrap.Nodes[0].IsExpanded, Is.EqualTo(native.Nodes[0].IsExpanded));
            Assert.That(bootstrap.SelectedNode?.Text, Is.EqualTo(native.SelectedNode?.Text));
            Assert.That(bootstrap.CheckBoxes, Is.True);
            Assert.That(bootstrapAfterHandle, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(nativeAfterHandle, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(bootstrapBeforeHandle, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(nativeBeforeHandle, Is.Not.EqualTo(IntPtr.Zero));
        }));

        Assert.DoesNotThrow((Action)(() =>
        {
            bootstrap.CheckBoxes = false;
            _ = bootstrap.Handle;
            bootstrap.Refresh();
        }));
    }

    private static T CreateCheckboxTree<T>(T treeView)
        where T : TreeView
    {
        treeView.Size = new Size(240, 100);
        treeView.ItemHeight = 24;
        treeView.CheckBoxes = true;
        treeView.Nodes.Add(new TreeNode("Node") { Checked = false });
        _ = treeView.Handle;
        return treeView;
    }

    private static T CreateExpandedTree<T>(T treeView)
        where T : TreeView
    {
        treeView.Size = new Size(240, 120);
        treeView.ItemHeight = 24;
        var root = new TreeNode("Root");
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        treeView.SelectedNode = root.Nodes[0];
        root.Expand();
        _ = treeView.Handle;
        return treeView;
    }

    private static CheckboxClickSnapshot CaptureCheckboxClick(TreeView treeView)
    {
        var node = treeView.Nodes[0];
        var snapshot = new CheckboxClickSnapshot();
        treeView.BeforeCheck += (_, _) => snapshot.BeforeCheck++;
        treeView.AfterCheck += (_, _) => snapshot.AfterCheck++;

        var point = GetNativeHitPoint(treeView, node, TreeViewHitTestLocations.StateImage);
        SendMouseMessage(treeView.Handle, WmLButtonDown, point, buttonDown: true);
        SendMouseMessage(treeView.Handle, WmLButtonUp, point, buttonDown: false);
        snapshot.Checked = node.Checked;
        return snapshot;
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

    private static void SendMouseMessage(IntPtr handle, int message, Point point, bool buttonDown)
    {
        var lParam = new IntPtr((point.Y << 16) | (point.X & 0xFFFF));
        SendMessage(handle, message, buttonDown ? new IntPtr(MkLButton) : IntPtr.Zero, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed class CheckboxClickSnapshot
    {
        internal int BeforeCheck { get; set; }

        internal int AfterCheck { get; set; }

        internal bool Checked { get; set; }
    }
}
