using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapToastLayoutLogicTests
{
    private static readonly Size[] ToastSizes =
    {
        new Size(200, 60),
        new Size(220, 80),
        new Size(180, 70)
    };

    [Test]
    public void PlacementEnumContainsExactlyTheFourPlannedCorners()
    {
        Assert.That(
            Enum.GetValues(typeof(BootstrapToastPlacement)).Cast<BootstrapToastPlacement>().ToArray(),
            Is.EqualTo(new[]
            {
                BootstrapToastPlacement.TopLeft,
                BootstrapToastPlacement.TopRight,
                BootstrapToastPlacement.BottomLeft,
                BootstrapToastPlacement.BottomRight
            }));
    }

    [TestCase(BootstrapToastPlacement.TopLeft, 10, 20, 10, 88, 10, 176)]
    [TestCase(BootstrapToastPlacement.TopRight, 210, 20, 190, 88, 230, 176)]
    [TestCase(BootstrapToastPlacement.BottomLeft, 10, 360, 10, 272, 10, 194)]
    [TestCase(BootstrapToastPlacement.BottomRight, 210, 360, 190, 272, 230, 194)]
    public void StackLayoutAnchorsFifoEntriesForEveryPlacement(
        BootstrapToastPlacement placement,
        int x1,
        int y1,
        int x2,
        int y2,
        int x3,
        int y3)
    {
        var actual = BootstrapToastLayoutLogic.CalculateStackBounds(
            new Rectangle(10, 20, 400, 400),
            ToastSizes,
            placement,
            logicalSpacing: 8,
            maximumVisibleToasts: 5,
            dpi: 96);

        Assert.That(actual, Is.EqualTo(new[]
        {
            new Rectangle(x1, y1, 200, 60),
            new Rectangle(x2, y2, 220, 80),
            new Rectangle(x3, y3, 180, 70)
        }));
    }

    [Test]
    public void StackSpacingScalesThroughDpiScaler()
    {
        var actual = BootstrapToastLayoutLogic.CalculateStackBounds(
            new Rectangle(10, 20, 400, 400),
            ToastSizes,
            BootstrapToastPlacement.TopLeft,
            logicalSpacing: 8,
            maximumVisibleToasts: 5,
            dpi: 192);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual[0].Y, Is.EqualTo(20));
            Assert.That(actual[1].Y, Is.EqualTo(96));
            Assert.That(actual[2].Y, Is.EqualTo(192));
        }));
    }

    [Test]
    public void StackLayoutHonorsMaximumVisibleCount()
    {
        var actual = BootstrapToastLayoutLogic.CalculateStackBounds(
            new Rectangle(0, 0, 400, 400),
            ToastSizes,
            BootstrapToastPlacement.TopRight,
            logicalSpacing: 8,
            maximumVisibleToasts: 2,
            dpi: 96);

        Assert.That(actual.Count, Is.EqualTo(2));
    }

    [Test]
    public void CalculateRequiredStackHeight_AddsHeightsAndScaledGaps()
    {
        var sizes = new[] { new Size(320, 80), new Size(320, 100), new Size(320, 120) };

        var height = BootstrapToastLayoutLogic.CalculateRequiredStackHeight(
            sizes,
            logicalSpacing: 8,
            dpi: 96);

        Assert.That(height, Is.EqualTo(80 + 8 + 100 + 8 + 120));
    }

    [Test]
    public void CalculateRequiredStackHeight_ValidatesInputsAndSaturatesOverflow()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapToastLayoutLogic.CalculateRequiredStackHeight(Array.Empty<Size>(), 8, 96), Is.Zero);
            Assert.That(
                BootstrapToastLayoutLogic.CalculateRequiredStackHeight(
                    new[] { new Size(1, int.MaxValue), new Size(1, int.MaxValue) },
                    8,
                    96),
                Is.EqualTo(int.MaxValue));
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapToastLayoutLogic.CalculateRequiredStackHeight(null!, 8, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateRequiredStackHeight(ToastSizes, -1, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateRequiredStackHeight(ToastSizes, 8, 0)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateRequiredStackHeight(new[] { new Size(1, -1) }, 8, 96)));
        }));
    }

    [Test]
    public void DefaultMetricsMatchStage8LogicalContract()
    {
        var actual = BootstrapToastLayoutLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.HorizontalPadding, Is.EqualTo(12));
            Assert.That(actual.VerticalPadding, Is.EqualTo(8));
            Assert.That(actual.ContentSpacing, Is.EqualTo(8));
            Assert.That(actual.TitleBodySpacing, Is.EqualTo(4));
            Assert.That(actual.IconSize, Is.EqualTo(16));
            Assert.That(actual.CloseButtonSize, Is.EqualTo(28));
            Assert.That(actual.BorderWidth, Is.EqualTo(1));
            Assert.That(actual.Radius, Is.EqualTo(6));
            Assert.That(actual.SlideDistance, Is.EqualTo(16));
        }));
    }

    [Test]
    public void MetricsScaleAcrossSupportedDpiMatrix()
    {
        var actual = BootstrapToastLayoutLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 192);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.HorizontalPadding, Is.EqualTo(24));
            Assert.That(actual.VerticalPadding, Is.EqualTo(16));
            Assert.That(actual.ContentSpacing, Is.EqualTo(16));
            Assert.That(actual.TitleBodySpacing, Is.EqualTo(8));
            Assert.That(actual.IconSize, Is.EqualTo(32));
            Assert.That(actual.CloseButtonSize, Is.EqualTo(56));
            Assert.That(actual.BorderWidth, Is.EqualTo(2));
            Assert.That(actual.Radius, Is.EqualTo(12));
            Assert.That(actual.SlideDistance, Is.EqualTo(32));
        }));
    }

    [Test]
    public void PreferredHeightUsesTitleBodyFlowAndTallestAdornment()
    {
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96);
        var actual = BootstrapToastLayoutLogic.CalculatePreferredHeight(
            metrics,
            new Size(220, 18),
            new Size(220, 36),
            hasTitle: true,
            hasIcon: true,
            dismissible: true);

        Assert.That(actual, Is.EqualTo(74));
    }

    [Test]
    public void ContentLayoutKeepsTitleBodyIconAndCloseInsidePaddedSurface()
    {
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96);
        var layout = BootstrapToastLayoutLogic.CalculateContentLayout(
            new Rectangle(0, 0, 320, 96),
            metrics,
            hasTitle: true,
            hasIcon: true,
            dismissible: true,
            titleSize: new Size(220, 18),
            bodySize: new Size(220, 36));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.SurfaceBounds, Is.EqualTo(new Rectangle(0, 0, 320, 96)));
            Assert.That(layout.ContentBounds, Is.EqualTo(new Rectangle(12, 8, 296, 80)));
            Assert.That(layout.IconBounds.Width, Is.EqualTo(16));
            Assert.That(layout.CloseBounds.Width, Is.EqualTo(28));
            Assert.That(layout.TitleBounds.Height, Is.EqualTo(18));
            Assert.That(layout.BodyBounds.Height, Is.EqualTo(36));
            Assert.That(layout.CornerRadius, Is.EqualTo(new CornerRadius(6f)));
            AssertContained(layout.ContentBounds, layout.IconBounds);
            AssertContained(layout.ContentBounds, layout.TitleBounds);
            AssertContained(layout.ContentBounds, layout.BodyBounds);
            AssertContained(layout.ContentBounds, layout.CloseBounds);
        }));
    }

    [Test]
    public void TinyAndMalformedContentBoundsClampWithoutNegativeGeometry()
    {
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96);
        var layouts = new[]
        {
            BootstrapToastLayoutLogic.CalculateContentLayout(new Rectangle(5, 7, 30, 18), metrics, true, true, true, new Size(200, 18), new Size(200, 80)),
            BootstrapToastLayoutLogic.CalculateContentLayout(Rectangle.Empty, metrics, true, true, true, Size.Empty, Size.Empty),
            BootstrapToastLayoutLogic.CalculateContentLayout(new Rectangle(3, 4, -10, -20), metrics, true, true, true, Size.Empty, Size.Empty)
        };

        Assert.Multiple((Action)(() =>
        {
            foreach (var layout in layouts)
            {
                AssertNonNegative(layout.SurfaceBounds);
                AssertNonNegative(layout.ContentBounds);
                AssertNonNegative(layout.IconBounds);
                AssertNonNegative(layout.TitleBounds);
                AssertNonNegative(layout.BodyBounds);
                AssertNonNegative(layout.CloseBounds);
            }
        }));
    }

    [Test]
    public void InvalidInputsAreRejectedBeforeProducingLayout()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapToastLayoutLogic.CalculateStackBounds(Rectangle.Empty, null!, BootstrapToastPlacement.TopLeft, 8, 5, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateStackBounds(Rectangle.Empty, ToastSizes, (BootstrapToastPlacement)999, 8, 5, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateStackBounds(Rectangle.Empty, ToastSizes, BootstrapToastPlacement.TopLeft, -1, 5, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateStackBounds(Rectangle.Empty, ToastSizes, BootstrapToastPlacement.TopLeft, 8, 0, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateStackBounds(Rectangle.Empty, ToastSizes, BootstrapToastPlacement.TopLeft, 8, 5, 0)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.CalculateStackBounds(Rectangle.Empty, new[] { new Size(-1, 20) }, BootstrapToastPlacement.TopLeft, 8, 5, 96)));
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapToastLayoutLogic.ResolveMetrics(null!, 96)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapToastLayoutLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 0)));
        }));
    }

    private static void AssertContained(Rectangle outer, Rectangle inner)
    {
        if (inner.Width == 0 || inner.Height == 0)
        {
            return;
        }

        Assert.That(inner.Left, Is.GreaterThanOrEqualTo(outer.Left));
        Assert.That(inner.Top, Is.GreaterThanOrEqualTo(outer.Top));
        Assert.That(inner.Right, Is.LessThanOrEqualTo(outer.Right));
        Assert.That(inner.Bottom, Is.LessThanOrEqualTo(outer.Bottom));
    }

    private static void AssertNonNegative(Rectangle bounds)
    {
        Assert.That(bounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(bounds.Height, Is.GreaterThanOrEqualTo(0));
    }
}
