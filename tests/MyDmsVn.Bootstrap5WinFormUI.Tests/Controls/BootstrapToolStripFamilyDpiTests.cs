using System;
using System.Drawing;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapToolStripFamilyDpiTests
{
    [TestCase(96)]
    [TestCase(144)]
    [TestCase(192)]
    public void SeparatorOverflowAndGripMetricsScaleMonotonically(int dpi)
    {
        var metrics = BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi);
        var horizontal = BootstrapToolStripRenderLogic.ResolveSeparatorLine(new Rectangle(0, 0, 80, 30), metrics.SeparatorInset, false);
        var vertical = BootstrapToolStripRenderLogic.ResolveSeparatorLine(new Rectangle(0, 0, 30, 80), metrics.SeparatorInset, true);
        var grip = BootstrapToolStripRenderLogic.ResolveSizingGripDots(new Size(120, 30), metrics.GripDotSize, false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.ArrowSize, Is.GreaterThanOrEqualTo(dpi / 24));
            Assert.That(horizontal.Start.Y, Is.EqualTo(horizontal.End.Y));
            Assert.That(vertical.Start.X, Is.EqualTo(vertical.End.X));
            Assert.That(grip.All(dot => dot.Width == metrics.GripDotSize), Is.True);
        }));
    }

    [TestCase(96, false)]
    [TestCase(144, false)]
    [TestCase(192, false)]
    [TestCase(96, true)]
    [TestCase(144, true)]
    [TestCase(192, true)]
    public void SplitDividerAndArrowRemainInsideNativeDropDownBounds(int dpi, bool rightToLeft)
    {
        var arrowSize = BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi).ArrowSize;
        var dropDown = rightToLeft ? new Rectangle(0, 0, 24, 36) : new Rectangle(76, 0, 24, 36);
        var button = rightToLeft ? new Rectangle(24, 0, 76, 36) : new Rectangle(0, 0, 76, 36);
        var geometry = BootstrapToolStripRenderLogic.ResolveSplitButtonGeometry(button, dropDown, rightToLeft, arrowSize);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(geometry.ArrowPoints.All(point => dropDown.Contains(Point.Round(point))), Is.True);
            Assert.That(geometry.DividerStart.X, Is.InRange(dropDown.Left, dropDown.Right - 1));
            Assert.That(geometry.DividerEnd.Y, Is.LessThan(dropDown.Bottom));
        }));
    }

    [Test]
    public void RtlSizingGripMirrorsWithoutLeavingSurface()
    {
        var ltr = BootstrapToolStripRenderLogic.ResolveSizingGripDots(new Size(120, 30), 2, false);
        var rtl = BootstrapToolStripRenderLogic.ResolveSizingGripDots(new Size(120, 30), 2, true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ltr.All(dot => dot.Left > 60), Is.True);
            Assert.That(rtl.All(dot => dot.Right < 60), Is.True);
            Assert.That(ltr.Select(dot => dot.Y), Is.EqualTo(rtl.Select(dot => dot.Y)));
        }));
    }
}
