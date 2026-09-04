using System;
using System.Drawing;
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
public sealed class BootstrapTreeViewOwnerDrawTests
{
    private BootstrapTheme _originalTheme = null!;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
    }

    [TearDown]
    public void TearDown()
    {
        BootstrapThemeManager.CurrentTheme = _originalTheme;
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void RenderNodeForTesting_RepeatedOwnerDrawCompletesForLightAndDark(BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var treeView = new BootstrapTreeView { Size = new Size(240, 60) };
        var node = new TreeNode("Owner drawn node");
        treeView.Nodes.Add(node);
        var rowBounds = new Rectangle(0, 0, 240, 24);
        var labelBounds = new Rectangle(48, 0, 150, 24);

        using var bitmap = new Bitmap(240, 60);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);

        Assert.DoesNotThrow((Action)(() =>
        {
            for (var index = 0; index < 32; index++)
            {
                treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, (TreeNodeStates)0);
            }
        }));

        Assert.That(
            bitmap.GetPixel(labelBounds.Right - 2, labelBounds.Top + 2).ToArgb(),
            Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Surface.ToArgb()));
    }

    [Test]
    public void DrawNodeEvent_IsRaisedExactlyOnce_AndDrawDefaultCannotReplaceFrameworkRendering()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(220, 50),
            Variant = BootstrapVariant.Danger,
            HideSelection = false,
        };
        var node = new TreeNode("Selected node");
        treeView.Nodes.Add(node);
        var eventCount = 0;
        DrawTreeNodeEventArgs? observed = null;
        treeView.DrawNode += (_, args) =>
        {
            eventCount++;
            args.DrawDefault = true;
            observed = args;
        };

        using var bitmap = new Bitmap(220, 50);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        var rowBounds = new Rectangle(0, 0, 220, 24);
        var labelBounds = new Rectangle(44, 0, 140, 24);

        treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, TreeNodeStates.Selected);

        var expected = BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, BootstrapVariant.Danger);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed!.DrawDefault, Is.False);
            Assert.That(bitmap.GetPixel(labelBounds.Right - 2, labelBounds.Top + 2).ToArgb(), Is.EqualTo(expected.ToArgb()));
        }));
    }

    [Test]
    public void SelectedFullRowBackground_IsUsedOnlyWhenNativeFullRowSelectionIsEffective()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var selectedColor = BootstrapThemeManager.CurrentTheme.Colors.Primary;
        var rowBounds = new Rectangle(0, 0, 220, 24);
        var labelBounds = new Rectangle(50, 0, 100, 24);

        using var effective = new BootstrapTreeView
        {
            Size = new Size(220, 50),
            FullRowSelect = true,
            ShowLines = false,
            HideSelection = false,
        };
        var effectiveNode = new TreeNode("Effective");
        effective.Nodes.Add(effectiveNode);
        using var effectiveBitmap = new Bitmap(220, 50);
        using (var graphics = Graphics.FromImage(effectiveBitmap))
        {
            graphics.Clear(Color.Magenta);
            effective.RenderNodeForTesting(graphics, effectiveNode, rowBounds, labelBounds, TreeNodeStates.Selected);
        }

        using var ineffective = new BootstrapTreeView
        {
            Size = new Size(220, 50),
            FullRowSelect = true,
            ShowLines = true,
            HideSelection = false,
        };
        var ineffectiveNode = new TreeNode("Ineffective");
        ineffective.Nodes.Add(ineffectiveNode);
        using var ineffectiveBitmap = new Bitmap(220, 50);
        using (var graphics = Graphics.FromImage(ineffectiveBitmap))
        {
            graphics.Clear(Color.Magenta);
            ineffective.RenderNodeForTesting(graphics, ineffectiveNode, rowBounds, labelBounds, TreeNodeStates.Selected);
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(effectiveBitmap.GetPixel(210, 2).ToArgb(), Is.EqualTo(selectedColor.ToArgb()));
            Assert.That(ineffectiveBitmap.GetPixel(210, 2).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
        }));
    }

    [Test]
    public void NeutralNode_RespectsCallerNodeFontForeColorAndBackColor()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        using var callerFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var treeView = new BootstrapTreeView { Size = new Size(240, 60) };
        var node = new TreeNode("Custom")
        {
            BackColor = Color.FromArgb(250, 230, 150),
            ForeColor = Color.FromArgb(90, 20, 140),
            NodeFont = callerFont,
        };
        treeView.Nodes.Add(node);
        var rowBounds = new Rectangle(0, 0, 240, 26);
        var labelBounds = new Rectangle(42, 0, 160, 26);

        using var bitmap = new Bitmap(240, 60);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Magenta);
            treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, (TreeNodeStates)0);
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bitmap.GetPixel(labelBounds.Right - 2, labelBounds.Top + 2), Is.EqualTo(node.BackColor));
            Assert.That(ContainsColorNear(bitmap, labelBounds, node.ForeColor, tolerance: 90), Is.True);
            Assert.That(node.NodeFont, Is.SameAs(callerFont));
            Assert.That(callerFont.GetHeight(), Is.GreaterThan(0));
        }));
    }

    [Test]
    public void OversizedNodeFont_DoesNotThrowOrBecomeFrameworkOwned()
    {
        using var callerFont = new Font("Segoe UI", 28f, FontStyle.Regular);
        var treeView = new BootstrapTreeView { Size = new Size(240, 50), ItemHeight = 20 };
        var node = new TreeNode("Oversized") { NodeFont = callerFont };
        treeView.Nodes.Add(node);
        using var bitmap = new Bitmap(240, 50);
        using var graphics = Graphics.FromImage(bitmap);

        Assert.DoesNotThrow((Action)(() =>
            treeView.RenderNodeForTesting(
                graphics,
                node,
                new Rectangle(0, 0, 240, 20),
                new Rectangle(40, 0, 160, 20),
                (TreeNodeStates)0)));

        treeView.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(callerFont.GetHeight(), Is.GreaterThan(0));
            Assert.That(node.NodeFont, Is.SameAs(callerFont));
        }));
    }

    [Test]
    public void OwnerDraw_EmptyZeroAndPartiallyClippedGeometryDoesNotThrow()
    {
        using var treeView = new BootstrapTreeView { Size = new Size(120, 40) };
        var node = new TreeNode(string.Empty);
        treeView.Nodes.Add(node);
        using var bitmap = new Bitmap(120, 40);
        using var graphics = Graphics.FromImage(bitmap);

        Assert.DoesNotThrow((Action)(() =>
        {
            treeView.RenderNodeForTesting(graphics, node, Rectangle.Empty, Rectangle.Empty, (TreeNodeStates)0);
            treeView.RenderNodeForTesting(
                graphics,
                node,
                new Rectangle(0, 0, 120, 20),
                new Rectangle(-40, 0, 80, 20),
                TreeNodeStates.Selected);
        }));
    }

    [Test]
    public void DefaultItemHeight_AccommodatesThemeBodyFontAcrossLogicalDpiMatrix()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        foreach (var dpi in new[] { 96, 120, 144, 168, 192 })
        {
            using var font = new Font(
                theme.Typography.Body.FontFamilyName,
                theme.Typography.Body.SizeInPoints,
                theme.Typography.Body.Style);
            var itemHeight = BootstrapTreeView.CalculateDefaultItemHeight(theme, dpi);
            var requiredTextHeight = (int)Math.Ceiling(font.GetHeight(dpi));

            Assert.That(itemHeight, Is.GreaterThanOrEqualTo(requiredTextHeight + DpiScaler.Scale(theme.Metrics.SpacingXS, dpi)), $"dpi={dpi}");
        }
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void HandleBackedOwnerDraw_RendersBitmapAndThemeFontFitsManagedItemHeight(BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var treeView = new BootstrapTreeView { Size = new Size(240, 60) };
        var node = new TreeNode("Handle-backed node");
        treeView.Nodes.Add(node);
        _ = treeView.Handle;

        var dpi = treeView.DeviceDpi > 0 ? treeView.DeviceDpi : DpiScaler.DefaultDpi;
        var nativeLabelBounds = node.Bounds;
        Assert.That(nativeLabelBounds.IsEmpty, Is.False);
        var rowBounds = new Rectangle(0, nativeLabelBounds.Top, treeView.ClientSize.Width, treeView.ItemHeight);
        using var bitmap = new Bitmap(treeView.ClientSize.Width, treeView.ClientSize.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Magenta);
            Assert.DoesNotThrow((Action)(() =>
                treeView.RenderNodeForTesting(graphics, node, rowBounds, nativeLabelBounds, (TreeNodeStates)0)));
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        using var themeFont = new Font(
            theme.Typography.Body.FontFamilyName,
            theme.Typography.Body.SizeInPoints,
            theme.Typography.Body.Style);
        var requiredHeight = (int)Math.Ceiling(themeFont.GetHeight(dpi)) + DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(treeView.ItemHeight, Is.GreaterThanOrEqualTo(requiredHeight));
            Assert.That(
                bitmap.GetPixel(nativeLabelBounds.Right - 2, nativeLabelBounds.Top + 2).ToArgb(),
                Is.EqualTo(theme.Colors.Surface.ToArgb()));
        }));
    }

    [Test]
    public void FocusCueDecision_RequiresVisibleSelectionFocusAndFocusCues()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapTreeView.ShouldDrawFocusCueForTesting(true, true, true), Is.True);
            Assert.That(BootstrapTreeView.ShouldDrawFocusCueForTesting(false, true, true), Is.False);
            Assert.That(BootstrapTreeView.ShouldDrawFocusCueForTesting(true, false, true), Is.False);
            Assert.That(BootstrapTreeView.ShouldDrawFocusCueForTesting(true, true, false), Is.False);
        }));
    }

    private static bool ContainsColorNear(Bitmap bitmap, Rectangle bounds, Color expected, int tolerance)
    {
        var clipped = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), bounds);
        for (var y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (var x = clipped.Left; x < clipped.Right; x++)
            {
                var actual = bitmap.GetPixel(x, y);
                var distance = Math.Abs(actual.R - expected.R) + Math.Abs(actual.G - expected.G) + Math.Abs(actual.B - expected.B);
                if (distance <= tolerance)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
