using System;
using System.Drawing;
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
public sealed class BootstrapTreeViewReviewRound10RegressionTests
{
    [Test]
    public void Layout_BottomClippedRowPreservesNativeVerticalSlotGeometry()
    {
        var layout = BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            clientBounds: new Rectangle(0, 0, 320, 24),
            drawBounds: new Rectangle(0, 14, 320, 24),
            nativeLabelBounds: new Rectangle(112, 14, 120, 24),
            nodeLevel: 0,
            dpi: 96,
            rightToLeft: false,
            effectiveFullRowSelection: false,
            hasExpander: true,
            hasStateImage: true,
            nativeStateImageSlotWidth: 16,
            hasNodeImage: true,
            nodeImageSize: new Size(16, 16),
            useNativeStateImageSize: true));
        var horizontalConnector = BootstrapTreeViewLayout.CalculateHorizontalConnectorLine(
            layout.RowBounds,
            layout.ExpanderAnchorX,
            layout.TextBounds.Left);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.RowBounds, Is.EqualTo(new Rectangle(0, 14, 320, 24)),
                "Row geometry must keep the complete native row even when only its top fragment is visible.");
            Assert.That(layout.NodeImageBounds, Is.EqualTo(new Rectangle(93, 18, 16, 16)),
                "The node image must stay centered in the full 24px native row and only be viewport-clipped by Graphics.");
            Assert.That(layout.StateImageBounds, Is.EqualTo(new Rectangle(77, 18, 16, 16)),
                "The native state image must not shrink or re-center into the 10px visible row fragment.");
            Assert.That(layout.ExpanderBounds, Is.EqualTo(new Rectangle(63, 21, 9, 9)),
                "The expander must keep its full native-row vertical position when the bottom row is partially visible.");
            Assert.That(horizontalConnector.Start.Y, Is.EqualTo(26),
                "Connector geometry must use the native row center rather than the clipped viewport fragment center.");
            Assert.That(horizontalConnector.End.Y, Is.EqualTo(26));
        }));
    }

    [Test]
    public void OwnerDraw_BottomClippedRowDoesNotPullConnectorCenterIntoViewport()
    {
        using var treeView = new BootstrapTreeView
        {
            Size = new Size(320, 24),
            ItemHeight = 24,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
        };
        var root = new TreeNode("Root");
        root.Nodes.Add(new TreeNode("Child"));
        treeView.Nodes.Add(root);
        _ = treeView.Handle;

        using var bitmap = new Bitmap(treeView.ClientSize.Width, treeView.ClientSize.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Magenta);
            treeView.RenderNodeForTesting(
                graphics,
                root,
                new Rectangle(0, 14, treeView.ClientSize.Width, treeView.ItemHeight),
                new Rectangle(80, 14, 80, treeView.ItemHeight),
                (TreeNodeStates)0);
        }

        var border = BootstrapThemeManager.CurrentTheme.Colors.Border;
        Assert.That(
            ContainsColorOnRow(bitmap, border, y: 19),
            Is.False,
            "The horizontal connector belongs at the full native row center (y=26), below the viewport; clipping must not synthesize a connector at the visible-fragment center.");
    }

    private static bool ContainsColorOnRow(Bitmap bitmap, Color expected, int y)
    {
        if (y < 0 || y >= bitmap.Height)
        {
            return false;
        }

        for (var x = 0; x < bitmap.Width; x++)
        {
            var actual = bitmap.GetPixel(x, y);
            if (Math.Abs(actual.R - expected.R) <= 8 &&
                Math.Abs(actual.G - expected.G) <= 8 &&
                Math.Abs(actual.B - expected.B) <= 8)
            {
                return true;
            }
        }

        return false;
    }
}
