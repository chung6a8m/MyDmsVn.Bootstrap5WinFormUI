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
public sealed class BootstrapTreeViewFullRowNativeBaselineTests
{
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmLButtonDoubleClick = 0x0203;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int MkLButton = 0x0001;
    private const int MkRButton = 0x0002;

    [Test]
    public void NativeRightOfLabelClick_SelectsFullRowOnceAndBootstrapMatchesNative()
    {
        using var nativeHost = CreateHostedTree(new TreeView());
        using var bootstrapHost = CreateHostedTree(new BootstrapTreeView());

        var nativeSnapshot = CaptureRightOfLabelClick(nativeHost.TreeView, MouseButtons.Left);
        var bootstrapSnapshot = CaptureRightOfLabelClick(bootstrapHost.TreeView, MouseButtons.Left);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeSnapshot.SelectedTarget, Is.True,
                "Native FullRowSelect selects the row when clicking its RightOfLabel hit region.");
            Assert.That(nativeSnapshot.BeforeSelect, Is.EqualTo(1));
            Assert.That(nativeSnapshot.AfterSelect, Is.EqualTo(1));
            Assert.That(nativeSnapshot.NodeMouseClick, Is.Zero,
                "Native TreeView does not raise NodeMouseClick for a RightOfLabel full-row click.");

