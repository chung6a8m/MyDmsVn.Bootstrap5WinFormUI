using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRound9RegressionTests
{
    private const int TvFirst = 0x1100;
    private const int TvmGetItemHeight = TvFirst + 28;

    [Test]
    public void FrameworkOwnedOddRawItemHeight_IsNormalizedBeforeFirstHandleCreation()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            var theme = CreateThemeWithRawItemHeightParity(BootstrapThemeMode.Light, odd: true);
            BootstrapThemeManager.CurrentTheme = theme;

            using var treeView = new BootstrapTreeView
            {
                Size = new Size(260, 120),
                ShowLines = false,
            };
            treeView.Nodes.Add(new TreeNode("Native row height"));

            _ = treeView.Handle;
            Application.DoEvents();

            var nativeItemHeight = (int)SendMessage(treeView.Handle, TvmGetItemHeight, IntPtr.Zero, IntPtr.Zero);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(treeView.ItemHeight % 2, Is.EqualTo(0),
                    "Framework-owned defaults must be normalized to an even native-safe item height.");
                Assert.That(treeView.ItemHeight, Is.EqualTo(nativeItemHeight),
                    "The managed ItemHeight must match the native TreeView row height after first handle creation.");
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void FrameworkOwnedOddRawItemHeight_RuntimeThemeChangeDoesNotRecreateHandleOrCollapseNodes()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            var initialTheme = CreateThemeWithRawItemHeightParity(BootstrapThemeMode.Light, odd: false);
            BootstrapThemeManager.CurrentTheme = initialTheme;

            using var treeView = new BootstrapTreeView
            {
                Size = new Size(260, 140),
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
            };
            var root = new TreeNode("Root");
            root.Nodes.Add(new TreeNode("Child"));
            treeView.Nodes.Add(root);
            _ = treeView.Handle;
            root.Expand();
            Application.DoEvents();
            var handleBefore = treeView.Handle;

            var oddTheme = CreateThemeWithRawItemHeightParity(BootstrapThemeMode.Dark, odd: true);
            BootstrapThemeManager.CurrentTheme = oddTheme;
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(treeView.ItemHeight % 2, Is.EqualTo(0),
                    "Runtime framework-owned item heights must remain native-safe.");
                Assert.That(treeView.Handle, Is.EqualTo(handleBefore),
                    "A framework theme refresh must not recreate the native TreeView solely to enable TVS_NONEVENHEIGHT.");
                Assert.That(root.IsExpanded, Is.True,
                    "Framework-owned theme/DPI row-height refresh must not introduce handle-recreation expansion side effects.");
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void CheckBoxesWithSingleStateImage_UseCallerImageForUncheckedAndCheckedStates()
    {
        using var stateImages = CreateSolidImageList(new Size(16, 16), Color.Lime);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(320, 140),
            ItemHeight = 24,
            Indent = 19,
            CheckBoxes = true,
            StateImageList = stateImages,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
        };
        var uncheckedNode = new TreeNode("Unchecked") { Checked = false };
        var checkedNode = new TreeNode("Checked") { Checked = true };
        treeView.Nodes.Add(uncheckedNode);
        treeView.Nodes.Add(checkedNode);
        _ = treeView.Handle;
        Application.DoEvents();

        var uncheckedHit = GetNativeHitBounds(treeView, uncheckedNode, TreeViewHitTestLocations.StateImage);
        var checkedHit = GetNativeHitBounds(treeView, checkedNode, TreeViewHitTestLocations.StateImage);
        using var uncheckedBitmap = RenderNode(treeView, uncheckedNode, Color.Magenta);
        using var checkedBitmap = RenderNode(treeView, checkedNode, Color.Magenta);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ContainsColor(uncheckedBitmap, uncheckedHit, Color.Lime), Is.True,
                "The single caller state image must render for the native unchecked state.");
            Assert.That(ContainsColor(checkedBitmap, checkedHit, Color.Lime), Is.True,
                "Native TreeView duplicates the first caller state image in its 1-based state list, so the checked state must not fall back to framework checkbox art.");
        }));
    }

    private static BootstrapTheme CreateThemeWithRawItemHeightParity(BootstrapThemeMode mode, bool odd)
    {
        const int dpi = DpiScaler.DefaultDpi;
        for (var quarterPoints = 28; quarterPoints <= 80; quarterPoints++)
        {
            var bodySize = quarterPoints / 4f;
            var theme = CreateTheme(mode, bodySize);
            using var font = new Font(
                theme.Typography.Body.FontFamilyName,
                theme.Typography.Body.SizeInPoints,
                theme.Typography.Body.Style);
            var rawHeight = (int)Math.Ceiling(font.GetHeight(dpi)) +
                DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
            if ((rawHeight % 2 != 0) == odd)
            {
                return theme;
            }
        }

        Assert.Fail("Expected to find a Segoe UI body size with the requested raw item-height parity.");
        return BootstrapTheme.CreateDefault(mode);
    }

    private static BootstrapTheme CreateTheme(BootstrapThemeMode mode, float bodySize)
    {
        var defaults = BootstrapThemeTypography.Default;
        var typography = new BootstrapThemeTypography(
            new BootstrapFontToken("Segoe UI", bodySize),
            defaults.BodySmall,
            defaults.Label,
            defaults.HeadingSmall,
            defaults.HeadingMedium);
        return new BootstrapTheme(
            mode,
            BootstrapThemeColors.CreateDefault(mode),
            BootstrapThemeMetrics.Default,
            typography);
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

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node, Color background)
    {
        var rowBounds = Rectangle.Intersect(
            treeView.ClientRectangle,
            new Rectangle(treeView.ClientRectangle.Left, node.Bounds.Top, treeView.ClientRectangle.Width, treeView.ItemHeight));
        var bitmap = new Bitmap(Math.Max(1, treeView.ClientSize.Width), Math.Max(1, treeView.ClientSize.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(background);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, node.Bounds, (TreeNodeStates)0);
        return bitmap;
    }

    private static bool ContainsColor(Bitmap bitmap, Rectangle bounds, Color expected)
    {
        var clipped = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), bounds);
        for (var y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (var x = clipped.Left; x < clipped.Right; x++)
            {
                var actual = bitmap.GetPixel(x, y);
                if (Math.Abs(actual.R - expected.R) <= 8 &&
                    Math.Abs(actual.G - expected.G) <= 8 &&
                    Math.Abs(actual.B - expected.B) <= 8)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
