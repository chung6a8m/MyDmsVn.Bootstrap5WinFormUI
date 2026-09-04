using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTreeViewNativeInteractionContractTests
{
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int MkLButton = 0x0001;
    private const int TvmGetEditControl = 0x110F;

    [Test]
    public void UpDownNavigation_UsesNativeSelectionPathAndSingleEventSequence()
    {
        using var nativeHost = CreateFlatHostedTree(new TreeView());
        using var bootstrapHost = CreateFlatHostedTree(new BootstrapTreeView());

        var native = CaptureUpDownNavigation(nativeHost.TreeView);
        var bootstrap = CaptureUpDownNavigation(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.AfterDownIndex, Is.EqualTo(2));
            Assert.That(native.AfterUpIndex, Is.EqualTo(1));
            Assert.That(native.BeforeSelect, Is.EqualTo(2));
            Assert.That(native.AfterSelect, Is.EqualTo(2));
            Assert.That(bootstrap.AfterDownIndex, Is.EqualTo(native.AfterDownIndex));
            Assert.That(bootstrap.AfterUpIndex, Is.EqualTo(native.AfterUpIndex));
            Assert.That(bootstrap.BeforeSelect, Is.EqualTo(native.BeforeSelect));
            Assert.That(bootstrap.AfterSelect, Is.EqualTo(native.AfterSelect));
        }));
    }

    [Test]
    public void LeftRightNavigation_MatchesNativeExpandCollapseAndParentChildBehavior()
    {
        using var nativeHost = CreateHierarchyHostedTree(new TreeView());
        using var bootstrapHost = CreateHierarchyHostedTree(new BootstrapTreeView());

        var native = CaptureLeftRightNavigation(nativeHost.TreeView);
        var bootstrap = CaptureLeftRightNavigation(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.RootExpandedAfterFirstRight, Is.True);
            Assert.That(native.SelectedChildAfterSecondRight, Is.True);
            Assert.That(native.SelectedRootAfterFirstLeft, Is.True);
            Assert.That(native.RootCollapsedAfterSecondLeft, Is.True);
            Assert.That(native.BeforeExpand, Is.EqualTo(1));
            Assert.That(native.AfterExpand, Is.EqualTo(1));
            Assert.That(native.BeforeCollapse, Is.EqualTo(1));
            Assert.That(native.AfterCollapse, Is.EqualTo(1));
            Assert.That(native.BeforeSelect, Is.EqualTo(2));
            Assert.That(native.AfterSelect, Is.EqualTo(2));

            Assert.That(bootstrap.RootExpandedAfterFirstRight, Is.EqualTo(native.RootExpandedAfterFirstRight));
            Assert.That(bootstrap.SelectedChildAfterSecondRight, Is.EqualTo(native.SelectedChildAfterSecondRight));
            Assert.That(bootstrap.SelectedRootAfterFirstLeft, Is.EqualTo(native.SelectedRootAfterFirstLeft));
            Assert.That(bootstrap.RootCollapsedAfterSecondLeft, Is.EqualTo(native.RootCollapsedAfterSecondLeft));
            Assert.That(bootstrap.BeforeExpand, Is.EqualTo(native.BeforeExpand));
            Assert.That(bootstrap.AfterExpand, Is.EqualTo(native.AfterExpand));
            Assert.That(bootstrap.BeforeCollapse, Is.EqualTo(native.BeforeCollapse));
            Assert.That(bootstrap.AfterCollapse, Is.EqualTo(native.AfterCollapse));
            Assert.That(bootstrap.BeforeSelect, Is.EqualTo(native.BeforeSelect));
            Assert.That(bootstrap.AfterSelect, Is.EqualTo(native.AfterSelect));
        }));
    }

    [Test]
    public void HomeEndPageNavigation_UsesNativeDirectionAndSelectionEventsWithoutFrameworkReset()
    {
        using var nativeHost = CreatePagingHostedTree(new TreeView());
        using var bootstrapHost = CreatePagingHostedTree(new BootstrapTreeView());

        var native = CaptureHomeEndPageNavigation(nativeHost.TreeView);
        var bootstrap = CaptureHomeEndPageNavigation(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.AfterHomeIndex, Is.Zero);
            Assert.That(native.AfterEndIndex, Is.EqualTo(39));
            Assert.That(native.AfterPageUpIndex, Is.LessThan(native.AfterEndIndex));
            Assert.That(native.AfterPageDownIndex, Is.GreaterThan(native.AfterPageUpIndex));
            Assert.That(native.BeforeSelect, Is.EqualTo(4));
            Assert.That(native.AfterSelect, Is.EqualTo(4));

            Assert.That(bootstrap.AfterHomeIndex, Is.Zero);
            Assert.That(bootstrap.AfterEndIndex, Is.EqualTo(39));
            Assert.That(bootstrap.AfterPageUpIndex, Is.LessThan(bootstrap.AfterEndIndex));
            Assert.That(bootstrap.AfterPageDownIndex, Is.GreaterThan(bootstrap.AfterPageUpIndex));
            Assert.That(bootstrap.BeforeSelect, Is.EqualTo(native.BeforeSelect));
            Assert.That(bootstrap.AfterSelect, Is.EqualTo(native.AfterSelect));
        }));
    }

    [Test]
    public void ExpandCollapseKeys_MatchNativeStateAndNativeEventCounts()
    {
        using var nativeHost = CreateHierarchyHostedTree(new TreeView());
        using var bootstrapHost = CreateHierarchyHostedTree(new BootstrapTreeView());

        var native = CaptureExpandCollapseKeys(nativeHost.TreeView);
        var bootstrap = CaptureExpandCollapseKeys(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.RootExpandedAfterAdd, Is.True);
            Assert.That(native.RootExpandedAfterMultiply, Is.True);
            Assert.That(native.ChildExpandedAfterMultiply, Is.True);
            Assert.That(native.RootCollapsedAfterSubtract, Is.True);
            Assert.That(native.BeforeExpand, Is.GreaterThan(0));
            Assert.That(native.AfterExpand, Is.GreaterThan(0));
            Assert.That(native.BeforeCollapse, Is.GreaterThan(0));
            Assert.That(native.AfterCollapse, Is.GreaterThan(0));

            Assert.That(bootstrap.RootExpandedAfterAdd, Is.EqualTo(native.RootExpandedAfterAdd));
            Assert.That(bootstrap.RootExpandedAfterMultiply, Is.EqualTo(native.RootExpandedAfterMultiply));
            Assert.That(bootstrap.ChildExpandedAfterMultiply, Is.EqualTo(native.ChildExpandedAfterMultiply));
            Assert.That(bootstrap.RootCollapsedAfterSubtract, Is.EqualTo(native.RootCollapsedAfterSubtract));
            Assert.That(bootstrap.BeforeExpand, Is.EqualTo(native.BeforeExpand));
            Assert.That(bootstrap.AfterExpand, Is.EqualTo(native.AfterExpand));
            Assert.That(bootstrap.BeforeCollapse, Is.EqualTo(native.BeforeCollapse));
            Assert.That(bootstrap.AfterCollapse, Is.EqualTo(native.AfterCollapse));
        }));
    }

    [Test]
    public void SpaceWithCheckBoxes_MatchesNativeCheckedStateAndRaisesCheckEventsOnce()
    {
        using var nativeHost = CreateCheckboxHostedTree(new TreeView());
        using var bootstrapHost = CreateCheckboxHostedTree(new BootstrapTreeView());

        var native = CaptureSpaceCheck(nativeHost.TreeView);
        var bootstrap = CaptureSpaceCheck(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Checked, Is.True);
            Assert.That(native.BeforeCheck, Is.EqualTo(1));
            Assert.That(native.AfterCheck, Is.EqualTo(1));
            Assert.That(bootstrap.Checked, Is.EqualTo(native.Checked));
            Assert.That(bootstrap.BeforeCheck, Is.EqualTo(native.BeforeCheck));
            Assert.That(bootstrap.AfterCheck, Is.EqualTo(native.AfterCheck));
        }));
    }

    [Test]
    public void BeginEdit_UsesNativeEditorAndCommitsOrCancelsThroughNativeLifecycle()
    {
        using var nativeHost = CreateLabelEditHostedTree(new TreeView());
        using var bootstrapHost = CreateLabelEditHostedTree(new BootstrapTreeView());

        var native = CaptureProgrammaticLabelEdit(nativeHost.TreeView);
        var bootstrap = CaptureProgrammaticLabelEdit(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.CommitEditorCreated, Is.True);
            Assert.That(native.TextAfterCommit, Is.EqualTo("Committed label"));
            Assert.That(native.CancelEditorCreated, Is.True);
            Assert.That(native.TextAfterCancel, Is.EqualTo("Committed label"));
            Assert.That(native.BeforeLabelEdit, Is.EqualTo(2));
            Assert.That(native.AfterLabelEdit, Is.EqualTo(2));

            Assert.That(bootstrap.CommitEditorCreated, Is.EqualTo(native.CommitEditorCreated));
            Assert.That(bootstrap.TextAfterCommit, Is.EqualTo(native.TextAfterCommit));
            Assert.That(bootstrap.CancelEditorCreated, Is.EqualTo(native.CancelEditorCreated));
            Assert.That(bootstrap.TextAfterCancel, Is.EqualTo(native.TextAfterCancel));
            Assert.That(bootstrap.BeforeLabelEdit, Is.EqualTo(native.BeforeLabelEdit));
            Assert.That(bootstrap.AfterLabelEdit, Is.EqualTo(native.AfterLabelEdit));
        }));
    }

    [Test]
    public void F2_MatchesPlainTreeViewWithoutFrameworkLabelEditShortcut()
    {
        using var nativeHost = CreateLabelEditHostedTree(new ProbeNativeTreeView());
        using var bootstrapHost = CreateLabelEditHostedTree(new ProbeBootstrapTreeView());

        var native = CaptureF2LabelEdit(
            nativeHost.TreeView,
            () => nativeHost.TreeView.ProcessCmdKeyForTesting(Keys.F2));
        var bootstrap = CaptureF2LabelEdit(
            bootstrapHost.TreeView,
            () => bootstrapHost.TreeView.ProcessCmdKeyForTesting(Keys.F2));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrap.CommandHandled, Is.EqualTo(native.CommandHandled));
            Assert.That(bootstrap.EditorCreated, Is.EqualTo(native.EditorCreated));
            Assert.That(bootstrap.EditorClosedAfterCancel, Is.EqualTo(native.EditorClosedAfterCancel));
            Assert.That(FindDeclaredProtectedMethod("ProcessCmdKey"), Is.Null);
            Assert.That(FindDeclaredProtectedMethod("OnKeyDown"), Is.Null);
        }));
    }

    [Test]
    public void DrawNodeEvent_RemainsObservableExactlyOnceWhileFrameworkPaintingStaysAuthoritative()
    {
        using var treeView = new BootstrapTreeView { Size = new Size(220, 60), HideSelection = false };
        var node = new TreeNode("Draw node");
        treeView.Nodes.Add(node);
        var count = 0;
        DrawTreeNodeEventArgs? observed = null;
        treeView.DrawNode += (_, args) =>
        {
            count++;
            args.DrawDefault = true;
            observed = args;
        };

        using var bitmap = new Bitmap(220, 60);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(
            graphics,
            node,
            new Rectangle(0, 0, 220, treeView.ItemHeight),
            new Rectangle(48, 0, 120, treeView.ItemHeight),
            TreeNodeStates.Selected);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed!.DrawDefault, Is.False);
            Assert.That(bitmap.GetPixel(50, 2), Is.Not.EqualTo(Color.Magenta));
        }));
    }

    [Test]
    public void NativeLabelClick_MatchesSelectionAndNodeMouseClickEventCounts()
    {
        using var nativeHost = CreateFlatHostedTree(new TreeView());
        using var bootstrapHost = CreateFlatHostedTree(new BootstrapTreeView());

        var native = CaptureLabelClick(nativeHost.TreeView);
        var bootstrap = CaptureLabelClick(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.SelectedTarget, Is.True);
            Assert.That(native.BeforeSelect, Is.EqualTo(1));
            Assert.That(native.AfterSelect, Is.EqualTo(1));
            Assert.That(bootstrap.SelectedTarget, Is.EqualTo(native.SelectedTarget));
            Assert.That(bootstrap.BeforeSelect, Is.EqualTo(native.BeforeSelect));
            Assert.That(bootstrap.AfterSelect, Is.EqualTo(native.AfterSelect));
            Assert.That(bootstrap.NodeMouseClick, Is.EqualTo(native.NodeMouseClick));
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HotTracking_RemainsInheritedNativeProperty(bool hotTracking)
    {
        using var treeView = new BootstrapTreeView { HotTracking = hotTracking };

        var declared = typeof(BootstrapTreeView).GetProperty(
            nameof(TreeView.HotTracking),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(declared, Is.Null);
            Assert.That(treeView.HotTracking, Is.EqualTo(hotTracking));
        }));
    }

    [Test]
    public void ItemDrag_MatchesNativeAndFiresExactlyOnceForRepresentativeGesture()
    {
        using var nativeHost = CreateFlatHostedTree(new TreeView());
        using var bootstrapHost = CreateFlatHostedTree(new BootstrapTreeView());

        var native = CaptureItemDrag(nativeHost.TreeView);
        var bootstrap = CaptureItemDrag(bootstrapHost.TreeView);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.ItemDrag, Is.EqualTo(1));
            Assert.That(native.DraggedTarget, Is.True);
            Assert.That(native.Button, Is.EqualTo(MouseButtons.Left));
            Assert.That(bootstrap.ItemDrag, Is.EqualTo(native.ItemDrag));
            Assert.That(bootstrap.DraggedTarget, Is.EqualTo(native.DraggedTarget));
            Assert.That(bootstrap.Button, Is.EqualTo(native.Button));
        }));
    }

    [Test]
    public void AllowDrop_RaisesNativeDragEventsOnceAndBootstrapAddsNoDragPolicyHooks()
    {
        using var treeView = new ProbeBootstrapTreeView { AllowDrop = true };
        var dragEnter = 0;
        var dragOver = 0;
        var dragDrop = 0;
        treeView.DragEnter += (_, _) => dragEnter++;
        treeView.DragOver += (_, _) => dragOver++;
        treeView.DragDrop += (_, _) => dragDrop++;

        treeView.RaiseNativeDragSequence();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.AllowDrop, Is.True);
            Assert.That(dragEnter, Is.EqualTo(1));
            Assert.That(dragOver, Is.EqualTo(1));
            Assert.That(dragDrop, Is.EqualTo(1));
            Assert.That(FindDeclaredProtectedMethod("OnDragEnter"), Is.Null);
            Assert.That(FindDeclaredProtectedMethod("OnDragOver"), Is.Null);
            Assert.That(FindDeclaredProtectedMethod("OnDragDrop"), Is.Null);
        }));
    }

    [Test]
    public void AccessibilityObject_RemainsNativeAndBootstrapDoesNotReplaceAccessibilityInstance()
    {
        using var nativeHost = CreateHierarchyHostedTree(new TreeView());
        using var bootstrapHost = CreateHierarchyHostedTree(new BootstrapTreeView());

        var native = nativeHost.TreeView.AccessibilityObject;
        var bootstrap = bootstrapHost.TreeView.AccessibilityObject;

        nativeHost.TreeView.Focus();
        Application.DoEvents();
        var nativeState = native.State;
        bootstrapHost.TreeView.Focus();
        Application.DoEvents();
        var bootstrapState = bootstrap.State;

        var declaredFactory = typeof(BootstrapTreeView).GetMethod(
            "CreateAccessibilityInstance",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.GetChildCount(), Is.EqualTo(native.GetChildCount()));
            Assert.That(bootstrap.Role, Is.EqualTo(native.Role));
            Assert.That(bootstrapState, Is.EqualTo(nativeState));
            Assert.That(declaredFactory, Is.Null);
        }));
    }

    [Test]
    public void TabAndShiftTab_MoveFocusToSiblingControlsAndTabStopCanSkipTree()
    {
        using var form = new Form
        {
            ClientSize = new Size(360, 180),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-2000, -2000),
        };
        using var before = new Button { Text = "Before", TabIndex = 0, Location = new Point(8, 8) };
        using var treeView = new ProbeBootstrapTreeView
        {
            TabIndex = 1,
            Location = new Point(8, 44),
            Size = new Size(220, 80),
        };
        using var after = new Button { Text = "After", TabIndex = 2, Location = new Point(240, 8) };
        treeView.Nodes.Add(new TreeNode("Node"));
        form.Controls.Add(before);
        form.Controls.Add(treeView);
        form.Controls.Add(after);
        form.Show();
        Application.DoEvents();

        treeView.Focus();
        Application.DoEvents();
        var tabHandled = treeView.ProcessDialogKeyForTesting(Keys.Tab);
        Application.DoEvents();
        var afterTabFocused = after.Focused;

        treeView.Focus();
        Application.DoEvents();
        var shiftTabHandled = treeView.ProcessDialogKeyForTesting(Keys.Shift | Keys.Tab);
        Application.DoEvents();
        var beforeShiftTabFocused = before.Focused;

        treeView.TabStop = false;
        before.Focus();
        Application.DoEvents();
        var moved = form.SelectNextControl(before, forward: true, tabStopOnly: true, nested: true, wrap: true);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabHandled, Is.True);
            Assert.That(afterTabFocused, Is.True);
            Assert.That(shiftTabHandled, Is.True);
            Assert.That(beforeShiftTabFocused, Is.True);
            Assert.That(moved, Is.True);
            Assert.That(after.Focused, Is.True);
        }));
    }

    private static MethodInfo? FindDeclaredProtectedMethod(string name)
    {
        return typeof(BootstrapTreeView).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
    }

    private static HostedTree<T> CreateFlatHostedTree<T>(T treeView)
        where T : TreeView
    {
        var host = CreateHostedTree(treeView);
        for (var index = 0; index < 4; index++)
        {
            treeView.Nodes.Add(new TreeNode("Node " + index));
        }

        treeView.SelectedNode = treeView.Nodes[1];
        FocusTree(treeView);
        return host;
    }

    private static HostedTree<T> CreatePagingHostedTree<T>(T treeView)
        where T : TreeView
    {
        var host = CreateHostedTree(treeView);
        treeView.Size = new Size(280, 120);
        for (var index = 0; index < 40; index++)
        {
            treeView.Nodes.Add(new TreeNode("Node " + index));
        }

        treeView.SelectedNode = treeView.Nodes[20];
        treeView.SelectedNode.EnsureVisible();
        FocusTree(treeView);
        return host;
    }

    private static HostedTree<T> CreateHierarchyHostedTree<T>(T treeView)
        where T : TreeView
    {
        var host = CreateHostedTree(treeView);
        var root = new TreeNode("Root");
        var child = new TreeNode("Child");
        child.Nodes.Add(new TreeNode("Grandchild"));
        root.Nodes.Add(child);
        treeView.Nodes.Add(root);
        root.Collapse();
        treeView.SelectedNode = root;
        FocusTree(treeView);
        return host;
    }

    private static HostedTree<T> CreateCheckboxHostedTree<T>(T treeView)
        where T : TreeView
    {
        var host = CreateHostedTree(treeView);
        treeView.CheckBoxes = true;
        treeView.Nodes.Add(new TreeNode("Check node") { Checked = false });
        treeView.SelectedNode = treeView.Nodes[0];
        FocusTree(treeView);
        return host;
    }

    private static HostedTree<T> CreateLabelEditHostedTree<T>(T treeView)
        where T : TreeView
    {
        var host = CreateHostedTree(treeView);
        treeView.LabelEdit = true;
        treeView.Nodes.Add(new TreeNode("Editable node"));
        treeView.SelectedNode = treeView.Nodes[0];
        FocusTree(treeView);
        return host;
    }

    private static HostedTree<T> CreateHostedTree<T>(T treeView)
        where T : TreeView
    {
        var form = new Form
        {
            ClientSize = new Size(340, 220),
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-2000, -2000),
        };
        treeView.Location = new Point(8, 8);
        treeView.Size = new Size(300, 160);
        treeView.ItemHeight = 24;
        form.Controls.Add(treeView);
        form.Show();
        Application.DoEvents();
        _ = treeView.Handle;
        return new HostedTree<T>(form, treeView);
    }

    private static void FocusTree(TreeView treeView)
    {
        treeView.Focus();
        Application.DoEvents();
        Assert.That(treeView.Focused, Is.True, "Hosted TreeView must have focus before native key input.");
    }

    private static SelectionNavigationSnapshot CaptureUpDownNavigation(TreeView treeView)
    {
        var snapshot = new SelectionNavigationSnapshot();
        treeView.BeforeSelect += (_, _) => snapshot.BeforeSelect++;
        treeView.AfterSelect += (_, _) => snapshot.AfterSelect++;

        PostKey(treeView, Keys.Down);
        snapshot.AfterDownIndex = treeView.SelectedNode?.Index ?? -1;
        PostKey(treeView, Keys.Up);
        snapshot.AfterUpIndex = treeView.SelectedNode?.Index ?? -1;
        return snapshot;
    }

    private static LeftRightSnapshot CaptureLeftRightNavigation(TreeView treeView)
    {
        var root = treeView.Nodes[0];
        var child = root.Nodes[0];
        var snapshot = new LeftRightSnapshot();
        treeView.BeforeExpand += (_, _) => snapshot.BeforeExpand++;
        treeView.AfterExpand += (_, _) => snapshot.AfterExpand++;
        treeView.BeforeCollapse += (_, _) => snapshot.BeforeCollapse++;
        treeView.AfterCollapse += (_, _) => snapshot.AfterCollapse++;
        treeView.BeforeSelect += (_, _) => snapshot.BeforeSelect++;
        treeView.AfterSelect += (_, _) => snapshot.AfterSelect++;

        PostKey(treeView, Keys.Right);
        snapshot.RootExpandedAfterFirstRight = root.IsExpanded;
        PostKey(treeView, Keys.Right);
        snapshot.SelectedChildAfterSecondRight = ReferenceEquals(treeView.SelectedNode, child);
        PostKey(treeView, Keys.Left);
        snapshot.SelectedRootAfterFirstLeft = ReferenceEquals(treeView.SelectedNode, root);
        PostKey(treeView, Keys.Left);
        snapshot.RootCollapsedAfterSecondLeft = !root.IsExpanded;
        return snapshot;
    }

    private static PageNavigationSnapshot CaptureHomeEndPageNavigation(TreeView treeView)
    {
        var snapshot = new PageNavigationSnapshot();
        treeView.BeforeSelect += (_, _) => snapshot.BeforeSelect++;
        treeView.AfterSelect += (_, _) => snapshot.AfterSelect++;

        PostKey(treeView, Keys.Home);
        snapshot.AfterHomeIndex = treeView.SelectedNode?.Index ?? -1;
        PostKey(treeView, Keys.End);
        snapshot.AfterEndIndex = treeView.SelectedNode?.Index ?? -1;
        PostKey(treeView, Keys.PageUp);
        snapshot.AfterPageUpIndex = treeView.SelectedNode?.Index ?? -1;
        PostKey(treeView, Keys.PageDown);
        snapshot.AfterPageDownIndex = treeView.SelectedNode?.Index ?? -1;
        return snapshot;
    }

    private static ExpandKeySnapshot CaptureExpandCollapseKeys(TreeView treeView)
    {
        var root = treeView.Nodes[0];
        var child = root.Nodes[0];
        var snapshot = new ExpandKeySnapshot();
        treeView.BeforeExpand += (_, _) => snapshot.BeforeExpand++;
        treeView.AfterExpand += (_, _) => snapshot.AfterExpand++;
        treeView.BeforeCollapse += (_, _) => snapshot.BeforeCollapse++;
        treeView.AfterCollapse += (_, _) => snapshot.AfterCollapse++;

        PostKey(treeView, Keys.Add);
        snapshot.RootExpandedAfterAdd = root.IsExpanded;
        PostKey(treeView, Keys.Multiply);
        snapshot.RootExpandedAfterMultiply = root.IsExpanded;
        snapshot.ChildExpandedAfterMultiply = child.IsExpanded;
        PostKey(treeView, Keys.Subtract);
        snapshot.RootCollapsedAfterSubtract = !root.IsExpanded;
        return snapshot;
    }

    private static CheckSnapshot CaptureSpaceCheck(TreeView treeView)
    {
        var node = treeView.Nodes[0];
        var snapshot = new CheckSnapshot();
        treeView.BeforeCheck += (_, _) => snapshot.BeforeCheck++;
        treeView.AfterCheck += (_, _) => snapshot.AfterCheck++;

        PostKey(treeView, Keys.Space);
        snapshot.Checked = node.Checked;
        return snapshot;
    }

    private static LabelEditSnapshot CaptureProgrammaticLabelEdit(TreeView treeView)
    {
        var node = treeView.Nodes[0];
        var snapshot = new LabelEditSnapshot();
        treeView.BeforeLabelEdit += (_, _) => snapshot.BeforeLabelEdit++;
        treeView.AfterLabelEdit += (_, _) => snapshot.AfterLabelEdit++;

        node.BeginEdit();
        Application.DoEvents();
        var editor = GetEditControl(treeView);
        snapshot.CommitEditorCreated = editor != IntPtr.Zero;
        Assert.That(editor, Is.Not.EqualTo(IntPtr.Zero), "BeginEdit must create the native TreeView label editor.");
        Assert.That(SetWindowText(editor, "Committed label"), Is.True);
        node.EndEdit(cancel: false);
        Application.DoEvents();
        snapshot.TextAfterCommit = node.Text;

        node.BeginEdit();
        Application.DoEvents();
        editor = GetEditControl(treeView);
        snapshot.CancelEditorCreated = editor != IntPtr.Zero;
        Assert.That(editor, Is.Not.EqualTo(IntPtr.Zero), "The native editor must be reusable for a second edit.");
        Assert.That(SetWindowText(editor, "Cancelled label"), Is.True);
        node.EndEdit(cancel: true);
        Application.DoEvents();
        snapshot.TextAfterCancel = node.Text;
        return snapshot;
    }

    private static F2EditSnapshot CaptureF2LabelEdit(TreeView treeView, Func<bool> processF2)
    {
        var node = treeView.Nodes[0];
        var snapshot = new F2EditSnapshot { CommandHandled = processF2() };
        Application.DoEvents();
        var editor = GetEditControl(treeView);
        snapshot.EditorCreated = editor != IntPtr.Zero;
        if (editor != IntPtr.Zero)
        {
            node.EndEdit(cancel: true);
            Application.DoEvents();
        }

        snapshot.EditorClosedAfterCancel = GetEditControl(treeView) == IntPtr.Zero;
        return snapshot;
    }

    private static MouseEventSnapshot CaptureLabelClick(TreeView treeView)
    {
        var target = treeView.Nodes[2];
        treeView.SelectedNode = treeView.Nodes[1];
        Application.DoEvents();
        var snapshot = new MouseEventSnapshot();
        treeView.BeforeSelect += (_, _) => snapshot.BeforeSelect++;
        treeView.AfterSelect += (_, _) => snapshot.AfterSelect++;
        treeView.NodeMouseClick += (_, _) => snapshot.NodeMouseClick++;

        var point = GetLabelPoint(target);
        PostMouseMessage(treeView.Handle, WmLButtonDown, point, MkLButton);
        PostMouseMessage(treeView.Handle, WmLButtonUp, point, 0);
        Application.DoEvents();
        snapshot.SelectedTarget = ReferenceEquals(treeView.SelectedNode, target);
        return snapshot;
    }

    private static ItemDragSnapshot CaptureItemDrag(TreeView treeView)
    {
        var target = treeView.Nodes[1];
        treeView.SelectedNode = target;
        Application.DoEvents();
        var snapshot = new ItemDragSnapshot();
        treeView.ItemDrag += (_, args) =>
        {
            snapshot.ItemDrag++;
            snapshot.DraggedTarget = ReferenceEquals(args.Item, target);
            snapshot.Button = args.Button;
        };

        var start = GetLabelPoint(target);
        var dragDistance = Math.Max(SystemInformation.DragSize.Width + 4, 12);
        var move = new Point(
            Math.Min(treeView.ClientRectangle.Right - 2, start.X + dragDistance),
            start.Y);
        PostMouseMessage(treeView.Handle, WmLButtonDown, start, MkLButton);
        PostMouseMessage(treeView.Handle, WmMouseMove, move, MkLButton);
        PostMouseMessage(
            treeView.Handle,
            WmMouseMove,
            new Point(Math.Min(treeView.ClientRectangle.Right - 2, move.X + 4), move.Y),
            MkLButton);
        PostMouseMessage(treeView.Handle, WmLButtonUp, move, 0);
        Application.DoEvents();
        return snapshot;
    }

    private static Point GetLabelPoint(TreeNode node)
    {
        var bounds = node.Bounds;
        Assert.That(bounds.IsEmpty, Is.False, "Expected a visible native label rectangle.");
        return new Point(
            bounds.Left + Math.Max(1, bounds.Width / 2),
            bounds.Top + Math.Max(1, bounds.Height / 2));
    }

    private static IntPtr GetEditControl(TreeView treeView)
    {
        return SendMessage(treeView.Handle, TvmGetEditControl, IntPtr.Zero, IntPtr.Zero);
    }

    private static void PostKey(TreeView treeView, Keys key)
    {
        Assert.That(PostMessage(treeView.Handle, WmKeyDown, new IntPtr((int)key), IntPtr.Zero), Is.True);
        Assert.That(PostMessage(treeView.Handle, WmKeyUp, new IntPtr((int)key), IntPtr.Zero), Is.True);
        Application.DoEvents();
    }

    private static void PostMouseMessage(IntPtr handle, int message, Point point, int keyState)
    {
        var lParam = new IntPtr((point.Y << 16) | (point.X & 0xFFFF));
        Assert.That(PostMessage(handle, message, new IntPtr(keyState), lParam), Is.True);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowText(IntPtr hWnd, string lpString);

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

    private sealed class ProbeNativeTreeView : TreeView
    {
        internal bool ProcessCmdKeyForTesting(Keys keyData)
        {
            var message = Message.Create(Handle, WmKeyDown, IntPtr.Zero, IntPtr.Zero);
            return base.ProcessCmdKey(ref message, keyData);
        }
    }

    private sealed class ProbeBootstrapTreeView : BootstrapTreeView
    {
        internal bool ProcessCmdKeyForTesting(Keys keyData)
        {
            var message = Message.Create(Handle, WmKeyDown, IntPtr.Zero, IntPtr.Zero);
            return base.ProcessCmdKey(ref message, keyData);
        }

        internal bool ProcessDialogKeyForTesting(Keys keyData)
        {
            return base.ProcessDialogKey(keyData);
        }

        internal void RaiseNativeDragSequence()
        {
            var data = new DataObject(DataFormats.Text, "Tree node");
            var args = new DragEventArgs(
                data,
                0,
                10,
                10,
                DragDropEffects.Copy,
                DragDropEffects.Copy);
            base.OnDragEnter(args);
            base.OnDragOver(args);
            base.OnDragDrop(args);
        }
    }

    private sealed class SelectionNavigationSnapshot
    {
        internal int AfterDownIndex { get; set; }
        internal int AfterUpIndex { get; set; }
        internal int BeforeSelect { get; set; }
        internal int AfterSelect { get; set; }
    }

    private sealed class LeftRightSnapshot
    {
        internal bool RootExpandedAfterFirstRight { get; set; }
        internal bool SelectedChildAfterSecondRight { get; set; }
        internal bool SelectedRootAfterFirstLeft { get; set; }
        internal bool RootCollapsedAfterSecondLeft { get; set; }
        internal int BeforeExpand { get; set; }
        internal int AfterExpand { get; set; }
        internal int BeforeCollapse { get; set; }
        internal int AfterCollapse { get; set; }
        internal int BeforeSelect { get; set; }
        internal int AfterSelect { get; set; }
    }

    private sealed class PageNavigationSnapshot
    {
        internal int AfterHomeIndex { get; set; }
        internal int AfterEndIndex { get; set; }
        internal int AfterPageUpIndex { get; set; }
        internal int AfterPageDownIndex { get; set; }
        internal int BeforeSelect { get; set; }
        internal int AfterSelect { get; set; }
    }

    private sealed class ExpandKeySnapshot
    {
        internal bool RootExpandedAfterAdd { get; set; }
        internal bool RootExpandedAfterMultiply { get; set; }
        internal bool ChildExpandedAfterMultiply { get; set; }
        internal bool RootCollapsedAfterSubtract { get; set; }
        internal int BeforeExpand { get; set; }
        internal int AfterExpand { get; set; }
        internal int BeforeCollapse { get; set; }
        internal int AfterCollapse { get; set; }
    }

    private sealed class CheckSnapshot
    {
        internal bool Checked { get; set; }
        internal int BeforeCheck { get; set; }
        internal int AfterCheck { get; set; }
    }

    private sealed class LabelEditSnapshot
    {
        internal bool CommitEditorCreated { get; set; }
        internal string TextAfterCommit { get; set; } = string.Empty;
        internal bool CancelEditorCreated { get; set; }
        internal string TextAfterCancel { get; set; } = string.Empty;
        internal int BeforeLabelEdit { get; set; }
        internal int AfterLabelEdit { get; set; }
    }

    private sealed class F2EditSnapshot
    {
        internal bool CommandHandled { get; set; }
        internal bool EditorCreated { get; set; }
        internal bool EditorClosedAfterCancel { get; set; }
    }

    private sealed class MouseEventSnapshot
    {
        internal bool SelectedTarget { get; set; }
        internal int BeforeSelect { get; set; }
        internal int AfterSelect { get; set; }
        internal int NodeMouseClick { get; set; }
    }

    private sealed class ItemDragSnapshot
    {
        internal int ItemDrag { get; set; }
        internal bool DraggedTarget { get; set; }
        internal MouseButtons Button { get; set; }
    }
}