            Assert.That(bootstrapSnapshot.SelectedTarget, Is.EqualTo(nativeSnapshot.SelectedTarget));
            Assert.That(bootstrapSnapshot.BeforeSelect, Is.EqualTo(nativeSnapshot.BeforeSelect));
            Assert.That(bootstrapSnapshot.AfterSelect, Is.EqualTo(nativeSnapshot.AfterSelect));
            Assert.That(bootstrapSnapshot.NodeMouseClick, Is.EqualTo(nativeSnapshot.NodeMouseClick));
        }));
    }

    [Test]
    public void NativeRightOfLabelRightClick_DoesNotForceSelectionAndBootstrapMatchesNative()
    {
        using var nativeHost = CreateHostedTree(new TreeView());
        using var bootstrapHost = CreateHostedTree(new BootstrapTreeView());

        var nativeSnapshot = CaptureRightOfLabelClick(nativeHost.TreeView, MouseButtons.Right);
        var bootstrapSnapshot = CaptureRightOfLabelClick(bootstrapHost.TreeView, MouseButtons.Right);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeSnapshot.SelectedIndex, Is.EqualTo(0),
                "Native right-click in RightOfLabel should not silently force selection to the target row.");
            Assert.That(nativeSnapshot.BeforeSelect, Is.Zero);
            Assert.That(nativeSnapshot.AfterSelect, Is.Zero);
            Assert.That(bootstrapSnapshot.SelectedIndex, Is.EqualTo(nativeSnapshot.SelectedIndex));
            Assert.That(bootstrapSnapshot.BeforeSelect, Is.EqualTo(nativeSnapshot.BeforeSelect));
            Assert.That(bootstrapSnapshot.AfterSelect, Is.EqualTo(nativeSnapshot.AfterSelect));
            Assert.That(bootstrapSnapshot.NodeMouseClick, Is.EqualTo(nativeSnapshot.NodeMouseClick));
        }));
    }

    [Test]
    public void NativeBlankAreaBelowLastNode_DoesNotChangeSelectionAndBootstrapMatchesNative()
    {
        using var nativeHost = CreateHostedTree(new TreeView());
        using var bootstrapHost = CreateHostedTree(new BootstrapTreeView());

        var nativeSnapshot = CaptureBlankAreaClick(nativeHost.TreeView);
        var bootstrapSnapshot = CaptureBlankAreaClick(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeSnapshot.SelectedIndex, Is.EqualTo(0));
            Assert.That(nativeSnapshot.BeforeSelect, Is.Zero);
            Assert.That(nativeSnapshot.AfterSelect, Is.Zero);
            Assert.That(bootstrapSnapshot.SelectedIndex, Is.EqualTo(nativeSnapshot.SelectedIndex));
            Assert.That(bootstrapSnapshot.BeforeSelect, Is.EqualTo(nativeSnapshot.BeforeSelect));
            Assert.That(bootstrapSnapshot.AfterSelect, Is.EqualTo(nativeSnapshot.AfterSelect));
        }));
    }

    [Test]
    public void NativeLabelDoubleClick_TogglesExpansionOncePerGestureAndBootstrapMatchesNative()
    {
        using var nativeHost = CreateHostedTree(new TreeView());
        using var bootstrapHost = CreateHostedTree(new BootstrapTreeView());

        var nativeSnapshot = CaptureDoubleClickToggle(nativeHost.TreeView);
        var bootstrapSnapshot = CaptureDoubleClickToggle(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeSnapshot.ExpandedAfterFirstGesture, Is.True,
                "Native TreeView should expand a collapsed node with children on label double-click.");
            Assert.That(nativeSnapshot.CollapsedAfterSecondGesture, Is.True,
                "Native TreeView should collapse the expanded node on the next label double-click.");
            Assert.That(nativeSnapshot.BeforeExpand, Is.EqualTo(1));
            Assert.That(nativeSnapshot.AfterExpand, Is.EqualTo(1));
            Assert.That(nativeSnapshot.BeforeCollapse, Is.EqualTo(1));
            Assert.That(nativeSnapshot.AfterCollapse, Is.EqualTo(1));

            Assert.That(bootstrapSnapshot.ExpandedAfterFirstGesture, Is.EqualTo(nativeSnapshot.ExpandedAfterFirstGesture));
            Assert.That(bootstrapSnapshot.CollapsedAfterSecondGesture, Is.EqualTo(nativeSnapshot.CollapsedAfterSecondGesture));
            Assert.That(bootstrapSnapshot.BeforeExpand, Is.EqualTo(nativeSnapshot.BeforeExpand));
            Assert.That(bootstrapSnapshot.AfterExpand, Is.EqualTo(nativeSnapshot.AfterExpand));
            Assert.That(bootstrapSnapshot.BeforeCollapse, Is.EqualTo(nativeSnapshot.BeforeCollapse));
            Assert.That(bootstrapSnapshot.AfterCollapse, Is.EqualTo(nativeSnapshot.AfterCollapse));
        }));
    }

    [Test]
    public void NativeItemDrag_FiresOnceAndBootstrapMatchesNative()
    {
        using var nativeHost = CreateHostedTree(new TreeView());
        using var bootstrapHost = CreateHostedTree(new BootstrapTreeView());

        var nativeSnapshot = CaptureItemDrag(nativeHost.TreeView);
        var bootstrapSnapshot = CaptureItemDrag(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeSnapshot.ItemDrag, Is.EqualTo(1),
                "Native TreeView should raise ItemDrag once after the drag threshold is crossed.");
            Assert.That(nativeSnapshot.DraggedTarget, Is.True);
            Assert.That(nativeSnapshot.Button, Is.EqualTo(MouseButtons.Left));
            Assert.That(bootstrapSnapshot.ItemDrag, Is.EqualTo(nativeSnapshot.ItemDrag));
            Assert.That(bootstrapSnapshot.DraggedTarget, Is.EqualTo(nativeSnapshot.DraggedTarget));
            Assert.That(bootstrapSnapshot.Button, Is.EqualTo(nativeSnapshot.Button));
            Assert.That(bootstrapSnapshot.SelectedIndex, Is.EqualTo(nativeSnapshot.SelectedIndex));
        }));
    }

    private static HostedTree<T> CreateHostedTree<T>(T treeView)
        where T : TreeView
    {
        var form = new Form
        {
            ClientSize = new Size(360, 220),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-2000, -2000),
        };

        treeView.Location = new Point(8, 8);
        treeView.Size = new Size(320, 160);
        treeView.ItemHeight = 24;
        treeView.DrawMode = TreeViewDrawMode.OwnerDrawAll;
        treeView.FullRowSelect = true;
        treeView.ShowLines = false;
        treeView.ShowPlusMinus = false;
        treeView.ShowRootLines = false;
        treeView.Nodes.Add(new TreeNode("First node"));
        treeView.Nodes.Add(new TreeNode("Second node"));

        form.Controls.Add(treeView);
        form.Show();
        Application.DoEvents();
        treeView.SelectedNode = treeView.Nodes[0];
        treeView.Focus();
        Application.DoEvents();
        _ = treeView.Handle;

        return new HostedTree<T>(form, treeView);
    }

    private static SelectionSnapshot CaptureRightOfLabelClick(TreeView treeView, MouseButtons button)
    {
        var target = treeView.Nodes[1];
        var point = GetNativeRightOfLabelPoint(treeView, target);
        var snapshot = CaptureClick(treeView, point, button);
        snapshot.SelectedTarget = ReferenceEquals(treeView.SelectedNode, target);
        snapshot.SelectedIndex = treeView.SelectedNode?.Index ?? -1;
        return snapshot;
    }

    private static SelectionSnapshot CaptureBlankAreaClick(TreeView treeView)
    {
        var last = treeView.Nodes[treeView.Nodes.Count - 1];
        var y = Math.Min(treeView.ClientRectangle.Bottom - 2, last.Bounds.Bottom + treeView.ItemHeight);
        var point = new Point(Math.Max(2, treeView.ClientRectangle.Width / 2), y);
        var hit = treeView.HitTest(point);
        Assert.That(hit.Node, Is.Null, "The baseline click must be below the last visible node.");

        var snapshot = CaptureClick(treeView, point, MouseButtons.Left);
        snapshot.SelectedIndex = treeView.SelectedNode?.Index ?? -1;
        return snapshot;
    }

    private static ExpandCollapseSnapshot CaptureDoubleClickToggle(TreeView treeView)
    {
        var target = treeView.Nodes[0];
        target.Nodes.Clear();
        target.Nodes.Add(new TreeNode("Child node"));
        target.Collapse();
        treeView.SelectedNode = target;
        Application.DoEvents();

        var snapshot = new ExpandCollapseSnapshot();
        treeView.BeforeExpand += (_, args) =>
        {
            if (ReferenceEquals(args.Node, target))
            {
                snapshot.BeforeExpand++;
            }
        };
        treeView.AfterExpand += (_, args) =>
        {
            if (ReferenceEquals(args.Node, target))
            {
                snapshot.AfterExpand++;
            }
        };
        treeView.BeforeCollapse += (_, args) =>
        {
            if (ReferenceEquals(args.Node, target))
            {
                snapshot.BeforeCollapse++;
            }
        };
        treeView.AfterCollapse += (_, args) =>
        {
            if (ReferenceEquals(args.Node, target))
            {
                snapshot.AfterCollapse++;
            }
        };

        var point = GetNativeLabelPoint(target);
        PostLeftDoubleClick(treeView, point);
        snapshot.ExpandedAfterFirstGesture = target.IsExpanded;

        PostLeftDoubleClick(treeView, point);
        snapshot.CollapsedAfterSecondGesture = !target.IsExpanded;
        return snapshot;
    }

    private static DragSnapshot CaptureItemDrag(TreeView treeView)
    {
        var target = treeView.Nodes[0];
        treeView.SelectedNode = target;
        Application.DoEvents();

        var snapshot = new DragSnapshot();
        treeView.ItemDrag += (_, args) =>
        {
            snapshot.ItemDrag++;
            snapshot.DraggedTarget = ReferenceEquals(args.Item, target);
            snapshot.Button = args.Button;
        };

        var start = GetNativeLabelPoint(target);
        var dragDistance = Math.Max(SystemInformation.DragSize.Width + 4, 12);
        var move = new Point(
            Math.Min(treeView.ClientRectangle.Right - 2, start.X + dragDistance),
            start.Y);

        PostMouseMessage(treeView.Handle, WmLButtonDown, start, MkLButton);
        PostMouseMessage(treeView.Handle, WmMouseMove, move, MkLButton);
        PostMouseMessage(treeView.Handle, WmMouseMove,
            new Point(Math.Min(treeView.ClientRectangle.Right - 2, move.X + 4), move.Y), MkLButton);
        PostMouseMessage(treeView.Handle, WmLButtonUp, move, 0);
        Application.DoEvents();

        snapshot.SelectedIndex = treeView.SelectedNode?.Index ?? -1;
        return snapshot;
    }

    private static SelectionSnapshot CaptureClick(TreeView treeView, Point point, MouseButtons button)
    {
        var snapshot = new SelectionSnapshot();
        treeView.BeforeSelect += (_, _) => snapshot.BeforeSelect++;
        treeView.AfterSelect += (_, _) => snapshot.AfterSelect++;
        treeView.NodeMouseClick += (_, _) => snapshot.NodeMouseClick++;

        switch (button)
        {
            case MouseButtons.Left:
                PostMouseMessage(treeView.Handle, WmLButtonDown, point, MkLButton);
                PostMouseMessage(treeView.Handle, WmLButtonUp, point, 0);
                break;
            case MouseButtons.Right:
                PostMouseMessage(treeView.Handle, WmRButtonDown, point, MkRButton);
                PostMouseMessage(treeView.Handle, WmRButtonUp, point, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(button));
        }

        Application.DoEvents();
        return snapshot;
    }

    private static void PostLeftDoubleClick(TreeView treeView, Point point)
    {
        PostMouseMessage(treeView.Handle, WmLButtonDown, point, MkLButton);
        PostMouseMessage(treeView.Handle, WmLButtonUp, point, 0);
        PostMouseMessage(treeView.Handle, WmLButtonDoubleClick, point, MkLButton);
        PostMouseMessage(treeView.Handle, WmLButtonUp, point, 0);
        Application.DoEvents();
    }

    private static Point GetNativeLabelPoint(TreeNode node)
    {
        var bounds = node.Bounds;
        Assert.That(bounds.IsEmpty, Is.False, "Expected a visible native label rectangle.");
        return new Point(
            bounds.Left + Math.Max(1, bounds.Width / 2),
            bounds.Top + Math.Max(1, bounds.Height / 2));
    }

    private static Point GetNativeRightOfLabelPoint(TreeView treeView, TreeNode node)
    {
        var y = node.Bounds.Top + Math.Max(1, node.Bounds.Height / 2);
        for (var x = node.Bounds.Right + 1; x < treeView.ClientRectangle.Right - 1; x++)
        {
            var hit = treeView.HitTest(x, y);
            if (ReferenceEquals(hit.Node, node) &&
                (hit.Location & TreeViewHitTestLocations.RightOfLabel) == TreeViewHitTestLocations.RightOfLabel)
            {
                return new Point(x, y);
            }
        }

        Assert.Fail("Expected a native RightOfLabel hit point inside the visible row.");
        return Point.Empty;
    }

    private static void PostMouseMessage(IntPtr handle, int message, Point point, int keyState)
    {
        var lParam = new IntPtr((point.Y << 16) | (point.X & 0xFFFF));
        var posted = PostMessage(handle, message, new IntPtr(keyState), lParam);

        Assert.That(posted, Is.True, $"Could not post native mouse message 0x{message:X}.");
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed class HostedTree<T> : IDisposable
        where T : TreeView
    {
        internal HostedTree(Form form, T treeView)
        {
            Form = form;
            TreeView = treeView;
        }

        internal Form Form { get; }

        internal T TreeView { get; }

        public void Dispose()
        {
            Form.Close();
            Form.Dispose();
        }
    }

    private sealed class SelectionSnapshot
    {
        internal int BeforeSelect { get; set; }

        internal int AfterSelect { get; set; }

        internal int NodeMouseClick { get; set; }

        internal int SelectedIndex { get; set; }

        internal bool SelectedTarget { get; set; }
    }

    private sealed class ExpandCollapseSnapshot
    {
        internal int BeforeExpand { get; set; }

        internal int AfterExpand { get; set; }

        internal int BeforeCollapse { get; set; }

        internal int AfterCollapse { get; set; }

        internal bool ExpandedAfterFirstGesture { get; set; }

        internal bool CollapsedAfterSecondGesture { get; set; }
    }

    private sealed class DragSnapshot
    {
        internal int ItemDrag { get; set; }

        internal int SelectedIndex { get; set; }

        internal bool DraggedTarget { get; set; }

        internal MouseButtons Button { get; set; }
    }
}
