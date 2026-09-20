using System;
using System.Collections.Generic;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapBreadcrumbLayoutLogicTests
{
    [Test]
    public void ValidationRejectsNullSegmentsAndNegativeGeometry()
    {
        var metrics = new BootstrapBreadcrumbLayoutMetrics(8, 4);

        Assert.That(
            (Action)(() => BootstrapBreadcrumbLayoutLogic.Measure(null!, 100, metrics, true)),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            (Action)(() => BootstrapBreadcrumbLayoutLogic.Arrange(null!, Rectangle.Empty, metrics, true, false)),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            (Action)(() => _ = new BootstrapBreadcrumbLayoutMetrics(-1, 0)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            (Action)(() => _ = new BootstrapBreadcrumbLayoutMetrics(0, -1)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            (Action)(() => _ = new BootstrapBreadcrumbSegmentSize(new Size(-1, 0), Size.Empty, false)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            (Action)(() => _ = new BootstrapBreadcrumbSegmentSize(Size.Empty, new Size(0, -1), true)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void EmptySegmentsReturnEmptyResultsAndNegativeBoundsAreNormalized()
    {
        var metrics = new BootstrapBreadcrumbLayoutMetrics(8, 4);
        var empty = Array.Empty<BootstrapBreadcrumbSegmentSize>();

        var measured = BootstrapBreadcrumbLayoutLogic.Measure(empty, 100, metrics, true);
        var arranged = BootstrapBreadcrumbLayoutLogic.Arrange(
            empty,
            new Rectangle(5, 7, -10, -20),
            metrics,
            true,
            false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(measured, Is.EqualTo(Size.Empty));
            Assert.That(arranged, Is.Empty);
        }));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NonPositiveMaximumWidthMeasuresOneUnboundedRow(int maximumWidth)
    {
        var measured = BootstrapBreadcrumbLayoutLogic.Measure(
            CreateThreeSegments(),
            maximumWidth,
            new BootstrapBreadcrumbLayoutMetrics(8, 4),
            true);

        Assert.That(measured, Is.EqualTo(new Size(168, 20)));
    }

    [Test]
    public void OneRowMeasurementIncludesDividerAndTwoGapsPerFollowingSegment()
    {
        var measured = BootstrapBreadcrumbLayoutLogic.Measure(
            CreateThreeSegments(),
            500,
            new BootstrapBreadcrumbLayoutMetrics(8, 4),
            true);

        Assert.That(measured, Is.EqualTo(new Size(168, 20)));
    }

    [Test]
    public void EmptyDividerStillLeavesTwoDividerGaps()
    {
        var segments = new[]
        {
            new BootstrapBreadcrumbSegmentSize(new Size(40, 20), Size.Empty, false),
            new BootstrapBreadcrumbSegmentSize(new Size(50, 20), Size.Empty, true),
        };

        var measured = BootstrapBreadcrumbLayoutLogic.Measure(
            segments,
            500,
            new BootstrapBreadcrumbLayoutMetrics(8, 4),
            true);
        var arranged = BootstrapBreadcrumbLayoutLogic.Arrange(
            segments,
            new Rectangle(0, 0, 106, 20),
            new BootstrapBreadcrumbLayoutMetrics(8, 4),
            true,
            false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(measured, Is.EqualTo(new Size(106, 20)));
            Assert.That(arranged[1].DividerBounds, Is.EqualTo(new Rectangle(48, 10, 0, 0)));
            Assert.That(arranged[1].ItemBounds, Is.EqualTo(new Rectangle(56, 0, 50, 20)));
        }));
    }

    [Test]
    public void WrappingMovesWholeThirdSegmentAndMeasureMatchesArrangeRows()
    {
        var segments = CreateThreeSegments();
        var metrics = new BootstrapBreadcrumbLayoutMetrics(8, 4);

        var measured = BootstrapBreadcrumbLayoutLogic.Measure(segments, 114, metrics, true);
        var arranged = BootstrapBreadcrumbLayoutLogic.Arrange(
            segments,
            new Rectangle(10, 20, 114, 100),
            metrics,
            true,
            false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(measured, Is.EqualTo(new Size(114, 44)));
            Assert.That(arranged[0].ItemBounds, Is.EqualTo(new Rectangle(10, 20, 40, 20)));
            Assert.That(arranged[1].DividerBounds, Is.EqualTo(new Rectangle(58, 20, 8, 20)));
            Assert.That(arranged[1].ItemBounds, Is.EqualTo(new Rectangle(74, 20, 50, 20)));
            Assert.That(arranged[2].DividerBounds, Is.EqualTo(new Rectangle(18, 44, 8, 20)));
            Assert.That(arranged[2].ItemBounds, Is.EqualTo(new Rectangle(34, 44, 30, 20)));
            Assert.That(arranged[2].DividerBounds.Top, Is.EqualTo(arranged[2].ItemBounds.Top));
        }));
    }

    [Test]
    public void WrapContentsFalseAlwaysUsesOneRow()
    {
        var metrics = new BootstrapBreadcrumbLayoutMetrics(8, 4);
        var measured = BootstrapBreadcrumbLayoutLogic.Measure(CreateThreeSegments(), 50, metrics, false);
        var arranged = BootstrapBreadcrumbLayoutLogic.Arrange(
            CreateThreeSegments(),
            new Rectangle(0, 0, 50, 20),
            metrics,
            false,
            false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(measured, Is.EqualTo(new Size(168, 20)));
            Assert.That(arranged[2].ItemBounds.Top, Is.Zero);
            Assert.That(arranged[2].ItemBounds.Right, Is.GreaterThan(50));
        }));
    }

    [Test]
    public void OversizedSegmentIsPlacedAloneWithoutNegativeGeometry()
    {
        var segments = new[]
        {
            new BootstrapBreadcrumbSegmentSize(new Size(200, 30), Size.Empty, false),
            new BootstrapBreadcrumbSegmentSize(new Size(20, 10), new Size(8, 10), true),
        };
        var metrics = new BootstrapBreadcrumbLayoutMetrics(8, 4);

        var measured = BootstrapBreadcrumbLayoutLogic.Measure(segments, 50, metrics, true);
        var arranged = BootstrapBreadcrumbLayoutLogic.Arrange(
            segments,
            new Rectangle(0, 0, 50, 100),
            metrics,
            true,
            false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(measured, Is.EqualTo(new Size(200, 44)));
            Assert.That(arranged[0].ItemBounds, Is.EqualTo(new Rectangle(0, 0, 200, 30)));
            Assert.That(arranged[1].ItemBounds, Is.EqualTo(new Rectangle(24, 34, 20, 10)));
            Assert.That(arranged[1].ItemBounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(arranged[1].DividerBounds.Width, Is.GreaterThanOrEqualTo(0));
        }));
    }

    [Test]
    public void RtlArrangementMirrorsCoordinatesAndPreservesLogicalResultOrder()
    {
        var segments = CreateThreeSegments();
        var metrics = new BootstrapBreadcrumbLayoutMetrics(8, 4);
        var bounds = new Rectangle(10, 20, 168, 20);

        var ltr = BootstrapBreadcrumbLayoutLogic.Arrange(segments, bounds, metrics, true, false);
        var rtl = BootstrapBreadcrumbLayoutLogic.Arrange(segments, bounds, metrics, true, true);

        Assert.That(BootstrapBreadcrumbLayoutLogic.Measure(segments, 168, metrics, true), Is.EqualTo(new Size(168, 20)));
        Assert.That(rtl.Count, Is.EqualTo(ltr.Count));
        for (var index = 0; index < ltr.Count; index++)
        {
            Assert.That(rtl[index].ItemBounds, Is.EqualTo(Mirror(ltr[index].ItemBounds, bounds)));
            if (segments[index].HasDivider)
            {
                Assert.That(rtl[index].DividerBounds, Is.EqualTo(Mirror(ltr[index].DividerBounds, bounds)));
            }
            else
            {
                Assert.That(rtl[index].DividerBounds, Is.EqualTo(Rectangle.Empty));
            }
        }
    }

    private static IReadOnlyList<BootstrapBreadcrumbSegmentSize> CreateThreeSegments()
    {
        return new[]
        {
            new BootstrapBreadcrumbSegmentSize(new Size(40, 20), Size.Empty, false),
            new BootstrapBreadcrumbSegmentSize(new Size(50, 20), new Size(8, 20), true),
            new BootstrapBreadcrumbSegmentSize(new Size(30, 20), new Size(8, 20), true),
        };
    }

    private static Rectangle Mirror(Rectangle rectangle, Rectangle bounds)
    {
        return new Rectangle(
            bounds.Left + bounds.Width - (rectangle.Left - bounds.Left) - rectangle.Width,
            rectangle.Top,
            rectangle.Width,
            rectangle.Height);
    }
}
