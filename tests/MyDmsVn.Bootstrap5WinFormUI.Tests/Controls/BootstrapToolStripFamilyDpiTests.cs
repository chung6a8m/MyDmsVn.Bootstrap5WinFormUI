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

    [TestCase(96, false, 2)]
    [TestCase(144, false, 3)]
    [TestCase(192, false, 4)]
    [TestCase(96, true, 2)]
    [TestCase(144, true, 3)]
    [TestCase(192, true, 4)]
    public void SplitDividerInsetScalesWithDpiAndArrowRemainsInsideNativeBounds(int dpi, bool rightToLeft, int expectedInset)
    {
        var metrics = BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi);
        var dropDown = rightToLeft ? new Rectangle(0, 0, 24, 36) : new Rectangle(78, 0, 24, 36);
        var splitter = rightToLeft ? new Rectangle(24, 0, 2, 36) : new Rectangle(76, 0, 2, 36);
        var geometry = BootstrapToolStripRenderLogic.ResolveSplitButtonGeometry(splitter, dropDown, metrics.ArrowSize, metrics.SplitDividerInset);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(geometry.ArrowPoints.All(point => dropDown.Contains(Point.Round(point))), Is.True);
            Assert.That(metrics.SplitDividerInset, Is.EqualTo(expectedInset));
            Assert.That(geometry.DividerStart.X, Is.InRange(splitter.Left, splitter.Right));
            Assert.That(geometry.DividerStart.Y, Is.EqualTo(splitter.Top + expectedInset));
            Assert.That(geometry.DividerEnd.Y, Is.EqualTo(splitter.Bottom - 1 - expectedInset));
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
