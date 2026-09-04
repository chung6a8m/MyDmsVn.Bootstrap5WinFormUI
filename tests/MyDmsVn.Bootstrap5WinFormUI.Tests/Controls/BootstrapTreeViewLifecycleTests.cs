using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTreeViewLifecycleTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = CreateTheme(BootstrapThemeMode.Light, 9f);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void Constructor_AppliesThemeSurfaceFontItemHeightAndSubscribesOnce()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var theme = BootstrapThemeManager.CurrentTheme;

        using var treeView = new BootstrapTreeView();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));
            Assert.That(treeView.BackColor, Is.EqualTo(theme.Colors.Surface));
            Assert.That(treeView.ForeColor, Is.EqualTo(theme.Colors.Text));
            Assert.That(treeView.Font.Name, Is.EqualTo(theme.Typography.Body.FontFamilyName).IgnoreCase);
            Assert.That(treeView.Font.SizeInPoints, Is.EqualTo(theme.Typography.Body.SizeInPoints).Within(0.01f));
            Assert.That(treeView.Font.Style, Is.EqualTo(theme.Typography.Body.Style));
            Assert.That(treeView.ItemHeight, Is.EqualTo(BootstrapTreeView.CalculateDefaultItemHeight(theme, treeView.DeviceDpi)));
        }));
    }

    [Test]
    public void RuntimeThemeSwitch_UpdatesControlAndRenderedPaletteWithoutReplacingNativeTreeState()
    {
        using var treeView = CreateTree();
        var root = treeView.Nodes[0];
        var selected = root.Nodes[0];
        treeView.SelectedNode = selected;
        root.Expand();
        selected.Checked = true;
        var nodes = treeView.Nodes;

        var dark = CreateTheme(BootstrapThemeMode.Dark, 11f);
        BootstrapThemeManager.CurrentTheme = dark;

        using var rendered = RenderNode(treeView, selected, TreeNodeStates.Selected);
        var selectedColor = BootstrapVariantColorResolver.Resolve(dark.Colors, treeView.Variant);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.BackColor, Is.EqualTo(dark.Colors.Surface));
            Assert.That(treeView.ForeColor, Is.EqualTo(dark.Colors.Text));
            Assert.That(treeView.Font.SizeInPoints, Is.EqualTo(11f).Within(0.01f));
            Assert.That(treeView.ItemHeight, Is.EqualTo(BootstrapTreeView.CalculateDefaultItemHeight(dark, treeView.DeviceDpi)));
            Assert.That(treeView.Nodes, Is.SameAs(nodes));
            Assert.That(treeView.Nodes[0], Is.SameAs(root));
            Assert.That(treeView.SelectedNode, Is.SameAs(selected));
            Assert.That(selected.Checked, Is.True);
            Assert.That(root.IsExpanded, Is.True);
            Assert.That(ContainsColor(rendered, selectedColor), Is.True);
        }));
    }

    [Test]
    public void CallerAssignedFontAndItemHeight_RemainCallerOwnedAcrossThemeChanges()
    {
        using var treeView = new BootstrapTreeView();
        using var callerFont = new Font("Segoe UI", 13f, FontStyle.Italic);
        treeView.Font = callerFont;
        treeView.ItemHeight = treeView.ItemHeight + 11;
        var callerItemHeight = treeView.ItemHeight;
        var callerIndent = treeView.Indent;

        BootstrapThemeManager.CurrentTheme = CreateTheme(BootstrapThemeMode.Dark, 15f);
        BootstrapThemeManager.CurrentTheme = CreateTheme(BootstrapThemeMode.Light, 8f);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.Font, Is.SameAs(callerFont));
            Assert.That(treeView.ItemHeight, Is.EqualTo(callerItemHeight));
            Assert.That(treeView.Indent, Is.EqualTo(callerIndent));
            Assert.That(IsFontUsable(callerFont), Is.True);
        }));
    }

    [Test]
    public void FrameworkOwnedFontAndItemHeight_RefreshAndDisposeReplacedFont()
    {
        BootstrapThemeManager.CurrentTheme = CreateTheme(BootstrapThemeMode.Light, 9f);
        using var treeView = new BootstrapTreeView();
        var previousFont = treeView.Font;
        Assert.That(IsFontUsable(previousFont), Is.True);

        var nextTheme = CreateTheme(BootstrapThemeMode.Dark, 12f);
        BootstrapThemeManager.CurrentTheme = nextTheme;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.Font, Is.Not.SameAs(previousFont));
            Assert.That(treeView.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
            Assert.That(treeView.ItemHeight, Is.EqualTo(BootstrapTreeView.CalculateDefaultItemHeight(nextTheme, treeView.DeviceDpi)));
            Assert.That(IsFontUsable(previousFont), Is.False, "Replaced framework-owned font must be disposed.");
        }));
    }

    [Test]
    public void DpiLifecycle_PreservesCallerItemHeightNativeStateAndCallerNodeFont()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        Assert.That(
            BootstrapTreeView.CalculateDefaultItemHeight(theme, 192),
            Is.GreaterThan(BootstrapTreeView.CalculateDefaultItemHeight(theme, 96)));

        var treeView = CreateTree();
        var root = treeView.Nodes[0];
        var child = root.Nodes[0];
        var callerItemHeight = treeView.ItemHeight + 9;
        treeView.ItemHeight = callerItemHeight;
        var nodeFont = new Font("Segoe UI", 18f, FontStyle.Bold);
        child.NodeFont = nodeFont;
        treeView.SelectedNode = child;
        child.Checked = true;
        root.Expand();

        treeView.RaiseDpiChangedAfterParent();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.ItemHeight, Is.EqualTo(callerItemHeight));
            Assert.That(treeView.Nodes[0], Is.SameAs(root));
            Assert.That(treeView.SelectedNode, Is.SameAs(child));
            Assert.That(child.Checked, Is.True);
            Assert.That(root.IsExpanded, Is.True);
            Assert.That(child.NodeFont, Is.SameAs(nodeFont));
        }));

        treeView.Dispose();
        Assert.That(IsFontUsable(nodeFont), Is.True, "Caller-owned TreeNode.NodeFont must survive control disposal.");
        nodeFont.Dispose();
    }

    [TestCase("CheckBoxes")]
    [TestCase("Scrollable")]
    [TestCase("ImageIndex")]
    [TestCase("SelectedImageIndex")]
    public void RuntimeNativeStateChanges_MatchNativeHandleAndStateWhileKeepingLifecycleUsable(string propertyName)
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        using var imageList = CreateImageList();
        using var native = CreateNativeTree(new TreeView(), imageList);
        using var bootstrap = CreateNativeTree(new ProbeBootstrapTreeView(), imageList);
        var nativeHandleBefore = native.Handle;
        var bootstrapHandleBefore = bootstrap.Handle;

        bootstrap.RaiseMouseMove(GetLabelPoint(bootstrap.Nodes[0]));
        ApplyNativeRuntimeChange(native, propertyName);
        ApplyNativeRuntimeChange(bootstrap, propertyName);
        Application.DoEvents();

        var nativeHandleRecreated = native.Handle != nativeHandleBefore;
        var bootstrapHandleRecreated = bootstrap.Handle != bootstrapHandleBefore;
        var dark = CreateTheme(BootstrapThemeMode.Dark, 10f);
        BootstrapThemeManager.CurrentTheme = dark;
        using var rendered = RenderNode(bootstrap, bootstrap.Nodes[0], (TreeNodeStates)0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrapHandleRecreated, Is.EqualTo(nativeHandleRecreated),
                "BootstrapTreeView must follow native handle-recreation semantics for " + propertyName + ".");
            Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));
            Assert.That(bootstrap.BackColor, Is.EqualTo(dark.Colors.Surface));
            Assert.That(bootstrap.Font.SizeInPoints, Is.EqualTo(10f).Within(0.01f));
            Assert.That(bootstrap.ItemHeight, Is.EqualTo(BootstrapTreeView.CalculateDefaultItemHeight(dark, bootstrap.DeviceDpi)));
            Assert.That(bootstrap.Nodes[0].IsExpanded, Is.EqualTo(native.Nodes[0].IsExpanded));
            Assert.That(bootstrap.Nodes[0].Nodes[0].IsExpanded, Is.EqualTo(native.Nodes[0].Nodes[0].IsExpanded));
            Assert.That(bootstrap.SelectedNode?.FullPath, Is.EqualTo(native.SelectedNode?.FullPath));
            Assert.That(ContainsColor(rendered, dark.Colors.Hover), Is.True,
                "Framework hover bookkeeping should remain valid after native runtime state/handle changes.");
        }));
    }

    [Test]
    public void Dispose_DetachesThemeSubscriptionDisposesFrameworkFontAndLeavesCallerFontAlone()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var frameworkTree = new BootstrapTreeView();
        var frameworkFont = frameworkTree.Font;
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));

        frameworkTree.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
            Assert.That(IsFontUsable(frameworkFont), Is.False, "Framework-owned font must be disposed with the control.");
        }));

        var callerFont = new Font("Segoe UI", 14f, FontStyle.Bold);
        var callerTree = new BootstrapTreeView { Font = callerFont };
        callerTree.Dispose();
        Assert.That(IsFontUsable(callerFont), Is.True, "Caller-owned control Font must survive control disposal.");
        callerFont.Dispose();

        Assert.DoesNotThrow((Action)(() =>
            BootstrapThemeManager.CurrentTheme = CreateTheme(BootstrapThemeMode.Dark, 10f)));
    }

    private static ProbeBootstrapTreeView CreateTree()
    {
        var treeView = new ProbeBootstrapTreeView
        {
            Size = new Size(320, 180),
            ShowLines = false,
            ShowPlusMinus = true,
            ShowRootLines = true,
        };
        var root = new TreeNode("Root");
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        _ = treeView.Handle;
        root.Expand();
        return treeView;
    }

    private static T CreateNativeTree<T>(T treeView, ImageList imageList)
        where T : TreeView
    {
        treeView.Size = new Size(320, 180);
        treeView.ImageList = imageList;
        treeView.ShowLines = false;
        treeView.ShowPlusMinus = true;
        treeView.ShowRootLines = true;
        var root = new TreeNode("Root") { ImageIndex = 0, SelectedImageIndex = 0 };
        var child = new TreeNode("Child") { ImageIndex = 0, SelectedImageIndex = 0 };
        child.Nodes.Add(new TreeNode("Leaf") { ImageIndex = 0, SelectedImageIndex = 0 });
        root.Nodes.Add(child);
        treeView.Nodes.Add(root);
        _ = treeView.Handle;
        root.ExpandAll();
        treeView.SelectedNode = child.Nodes[0];
        Application.DoEvents();
        return treeView;
    }

    private static void ApplyNativeRuntimeChange(TreeView treeView, string propertyName)
    {
        switch (propertyName)
        {
            case "CheckBoxes":
                treeView.CheckBoxes = true;
                break;
            case "Scrollable":
                treeView.Scrollable = !treeView.Scrollable;
                break;
            case "ImageIndex":
                treeView.ImageIndex = 1;
                break;
            case "SelectedImageIndex":
                treeView.SelectedImageIndex = 1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(propertyName));
        }
    }

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node, TreeNodeStates state)
    {
        var rowBounds = Rectangle.Intersect(
            treeView.ClientRectangle,
            new Rectangle(treeView.ClientRectangle.Left, node.Bounds.Top, treeView.ClientRectangle.Width, treeView.ItemHeight));
        var bitmap = new Bitmap(Math.Max(1, treeView.ClientSize.Width), Math.Max(1, treeView.ClientSize.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, node.Bounds, state);
        return bitmap;
    }

    private static bool ContainsColor(Bitmap bitmap, Color expected)
    {
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == expected.ToArgb())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Point GetLabelPoint(TreeNode node)
    {
        return new Point(
            node.Bounds.Left + Math.Max(1, node.Bounds.Width / 2),
            node.Bounds.Top + Math.Max(1, node.Bounds.Height / 2));
    }

    private static ImageList CreateImageList()
    {
        var imageList = new ImageList { ImageSize = new Size(16, 16) };
        imageList.Images.Add(new Bitmap(16, 16));
        imageList.Images.Add(new Bitmap(16, 16));
        _ = imageList.Handle;
        return imageList;
    }

    private static BootstrapTheme CreateTheme(BootstrapThemeMode mode, float bodySize)
    {
        var defaultTypography = BootstrapThemeTypography.Default;
        var typography = new BootstrapThemeTypography(
            new BootstrapFontToken("Segoe UI", bodySize),
            defaultTypography.BodySmall,
            defaultTypography.Label,
            defaultTypography.HeadingSmall,
            defaultTypography.HeadingMedium);
        return new BootstrapTheme(
            mode,
            BootstrapThemeColors.CreateDefault(mode),
            BootstrapThemeMetrics.Default,
            typography);
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static bool IsFontUsable(Font font)
    {
        IntPtr hfont = IntPtr.Zero;
        try
        {
            hfont = font.ToHfont();
            return hfont != IntPtr.Zero;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (ExternalException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        finally
        {
            if (hfont != IntPtr.Zero)
            {
                DeleteObject(hfont);
            }
        }
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private sealed class ProbeBootstrapTreeView : BootstrapTreeView
    {
        internal void RaiseDpiChangedAfterParent()
        {
            base.OnDpiChangedAfterParent(EventArgs.Empty);
        }

        internal void RaiseMouseMove(Point point)
        {
            base.OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
        }
    }
}
