using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapAlertRenderLogicTests
{
    private static readonly BootstrapVariant[] Variants =
    {
        BootstrapVariant.Primary,
        BootstrapVariant.Secondary,
        BootstrapVariant.Success,
        BootstrapVariant.Danger,
        BootstrapVariant.Warning,
        BootstrapVariant.Info,
        BootstrapVariant.Light,
        BootstrapVariant.Dark
    };

    [TestCase(BootstrapVariant.Primary)]
    [TestCase(BootstrapVariant.Secondary)]
    [TestCase(BootstrapVariant.Success)]
    [TestCase(BootstrapVariant.Danger)]
    [TestCase(BootstrapVariant.Warning)]
    [TestCase(BootstrapVariant.Info)]
    [TestCase(BootstrapVariant.Light)]
    [TestCase(BootstrapVariant.Dark)]
    public void ValidateVariantAcceptsDefinedVariants(BootstrapVariant variant)
    {
        Assert.DoesNotThrow((Action)(() => BootstrapAlertRenderLogic.ValidateVariant(variant)));
    }

    [Test]
    public void ValidateVariantRejectsUndefinedVariant()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapAlertRenderLogic.ValidateVariant((BootstrapVariant)999)));
    }

    [Test]
    public void DefaultMetricsAt96DpiMatchAlertContract()
    {
        var actual = BootstrapAlertRenderLogic.ResolveMetrics(
            BootstrapThemeMetrics.Default,
            96,
            -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.HorizontalPadding, Is.EqualTo(12));
            Assert.That(actual.VerticalPadding, Is.EqualTo(8));
            Assert.That(actual.ContentSpacing, Is.EqualTo(8));
            Assert.That(actual.IconSize, Is.EqualTo(16));
            Assert.That(actual.CloseButtonSize, Is.EqualTo(28));
            Assert.That(actual.BorderWidth, Is.EqualTo(1));
            Assert.That(actual.FocusBorderWidth, Is.EqualTo(2));
            Assert.That(actual.Radius, Is.EqualTo(6));
        }));
    }

    [TestCase(120, 15, 10, 10, 20, 35, 1, 3, 8)]
    [TestCase(144, 18, 12, 12, 24, 42, 2, 3, 9)]
    [TestCase(168, 21, 14, 14, 28, 49, 2, 4, 11)]
    [TestCase(192, 24, 16, 16, 32, 56, 2, 4, 12)]
    public void MetricsScaleAcrossSupportedDpiMatrix(
        int dpi,
        int horizontalPadding,
        int verticalPadding,
        int contentSpacing,
        int iconSize,
        int closeButtonSize,
        int borderWidth,
        int focusBorderWidth,
        int radius)
    {
        var actual = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, dpi, -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.HorizontalPadding, Is.EqualTo(horizontalPadding));
            Assert.That(actual.VerticalPadding, Is.EqualTo(verticalPadding));
            Assert.That(actual.ContentSpacing, Is.EqualTo(contentSpacing));
            Assert.That(actual.IconSize, Is.EqualTo(iconSize));
            Assert.That(actual.CloseButtonSize, Is.EqualTo(closeButtonSize));
            Assert.That(actual.BorderWidth, Is.EqualTo(borderWidth));
            Assert.That(actual.FocusBorderWidth, Is.EqualTo(focusBorderWidth));
            Assert.That(actual.Radius, Is.EqualTo(radius));
        }));
    }

    [Test]
    public void ExplicitRadiusIsScaledAndZeroRemainsSquare()
    {
        var square = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 192, 0);
        var explicitRadius = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 192, 10);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(square.Radius, Is.EqualTo(0));
            Assert.That(explicitRadius.Radius, Is.EqualTo(20));
        }));
    }

    [Test]
    public void ResolveMetricsRejectsInvalidArguments()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapAlertRenderLogic.ResolveMetrics(null!, 96, -1)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 0, -1)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -2)));
        }));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void EnabledPaletteUsesOneThemeDerivedFormulaForEveryVariant(BootstrapThemeMode mode)
    {
        var colors = BootstrapTheme.CreateDefault(mode).Colors;

        Assert.Multiple((Action)(() =>
        {
            foreach (var variant in Variants)
            {
                var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
                var expectedSurface = ColorUtil.Blend(semantic, colors.Surface, 0.12f);
                var expectedBorder = ColorUtil.Blend(semantic, colors.Border, 0.45f);
                var candidate = ColorUtil.Blend(semantic, colors.Text, 0.72f);
                var expectedForeground = ColorUtil.GetContrastRatio(candidate, expectedSurface) >= 4.5d
                    ? candidate
                    : colors.Text;

                var actual = BootstrapAlertRenderLogic.ResolvePalette(colors, variant, enabled: true);

                Assert.That(actual.Surface, Is.EqualTo(expectedSurface), $"{mode}/{variant} surface");
                Assert.That(actual.Border, Is.EqualTo(expectedBorder), $"{mode}/{variant} border");
                Assert.That(actual.Foreground, Is.EqualTo(expectedForeground), $"{mode}/{variant} foreground");
                Assert.That(actual.Focus, Is.EqualTo(colors.Focus), $"{mode}/{variant} focus");
            }
        }));
    }

    [Test]
    public void DisabledPaletteIgnoresSemanticVariant()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;

        Assert.Multiple((Action)(() =>
        {
            foreach (var variant in Variants)
            {
                var actual = BootstrapAlertRenderLogic.ResolvePalette(colors, variant, enabled: false);
                Assert.That(actual.Surface, Is.EqualTo(colors.SurfaceSecondary), $"{variant} surface");
                Assert.That(actual.Border, Is.EqualTo(colors.Border), $"{variant} border");
                Assert.That(actual.Foreground, Is.EqualTo(colors.MutedText), $"{variant} foreground");
                Assert.That(actual.Focus, Is.EqualTo(colors.Disabled), $"{variant} focus");
            }
        }));
    }

    [Test]
    public void LayoutWithoutIconOrDismissUsesAllPaddedContentForText()
    {
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var layout = BootstrapAlertRenderLogic.CalculateLayout(new Rectangle(0, 0, 360, 52), metrics, hasIcon: false, dismissible: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.SurfaceBounds, Is.EqualTo(new Rectangle(0, 0, 360, 52)));
            Assert.That(layout.ContentBounds, Is.EqualTo(new Rectangle(12, 8, 336, 36)));
            Assert.That(layout.IconBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(layout.CloseBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(layout.TextBounds, Is.EqualTo(layout.ContentBounds));
            Assert.That(layout.CornerRadius, Is.EqualTo(new CornerRadius(6f)));
        }));
    }

    [Test]
    public void LayoutReservesLeadingIconSlot()
    {
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var layout = BootstrapAlertRenderLogic.CalculateLayout(new Rectangle(0, 0, 360, 52), metrics, hasIcon: true, dismissible: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.IconBounds, Is.EqualTo(new Rectangle(12, 18, 16, 16)));
            Assert.That(layout.CloseBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(layout.TextBounds, Is.EqualTo(new Rectangle(36, 8, 312, 36)));
        }));
    }

    [Test]
    public void LayoutReservesTrailingCloseSlot()
    {
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var layout = BootstrapAlertRenderLogic.CalculateLayout(new Rectangle(0, 0, 360, 52), metrics, hasIcon: false, dismissible: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.IconBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(layout.CloseBounds, Is.EqualTo(new Rectangle(320, 12, 28, 28)));
            Assert.That(layout.TextBounds, Is.EqualTo(new Rectangle(12, 8, 300, 36)));
        }));
    }

    [Test]
    public void LayoutReservesIconAndCloseWithTextBetweenThem()
    {
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var layout = BootstrapAlertRenderLogic.CalculateLayout(new Rectangle(0, 0, 360, 52), metrics, hasIcon: true, dismissible: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(layout.IconBounds, Is.EqualTo(new Rectangle(12, 18, 16, 16)));
            Assert.That(layout.TextBounds, Is.EqualTo(new Rectangle(36, 8, 276, 36)));
            Assert.That(layout.CloseBounds, Is.EqualTo(new Rectangle(320, 12, 28, 28)));
        }));
    }

    [Test]
    public void NarrowAndEmptyLayoutsClampAllRectanglesWithoutThrowing()
    {
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 96, -1);
        var narrow = BootstrapAlertRenderLogic.CalculateLayout(new Rectangle(5, 7, 40, 20), metrics, hasIcon: true, dismissible: true);
        var empty = BootstrapAlertRenderLogic.CalculateLayout(new Rectangle(0, 0, 0, 0), metrics, hasIcon: true, dismissible: true);
        var negative = BootstrapAlertRenderLogic.CalculateLayout(new Rectangle(3, 4, -10, -20), metrics, hasIcon: true, dismissible: true);

        Assert.Multiple((Action)(() =>
        {
            AssertLayoutIsNonNegativeAndContained(narrow);
            AssertLayoutIsNonNegativeAndContained(empty);
            AssertLayoutIsNonNegativeAndContained(negative);
            Assert.That(empty.SurfaceBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(empty.TextBounds, Is.EqualTo(Rectangle.Empty));
        }));
    }

    private static void AssertLayoutIsNonNegativeAndContained(BootstrapAlertLayout layout)
    {
        Assert.That(layout.SurfaceBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.SurfaceBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.ContentBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.ContentBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.IconBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.IconBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.TextBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.TextBounds.Height, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.CloseBounds.Width, Is.GreaterThanOrEqualTo(0));
        Assert.That(layout.CloseBounds.Height, Is.GreaterThanOrEqualTo(0));

        AssertContained(layout.SurfaceBounds, layout.ContentBounds);
        AssertContained(layout.ContentBounds, layout.IconBounds);
        AssertContained(layout.ContentBounds, layout.TextBounds);
        AssertContained(layout.ContentBounds, layout.CloseBounds);
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
