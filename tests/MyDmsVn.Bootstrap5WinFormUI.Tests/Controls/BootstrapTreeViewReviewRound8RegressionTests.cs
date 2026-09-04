using System;
using System.Collections.Generic;
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
public sealed class BootstrapTreeViewReviewRound8RegressionTests
{
    private const int TvFirst = 0x1100;
    private const int TvmSetImageList = TvFirst + 9;
    private const int TvsilState = 2;
    private const int WmHScroll = 0x0114;
    private const int SbLineRight = 1;

    [Test]
    public void NativeStateImageFallback_UsesActualNativeImageListSizeAfterCacheReset()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var sourceStateImages = CreateSolidImageList(new Size(16, 16), Color.Lime);
            using var nativeStateImages = CreateSolidImageList(new Size(24, 24), Color.Blue);
            using var form = CreateHostForm(new Size(180, 120));
            using var treeView = new BootstrapTreeView
            {
                Dock = DockStyle.Fill,
                ItemHeight = 32,
                ShowLines = false,
                ShowPlusMinus = false,
                ShowRootLines = false,
                Scrollable = true,
                StateImageList = sourceStateImages,
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

            _ = SendMessage(
                treeView.Handle,
                TvmSetImageList,
                new IntPtr(TvsilState),
                nativeStateImages.Handle);
            Application.DoEvents();

            var fullStateHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
            Assert.That(
                fullStateHit.Width,
                Is.GreaterThan(sourceStateImages.ImageSize.Width),
                "The test must establish native state-image geometry that differs from the managed source list/default 16px fallback.");

            int primedPaintedHeight;
            using (var primed = RenderNode(treeView, node, Color.Magenta))
            {
                primedPaintedHeight = GetVerticalColorSpan(primed, GetRowBounds(treeView, node), Color.Lime);
                Assert.That(
                    primedPaintedHeight,
                    Is.GreaterThan(sourceStateImages.ImageSize.Height),
                    "A fully visible native state-image hit must prime owner draw with the enlarged native display geometry.");
            }

            var partialStateHit = ScrollRightUntilNarrowStateImage(treeView, node, fullStateHit.Width);
            Assert.That(partialStateHit.Width, Is.LessThan(16));

            var nextMode = originalTheme.Mode == BootstrapThemeMode.Light
                ? BootstrapThemeMode.Dark
                : BootstrapThemeMode.Light;
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(nextMode, originalTheme.ReducedMotion);
            Application.DoEvents();

            var afterResetHit = GetNativeHitBounds(treeView, node, TreeViewHitTestLocations.StateImage);
            Assert.That(afterResetHit.Width, Is.LessThan(16));

            using var bitmap = RenderNode(treeView, node, Color.Magenta);
            var paintedHeight = GetVerticalColorSpan(bitmap, GetRowBounds(treeView, node), Color.Lime);

            Assert.That(
                paintedHeight,
                Is.GreaterThanOrEqualTo(primedPaintedHeight - 1),
                "After the measurement cache is reset while the state image is partially clipped, owner draw must retain the actual native state image-list size rather than guessing from the current DeviceDpi.");
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void StateImageKey_AboveNativeMaximumDoesNotRenderFrameworkStateImage()
    {
        using var stateImages = CreateKeyedStateImageList();
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(320, 120),
            ItemHeight = 24,
            Indent = 32,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = true,
            StateImageList = stateImages,
        };
        var root = new TreeNode("Root");
        var node = new TreeNode("State image key above native maximum")
        {
            StateImageKey = "state-15",
        };
        root.Nodes.Add(node);
        treeView.Nodes.Add(root);
        _ = treeView.Handle;
        root.Expand();
        Application.DoEvents();

        Assert.That(node.Bounds.Left, Is.GreaterThan(16), "The child indentation must leave a visible framework state-image slot for this regression.");
        var nativeStateHit = GetNativeHitBoundsOrEmpty(treeView, node, TreeViewHitTestLocations.StateImage);
        using var bitmap = RenderNode(treeView, node, Color.Magenta);
        var rowBounds = GetRowBounds(treeView, node);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                nativeStateHit.IsEmpty,
                Is.True,
                "Native TreeView cannot represent StateImageKey values resolving above index 14 in TVIS_STATEIMAGEMASK.");
            Assert.That(
                ContainsColor(bitmap, rowBounds, Color.Lime),
                Is.False,
                "Owner draw must not render a state image that native TreeView cannot represent or hit-test.");
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

    private static Rectangle ScrollRightUntilNarrowStateImage(
        BootstrapTreeView treeView,
        TreeNode node,
        int fullWidth)
    {
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var current = GetNativeHitBoundsOrEmpty(treeView, node, TreeViewHitTestLocations.StateImage);
            if (!current.IsEmpty && current.Width > 0 && current.Width < Math.Min(16, fullWidth))
            {
                return current;
            }

            _ = SendMessage(treeView.Handle, WmHScroll, new IntPtr(SbLineRight), IntPtr.Zero);
            Application.DoEvents();
        }

        Assert.Fail("Expected the native StateImage hit region to become a narrow partially visible fragment during horizontal scrolling.");
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
        var rowBounds = GetRowBounds(treeView, node);
        var bitmap = new Bitmap(Math.Max(1, treeView.ClientSize.Width), Math.Max(1, treeView.ClientSize.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(background);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, node.Bounds, (TreeNodeStates)0);
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
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(color);
        }

        imageList.Images.Add(bitmap);
        _ = imageList.Handle;
        return imageList;
    }

    private static ImageList CreateKeyedStateImageList()
    {
        var imageList = new ImageList
        {
            ImageSize = new Size(16, 16),
            ColorDepth = ColorDepth.Depth32Bit,
        };
        var sourceImages = new List<Bitmap>();
        for (var index = 0; index < 16; index++)
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(index == 15 ? Color.Lime : Color.Red);
            }

            sourceImages.Add(bitmap);
            imageList.Images.Add($"state-{index}", bitmap);
        }

        _ = imageList.Handle;
        foreach (var bitmap in sourceImages)
        {
            bitmap.Dispose();
        }

        return imageList;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
