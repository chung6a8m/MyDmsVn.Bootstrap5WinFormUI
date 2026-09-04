using System;
using System.Drawing;
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
public sealed class BootstrapTreeViewDpiLifecycleTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
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
    public void DpiLifecycle_FrameworkOwnedItemHeightRemainsThemeDerivedAndNodeFontStaysCallerOwned()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        Assert.That(
            BootstrapTreeView.CalculateDefaultItemHeight(theme, 192),
            Is.GreaterThan(BootstrapTreeView.CalculateDefaultItemHeight(theme, 96)),
            "The framework default ItemHeight must scale with DPI.");

        var treeView = new ProbeBootstrapTreeView { Size = new Size(320, 180) };
        var root = new TreeNode("Root");
        var child = new TreeNode("Child");
        root.Nodes.Add(child);
        treeView.Nodes.Add(root);
        _ = treeView.Handle;
        root.Expand();
        treeView.SelectedNode = child;
        child.Checked = true;

        var nodeFont = new Font("Segoe UI", 22f, FontStyle.Bold);
        child.NodeFont = nodeFont;
        var expectedItemHeight = BootstrapTreeView.CalculateDefaultItemHeight(theme, treeView.DeviceDpi);

        treeView.RaiseDpiChangedAfterParent();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.ItemHeight, Is.EqualTo(expectedItemHeight));
            Assert.That(treeView.Nodes[0], Is.SameAs(root));
            Assert.That(treeView.SelectedNode, Is.SameAs(child));
            Assert.That(root.IsExpanded, Is.True);
            Assert.That(child.Checked, Is.True);
            Assert.That(child.NodeFont, Is.SameAs(nodeFont));
        }));

        treeView.Dispose();
        Assert.That(IsFontUsable(nodeFont), Is.True, "Caller-owned TreeNode.NodeFont must survive TreeView disposal.");
        nodeFont.Dispose();
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
    }
}
