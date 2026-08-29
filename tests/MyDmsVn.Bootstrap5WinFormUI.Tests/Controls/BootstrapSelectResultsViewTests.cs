using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectResultsViewTests
{
    [Test]
    public void VisibleRangeStartsAtScrollOffsetRow()
    {
        var layout = BootstrapSelectResultLayout.Create(1000, rowHeight: 32, viewportHeight: 160, scrollOffset: 320);

        Assert.Multiple((System.Action)(() =>
        {
            Assert.That(layout.FirstVisibleIndex, Is.EqualTo(10));
            Assert.That(layout.LastVisibleIndex, Is.EqualTo(14));
            Assert.That(layout.TotalHeight, Is.EqualTo(32000));
            Assert.That(layout.HitTestIndex(0), Is.EqualTo(10));
            Assert.That(layout.HitTestIndex(159), Is.EqualTo(14));
        }));
    }

    [Test]
    public void LayoutClampsScrollOffsetToLastViewport()
    {
        var layout = BootstrapSelectResultLayout.Create(4, rowHeight: 32, viewportHeight: 64, scrollOffset: 999);

        Assert.That(layout.ScrollOffset, Is.EqualTo(64));
        Assert.That(layout.FirstVisibleIndex, Is.EqualTo(2));
        Assert.That(layout.LastVisibleIndex, Is.EqualTo(3));
    }
}
