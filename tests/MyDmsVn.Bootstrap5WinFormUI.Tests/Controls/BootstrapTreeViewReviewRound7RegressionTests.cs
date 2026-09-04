using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRound7RegressionTests
{
    private const int WmHScroll = 0x0114;
    private const int SbLineRight = 1;

    [Test]
    public void ThemeCacheReset_PartiallyScrolledStateImageKeepsFullNativeDisplayHeight()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var stateImages = CreateSolidImageList(new Size(16, 16), Color.Lime);
            using var form = CreateHostForm(new Size(180, 120));
            using var treeView = new BootstrapTreeView
            {
                Dock = DockStyle.Fill,
                ItemHeight = 24,
                ShowLines = false,
                ShowPlusMinus = false,
                ShowRootLines = false,
                Scrollable = true,
                StateImageList = stateImages,
            };
            var node = new TreeNode(
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                StateImageIndex = 0,
            };
            treeView.Nodes.Add(node);
            form.Controls.Add(treeView);
            form.Show();
            Application.DoEvents();

            var fullStateHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
            using (var primed = RenderNode(treeView, node, Color.Magenta))
            {
                Assert.That(GetVerticalColorSpan(primed, GetRowBounds(treeView, node), Color.Lime), Is.GreaterThan(0));
            }

            var partialStateHit = ScrollRightUntilPartialStateImage(treeView, node, fullStateHit.Width);
            Assert.That(partialStateHit.Width, Is.LessThan(fullStateHit.Width));

            var nextMode = originalTheme.Mode == BootstrapThemeMode.Light
                ? BootstrapThemeMode.Dark
                : BootstrapThemeMode.Light;
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(nextMode, originalTheme.ReducedMotion);
            Application.DoEvents();

            var afterThemeHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
            Assert.That(afterThemeHit.Width, Is.LessThan(fullStateHit.Width),
                "The native state-image hit target must still be partially clipped so the cache-reset path is exercised.");

            using var bitmap = RenderNode(treeView, node, Color.Magenta);
            var paintedHeight = GetVerticalColorSpan(bitmap, GetRowBounds(treeView, node), Color.Lime);

            Assert.That(
                paintedHeight,
                Is.GreaterThanOrEqualTo(fullStateHit.Width - 1),
                "Resetting the native-state-image slot cache while horizontally scrolled must not turn the visible hit fragment into a smaller square destination.");
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void OwnerDraw_OversizedNodeImageDoesNotPaintIntoAdjacentRowSpace()
    {
        using var images = CreateSolidImageList(new Size(32, 32), Color.Lime);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(180, 70),
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            ImageList = images,
        };
        treeView.ItemHeight = 20;
        var node = new TreeNode("Oversized image")
        {
            ImageIndex = 0,
            SelectedImageIndex = 0,
        };
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var rowBounds = GetRowBounds(treeView, node);
        using var bitmap = RenderNode(treeView, node, Color.Magenta);
        var belowRow = Rectangle.Intersect(
            new Rectangle(Point.Empty, bitmap.Size),
            new Rectangle(
                treeView.ClientRectangle.Left,
                rowBounds.Bottom,
                treeView.ClientRectangle.Width,
                Math.Max(0, Math.Min(12, bitmap.Height - rowBounds.Bottom))));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsColor(bitmap, rowBounds, Color.Lime), Is.True,
                "The node image must remain visible inside its own row.");
            Assert.That(ContainsColor(bitmap, belowRow, Color.Lime), Is.False,
                "A caller ImageList image taller than ItemHeight must not paint into the following row's visual space.");
        }));
    }

    [Test]
    public void OwnerDraw_OffscreenCurrentConnectorDoesNotSnapToEdgeAndVisibleRtlAncestorStillDraws()
    {
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(120, 50),
            ItemHeight = 24,
            Indent = 19,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true,
        };
        var root = new TreeNode("Root");
        var parent = new TreeNode("Parent");
        var target = new TreeNode(string.Empty);
        parent.Nodes.Add(target);
        root.Nodes.Add(parent);
        treeView.Nodes.Add(root);
        treeView.Nodes.Add(new TreeNode("Root sibling"));
        _ = treeView.Handle;
        root.ExpandAll();

        var rowBounds = new Rectangle(
            treeView.ClientRectangle.Left,
            treeView.ClientRectangle.Top,
            treeView.ClientRectangle.Width,
            Math.Min(treeView.ItemHeight, treeView.ClientRectangle.Height));
        var shiftedLabel = new Rectangle(
            treeView.ClientRectangle.Left - 10,
            rowBounds.Top,
            60,
            rowBounds.Height);
        var layout = BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            treeView.ClientRectangle,
            rowBounds,
            shiftedLabel,
            target.Level,
            treeView.DeviceDpi,
            rightToLeft: true,
            effectiveFullRowSelection: false,
            hasExpander: false,
            hasStateImage: false,
            nativeStateImageSlotWidth: 0,
            hasNodeImage: false,
            nodeImageSize: Size.Empty));
        var visibleRootAncestorX = BootstrapTreeViewLayout.CalculateAncestorConnectorX(
            layout.ExpanderAnchorX,
            target.Level,
            ancestorLevel: 0,
            treeView.Indent,
            rightToLeft: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.ExpanderSlotBounds.IsEmpty, Is.True,
                "The current node expander slot must be fully outside the viewport for this regression.");
            Assert.That(visibleRootAncestorX, Is.GreaterThanOrEqualTo(rowBounds.Left));
            Assert.That(visibleRootAncestorX, Is.LessThan(rowBounds.Right));
        }));

        var theme = BootstrapThemeManager.CurrentTheme;
        using var bitmap = RenderNode(treeView, target, rowBounds, shiftedLabel, theme.Colors.Surface);
        var ancestorColumn = new Rectangle(visibleRootAncestorX, rowBounds.Top, 1, rowBounds.Height);
        var leftEdgeColumn = new Rectangle(rowBounds.Left, rowBounds.Top, 1, rowBounds.Height);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                ContainsColor(bitmap, ancestorColumn, theme.Colors.Border),
                Is.True,
                "A visible RTL ancestor continuation must still render even when the current node's expander slot is fully off-screen.");
            Assert.That(
                ContainsColor(bitmap, leftEdgeColumn, theme.Colors.Border),
                Is.False,
                "An off-screen connector x-coordinate must be clipped away, not clamped into a synthetic vertical line at the viewport edge.");
        }));
    }

    private static Form CreateHostForm(Size clientSize)
    {
        return new Form
        {
            ClientSize = clientSize,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-2000, -2000),
        };
    }

    private static Rectangle ScrollRightUntilPartialStateImage(
        BootstrapTreeView treeView,
        TreeNode node,
        int fullWidth)
    {
        for (var attempt = 0; attempt < 96; attempt++)
        {
            var current = GetNativeHitBoundsOrEmpty(treeView, node, TreeViewHitTestLocations.StateImage);
            if (!current.IsEmpty && current.Width > 0 && current.Width < fullWidth)
            {
                return current;
            }

            SendMessage(treeView.Handle, WmHScroll, new IntPtr(SbLineRight), IntPtr.Zero);
            Application.DoEvents();
        }

        Assert.Fail("Expected the native StateImage hit region to become partially visible during horizontal scrolling.");
        return Rectangle.Empty;
    }

    private static Rectangle GetNativeHitBounds(
        TreeView treeView,
        TreeNode expectedNode,
        TreeViewHitTestLocations expectedLocation)
    {
        var result = GetNativeHitBoundsOrEmpty(treeView, expectedNode, expectedLocation);
        Assert.That(result.IsEmpty, Is.False, $"Expected native {expectedLocation} hit geometry.");
        return result;
    }

    private static Rectangle GetNativeHitBoundsOrEmpty(
        TreeView treeView,
        TreeNode expectedNode,
        TreeViewHitTestLocations expectedLocation)
    {
        var bounds = expectedNode.Bounds;
        if (bounds.IsEmpty)
        {
            return Rectangle.Empty;
        }

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

        return first < 0
            ? Rectangle.Empty
            : Rectangle.FromLTRB(first, bounds.Top, last + 1, bounds.Bottom);
    }

    private static Rectangle GetRowBounds(TreeView treeView, TreeNode node)
    {
        return Rectangle.Intersect(
            treeView.ClientRectangle,
            new Rectangle(
                treeView.ClientRectangle.Left,
                node.Bounds.Top,
                treeView.ClientRectangle.Width,
                treeView.ItemHeight));
    }

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node, Color background)
    {
        return RenderNode(treeView, node, GetRowBounds(treeView, node), node.Bounds, background);
    }

    private static Bitmap RenderNode(
        BootstrapTreeView treeView,
        TreeNode node,
        Rectangle rowBounds,
        Rectangle nativeLabelBounds,
        Color background)
    {
        var bitmap = new Bitmap(Math.Max(1, treeView.ClientSize.Width), Math.Max(1, treeView.ClientSize.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(background);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, nativeLabelBounds, (TreeNodeStates)0);
        return bitmap;
    }

    private static int GetVerticalColorSpan(Bitmap bitmap, Rectangle bounds, Color expected)
    {
        var clipped = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), bounds);
        var first = -1;
        var last = -1;
        for (var y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (var x = clipped.Left; x < clipped.Right; x++)
            {
                if (!IsColorNear(bitmap.GetPixel(x, y), expected))
                {
                    continue;
                }

                if (first < 0)
                {
                    first = y;
                }

                last = y;
            }
        }

        return first < 0 ? 0 : last - first + 1;
    }

    private static bool ContainsColor(Bitmap bitmap, Rectangle bounds, Color expected)
    {
        var clipped = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), bounds);
        for (var y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (var x = clipped.Left; x < clipped.Right; x++)
            {
                if (IsColorNear(bitmap.GetPixel(x, y), expected))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsColorNear(Color actual, Color expected)
    {
        return Math.Abs(actual.R - expected.R) <= 8 &&
               Math.Abs(actual.G - expected.G) <= 8 &&
               Math.Abs(actual.B - expected.B) <= 8;
    }

    private static ImageList CreateSolidImageList(Size imageSize, Color color)
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
            graphics.Clear(color);
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

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
