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
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRound3RegressionTests
{
    private const int WmHScroll = 0x0114;
    private const int SbLineRight = 1;

    [TestCase(10, false)]
    [TestCase(48, false)]
    [TestCase(10, true)]
    [TestCase(48, true)]
    public void CustomIndent_KeepsFrameworkExpanderInsideNativePlusMinusHitRegion(int indent, bool rightToLeft)
    {
        using var form = CreateHostForm();
        using var treeView = new BootstrapTreeView
        {
            Dock = DockStyle.Fill,
            ItemHeight = 24,
            Indent = indent,
            ShowLines = false,
            ShowPlusMinus = true,
            ShowRootLines = true,
            RightToLeft = rightToLeft ? RightToLeft.Yes : RightToLeft.No,
            RightToLeftLayout = rightToLeft,
        };
        var root = new TreeNode("Root");
        var branch = new TreeNode("Branch");
        branch.Nodes.Add(new TreeNode("Leaf"));
        root.Nodes.Add(branch);
        treeView.Nodes.Add(root);
        form.Controls.Add(treeView);
        form.Show();
        Application.DoEvents();
        root.Expand();
        branch.Collapse();
        Application.DoEvents();

        var nativePlusMinus = GetNativeHitBounds(treeView, branch, TreeViewHitTestLocations.PlusMinus);
        using var bitmap = RenderNode(treeView, branch);
        var labelBounds = branch.Bounds;
        var structureBand = Rectangle.Intersect(
            treeView.ClientRectangle,
            Rectangle.FromLTRB(
                treeView.ClientRectangle.Left,
                labelBounds.Top,
                Math.Max(treeView.ClientRectangle.Left, labelBounds.Left),
                labelBounds.Bottom));
        var frameworkExpander = GetPaintedBounds(bitmap, structureBand, Color.Magenta);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(frameworkExpander.IsEmpty, Is.False, "Expected the framework expander to be rendered.");
            Assert.That(
                nativePlusMinus.IntersectsWith(frameworkExpander),
                Is.True,
                $"Indent={indent}, RTL={rightToLeft}: framework expander must overlap the native PlusMinus hit target.");
            if (!frameworkExpander.IsEmpty)
            {
                var center = new Point(
                    frameworkExpander.Left + (frameworkExpander.Width / 2),
                    frameworkExpander.Top + (frameworkExpander.Height / 2));
                Assert.That(
                    nativePlusMinus.Contains(center),
                    Is.True,
                    $"Indent={indent}, RTL={rightToLeft}: framework expander center must remain inside the native PlusMinus hit target.");
            }
        }));
    }

    [Test]
    public void HorizontalScroll_TranslatesOwnerDrawnLabelInsteadOfRelayingItAtViewportEdge()
    {
        using var form = CreateHostForm(new Size(180, 120));
        using var treeView = new BootstrapTreeView
        {
            Dock = DockStyle.Fill,
            ItemHeight = 24,
            Indent = 19,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            Scrollable = true,
        };
        var node = new TreeNode("ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        treeView.Nodes.Add(node);
        form.Controls.Add(treeView);
        form.Show();
        Application.DoEvents();

        ScrollRightUntil(treeView, node, targetLeft: -24);
        var firstBounds = node.Bounds;
        using var firstScrolled = RenderNode(treeView, node);

        ScrollRight(treeView, 6);
        var secondBounds = node.Bounds;
        using var secondScrolled = RenderNode(treeView, node);

        var comparisonWidth = Math.Min(64, treeView.ClientSize.Width);
        var comparisonBand = new Rectangle(0, firstBounds.Top, comparisonWidth, treeView.ItemHeight);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(firstBounds.Left, Is.LessThan(0), "The first sample must already be horizontally scrolled.");
            Assert.That(secondBounds.Left, Is.LessThan(firstBounds.Left), "The second sample must use a later native horizontal-scroll position.");
            Assert.That(firstBounds.Right, Is.GreaterThan(treeView.ClientRectangle.Right), "Keep the right edge off-screen so viewport clipping stays stable.");
            Assert.That(secondBounds.Right, Is.GreaterThan(treeView.ClientRectangle.Right), "Keep the right edge off-screen so viewport clipping stays stable.");
            Assert.That(
                ContainsDifferentPixels(firstScrolled, comparisonBand, secondScrolled, comparisonBand),
                Is.True,
                "Changing the native horizontal-scroll position must change the visible label pixels instead of restarting the full label at x=0.");
        }));
    }

    private static Form CreateHostForm()
    {
        return CreateHostForm(new Size(320, 180));
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

    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node)
    {
        var labelBounds = node.Bounds;
        Assert.That(labelBounds.IsEmpty, Is.False);
        var rowBounds = new Rectangle(
            treeView.ClientRectangle.Left,
            labelBounds.Top,
            treeView.ClientSize.Width,
            treeView.ItemHeight);
        var bitmap = new Bitmap(treeView.ClientSize.Width, treeView.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        treeView.RenderNodeForTesting(graphics, node, rowBounds, labelBounds, (TreeNodeStates)0);
        return bitmap;
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

    private static Rectangle GetPaintedBounds(Bitmap bitmap, Rectangle bounds, Color background)
    {
        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;
        var clipped = Rectangle.Intersect(new Rectangle(Point.Empty, bitmap.Size), bounds);
        for (var y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (var x = clipped.Left; x < clipped.Right; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == background.ToArgb())
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return left == int.MaxValue
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static bool ContainsDifferentPixels(
        Bitmap first,
        Rectangle firstBounds,
        Bitmap second,
        Rectangle secondBounds)
    {
        var width = Math.Min(firstBounds.Width, secondBounds.Width);
        var height = Math.Min(firstBounds.Height, secondBounds.Height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (first.GetPixel(firstBounds.Left + x, firstBounds.Top + y).ToArgb() !=
                    second.GetPixel(secondBounds.Left + x, secondBounds.Top + y).ToArgb())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ScrollRightUntil(BootstrapTreeView treeView, TreeNode node, int targetLeft)
    {
        for (var attempt = 0; attempt < 64 && node.Bounds.Left > targetLeft; attempt++)
        {
            ScrollRight(treeView, 1);
        }

        Assert.That(node.Bounds.Left, Is.LessThanOrEqualTo(targetLeft), "Expected a non-zero native horizontal-scroll position.");
    }

    private static void ScrollRight(BootstrapTreeView treeView, int lineCount)
    {
        for (var index = 0; index < lineCount; index++)
        {
            SendMessage(treeView.Handle, WmHScroll, new IntPtr(SbLineRight), IntPtr.Zero);
            Application.DoEvents();
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
