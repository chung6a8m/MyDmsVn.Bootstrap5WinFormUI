using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapListViewLayoutLogicTests
{
    [Test]
    public void DeflateClampsCollapsedGeometryToEmpty()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapListViewLayoutLogic.Deflate(new Rectangle(2, 3, 20, 10), 4, 2), Is.EqualTo(new Rectangle(6, 5, 12, 6)));
            Assert.That(BootstrapListViewLayoutLogic.Deflate(new Rectangle(0, 0, 3, 2), 2, 2), Is.EqualTo(Rectangle.Empty));
            Assert.That(BootstrapListViewLayoutLogic.Deflate(new Rectangle(0, 0, -1, 4), 1, 1), Is.EqualTo(Rectangle.Empty));
        }));
    }

    [Test]
    public void FocusBoundsUseNativeRowAndLabelSemantics()
    {
        var item = new Rectangle(0, 10, 200, 24);
        var label = new Rectangle(28, 10, 80, 24);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapListViewLayoutLogic.GetFocusBounds(View.Details, item, label, true), Is.EqualTo(item));
            Assert.That(BootstrapListViewLayoutLogic.GetFocusBounds(View.Details, item, label, false), Is.EqualTo(label));
            Assert.That(BootstrapListViewLayoutLogic.GetFocusBounds(View.List, item, label, false), Is.EqualTo(label));
            Assert.That(BootstrapListViewLayoutLogic.GetFocusBounds(View.LargeIcon, item, label, false), Is.EqualTo(item));
            Assert.That(BootstrapListViewLayoutLogic.GetFocusBounds(View.Tile, item, label, false), Is.EqualTo(item));
        }));
    }

    [TestCase(HorizontalAlignment.Left, false, TextFormatFlags.Left)]
    [TestCase(HorizontalAlignment.Center, false, TextFormatFlags.HorizontalCenter)]
    [TestCase(HorizontalAlignment.Right, false, TextFormatFlags.Right)]
    [TestCase(HorizontalAlignment.Left, true, TextFormatFlags.Right)]
    [TestCase(HorizontalAlignment.Right, true, TextFormatFlags.Left)]
    public void TextFlagsRespectAlignmentAndRtl(
        HorizontalAlignment alignment,
        bool rightToLeft,
        TextFormatFlags expectedAlignment)
    {
        var flags = BootstrapListViewLayoutLogic.GetTextFlags(alignment, rightToLeft, wordWrap: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That((flags & expectedAlignment) == expectedAlignment, Is.True);
            Assert.That((flags & TextFormatFlags.NoPrefix) != 0, Is.True);
            Assert.That((flags & TextFormatFlags.EndEllipsis) != 0, Is.True);
            Assert.That((flags & TextFormatFlags.SingleLine) != 0, Is.True);
            Assert.That((flags & TextFormatFlags.RightToLeft) != 0, Is.EqualTo(rightToLeft));
        }));
    }

    [Test]
    public void TileTextBoundsUseImageAnchorAndMirrorForRtl()
    {
        var item = new Rectangle(10, 20, 200, 64);
        var leftImage = new Rectangle(18, 28, 32, 32);
        var rightImage = new Rectangle(170, 28, 32, 32);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                BootstrapListViewLayoutLogic.GetTileTextBounds(item, leftImage, 8, false),
                Is.EqualTo(new Rectangle(58, 20, 152, 64)));
            Assert.That(
                BootstrapListViewLayoutLogic.GetTileTextBounds(item, rightImage, 8, true),
                Is.EqualTo(new Rectangle(10, 20, 152, 64)));
            Assert.That(
                BootstrapListViewLayoutLogic.GetTileTextBounds(new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 10, 10), 4, false),
                Is.EqualTo(Rectangle.Empty));
        }));
    }
}
