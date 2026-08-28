using System;
using System.Collections.Generic;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapTabControlRenderLogicTests
{
    [Test]
    public void DefaultMetricsAt96DpiMatchTabHeaderContract()
    {
        var actual = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.Height, Is.EqualTo(32));
            Assert.That(actual.HorizontalPadding, Is.EqualTo(12));
            Assert.That(actual.ContentSpacing, Is.EqualTo(8));
            Assert.That(actual.MinimumWidth, Is.EqualTo(54));
            Assert.That(actual.BorderWidth, Is.EqualTo(1));
            Assert.That(actual.FocusBorderWidth, Is.EqualTo(2));
            Assert.That(actual.UnderlineHeight, Is.EqualTo(2));
            Assert.That(actual.Radius, Is.EqualTo(6));
        }));
    }

    [TestCase(120, 40, 15, 10, 68, 1, 3, 3, 8)]
    [TestCase(144, 48, 18, 12, 81, 2, 3, 3, 9)]
    [TestCase(168, 56, 21, 14, 95, 2, 4, 4, 11)]
    [TestCase(192, 64, 24, 16, 108, 2, 4, 4, 12)]
    public void MetricsScaleAcrossSupportedDpiMatrix(
        int dpi,
        int height,
        int padding,
        int spacing,
        int minimumWidth,
        int borderWidth,
        int focusBorderWidth,
        int underlineHeight,
        int radius)
    {
        var actual = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi, -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.Height, Is.EqualTo(height));
            Assert.That(actual.HorizontalPadding, Is.EqualTo(padding));
            Assert.That(actual.ContentSpacing, Is.EqualTo(spacing));
            Assert.That(actual.MinimumWidth, Is.EqualTo(minimumWidth));
            Assert.That(actual.BorderWidth, Is.EqualTo(borderWidth));
            Assert.That(actual.FocusBorderWidth, Is.EqualTo(focusBorderWidth));
            Assert.That(actual.UnderlineHeight, Is.EqualTo(underlineHeight));
            Assert.That(actual.Radius, Is.EqualTo(radius));
        }));
    }

    [Test]
    public void ResolveMetricsValidatesArgumentsAndExplicitRadius()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapTabControlRenderLogic.ResolveMetrics(null!, 96, -1)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 0, -1)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -2)));
            Assert.That(BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 192, 10).Radius, Is.EqualTo(20));
            Assert.That(BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 192, 0).Radius, Is.EqualTo(0));
        }));
    }

    [Test]
    public void UniformWidthUsesWidestContentWhenFillIsFalse()
    {
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var width = BootstrapTabControlRenderLogic.CalculateUniformItemWidth(
            3,
            600,
            new List<int> { 30, 70, 45 },
            metrics,
            fill: false);

        Assert.That(width, Is.EqualTo(94));
    }

    [Test]
    public void UniformWidthUsesAvailableSpaceWhenFillIsTrueAndRespectsMinimum()
    {
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                BootstrapTabControlRenderLogic.CalculateUniformItemWidth(3, 600, new[] { 20, 20, 20 }, metrics, fill: true),
                Is.EqualTo(200));
            Assert.That(
                BootstrapTabControlRenderLogic.CalculateUniformItemWidth(3, 90, new[] { 20, 20, 20 }, metrics, fill: true),
                Is.EqualTo(54));
            Assert.That(
                BootstrapTabControlRenderLogic.CalculateUniformItemWidth(0, 600, Array.Empty<int>(), metrics, fill: true),
                Is.EqualTo(54));
        }));
    }

    [Test]
    public void UniformWidthRejectsPreferredWidthCountMismatch()
    {
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        Assert.Throws<ArgumentException>((Action)(() =>
            BootstrapTabControlRenderLogic.CalculateUniformItemWidth(2, 400, new[] { 20 }, metrics, fill: false)));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void SelectedPillsUseSemanticSurfaceAndContrastingText(BootstrapThemeMode mode)
    {
        var colors = BootstrapTheme.CreateDefault(mode).Colors;
        var semantic = BootstrapVariantColorResolver.Resolve(colors, BootstrapVariant.Primary);
        var actual = BootstrapTabControlRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Primary,
            BootstrapTabStyle.Pills,
            selected: true,
            enabled: true,
            hovered: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.Background, Is.EqualTo(semantic));
            Assert.That(actual.Foreground, Is.EqualTo(ColorUtil.GetContrastingTextColor(semantic, colors.Light, colors.Dark)));
            Assert.That(actual.Border, Is.EqualTo(semantic));
            Assert.That(actual.Accent, Is.EqualTo(semantic));
            Assert.That(actual.Focus, Is.EqualTo(colors.Focus));
        }));
    }

    [Test]
    public void DisabledPaletteUsesDisabledTokensRegardlessOfStyle()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;

        Assert.Multiple((Action)(() =>
        {
            foreach (BootstrapTabStyle style in Enum.GetValues(typeof(BootstrapTabStyle)))
            {
                var actual = BootstrapTabControlRenderLogic.ResolvePalette(
                    colors,
                    BootstrapVariant.Danger,
                    style,
                    selected: true,
                    enabled: false,
                    hovered: true);
                Assert.That(actual.Background, Is.EqualTo(colors.Surface));
                Assert.That(actual.Foreground, Is.EqualTo(colors.Disabled));
                Assert.That(actual.Border, Is.EqualTo(colors.Border));
                Assert.That(actual.Accent, Is.EqualTo(colors.Disabled));
                Assert.That(actual.Focus, Is.EqualTo(colors.Focus));
            }
        }));
    }

    [Test]
    public void LayoutUsesStyleSpecificCornerGeometryAndKeepsContentContained()
    {
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var bounds = new Rectangle(10, 5, 120, 32);
        var tabs = BootstrapTabControlRenderLogic.CalculateLayout(bounds, BootstrapTabStyle.Tabs, metrics, 50, Size.Empty, hasImage: false);
        var pills = BootstrapTabControlRenderLogic.CalculateLayout(bounds, BootstrapTabStyle.Pills, metrics, 50, new Size(16, 16), hasImage: true);
        var underline = BootstrapTabControlRenderLogic.CalculateLayout(bounds, BootstrapTabStyle.Underline, metrics, 50, Size.Empty, hasImage: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.CornerRadius, Is.EqualTo(new CornerRadius(6f, 6f, 0f, 0f)));
            Assert.That(pills.CornerRadius, Is.EqualTo(new CornerRadius(6f)));
            Assert.That(underline.CornerRadius, Is.EqualTo(CornerRadius.Empty));
            Assert.That(tabs.UnderlineBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(pills.ImageBounds.IsEmpty, Is.False);
            Assert.That(underline.UnderlineBounds.Height, Is.EqualTo(2));
            AssertContained(bounds, tabs.ContentBounds);
            AssertContained(bounds, pills.ImageBounds);
            AssertContained(bounds, pills.TextBounds);
            AssertContained(bounds, underline.UnderlineBounds);
        }));
    }

    [Test]
    public void LayoutClampsNarrowAndMalformedBoundsWithoutNegativeRectangles()
    {
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var narrow = BootstrapTabControlRenderLogic.CalculateLayout(new Rectangle(3, 4, 20, 12), BootstrapTabStyle.Pills, metrics, 100, new Size(16, 16), hasImage: true);
        var malformed = BootstrapTabControlRenderLogic.CalculateLayout(new Rectangle(3, 4, -10, -12), BootstrapTabStyle.Underline, metrics, 100, new Size(16, 16), hasImage: true);

        Assert.Multiple((Action)(() =>
        {
            AssertNonNegative(narrow);
            AssertNonNegative(malformed);
            Assert.That(malformed.SurfaceBounds.Width, Is.EqualTo(0));
            Assert.That(malformed.SurfaceBounds.Height, Is.EqualTo(0));
        }));
    }

    [Test]
    public void UndefinedStyleAndVariantAreRejected()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTabControlRenderLogic.ValidateStyle((BootstrapTabStyle)999)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTabControlRenderLogic.ValidateVariant((BootstrapVariant)999)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTabControlRenderLogic.ResolvePalette(colors, BootstrapVariant.Primary, (BootstrapTabStyle)999, false, true, false)));
        }));
    }

    private static void AssertNonNegative(BootstrapTabHeaderLayout layout)
    {
        Assert.That(layout.SurfaceBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.SurfaceBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.ContentBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.ContentBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.ImageBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.ImageBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.TextBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.TextBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.UnderlineBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.UnderlineBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.FocusBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.FocusBounds.Height, Is.GreaterThanOrEqualTo(0));
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
}
