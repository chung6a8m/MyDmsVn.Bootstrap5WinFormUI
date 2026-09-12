using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapRangeRenderLogicTests
{
    [TestCase(BootstrapVariant.Primary)]
    [TestCase(BootstrapVariant.Secondary)]
    [TestCase(BootstrapVariant.Success)]
    [TestCase(BootstrapVariant.Danger)]
    [TestCase(BootstrapVariant.Warning)]
    [TestCase(BootstrapVariant.Info)]
    [TestCase(BootstrapVariant.Light)]
    [TestCase(BootstrapVariant.Dark)]
    public void NormalPaletteUsesSemanticVariantAndNeutralRail(BootstrapVariant variant)
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var palette = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            variant,
            new BootstrapRangeVisualState(enabled: true, focused: false, hot: false, pressed: false));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.RailColor, Is.EqualTo(theme.Colors.Border));
            Assert.That(palette.ThumbColor, Is.EqualTo(BootstrapVariantColorResolver.Resolve(theme.Colors, variant)));
            Assert.That(palette.TickColor, Is.EqualTo(theme.Colors.MutedText));
            Assert.That(palette.DrawFocusHalo, Is.False);
        }));
    }

    [TestCase(BootstrapVariant.Primary)]
    [TestCase(BootstrapVariant.Secondary)]
    [TestCase(BootstrapVariant.Success)]
    [TestCase(BootstrapVariant.Danger)]
    [TestCase(BootstrapVariant.Warning)]
    [TestCase(BootstrapVariant.Info)]
    [TestCase(BootstrapVariant.Light)]
    [TestCase(BootstrapVariant.Dark)]
    public void DarkPaletteUsesCurrentThemeTokens(BootstrapVariant variant)
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var palette = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            variant,
            new BootstrapRangeVisualState(enabled: true, focused: false, hot: false, pressed: false));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.BackgroundColor, Is.EqualTo(theme.Colors.Surface));
            Assert.That(palette.RailColor, Is.EqualTo(theme.Colors.Border));
            Assert.That(palette.ThumbColor, Is.EqualTo(BootstrapVariantColorResolver.Resolve(theme.Colors, variant)));
        }));
    }

    [Test]
    public void DisabledPaletteWinsOverInteractionStates()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var palette = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            BootstrapVariant.Danger,
            new BootstrapRangeVisualState(enabled: false, focused: true, hot: true, pressed: true));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.ThumbColor, Is.EqualTo(theme.Colors.Disabled));
            Assert.That(palette.RailColor, Is.Not.EqualTo(theme.Colors.Border));
            Assert.That(palette.DrawFocusHalo, Is.False);
        }));
    }

    [Test]
    public void FocusedPaletteExposesThemeFocusHalo()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var palette = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            BootstrapVariant.Primary,
            new BootstrapRangeVisualState(enabled: true, focused: true, hot: false, pressed: false));

        Assert.That(palette.DrawFocusHalo, Is.True);
        Assert.That(palette.FocusColor, Is.EqualTo(theme.Colors.Focus));
    }

    [Test]
    public void HoverAndPressedThumbTreatmentsAreDistinctWithPressedPrecedence()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var normal = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            BootstrapVariant.Primary,
            new BootstrapRangeVisualState(true, false, false, false));
        var hot = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            BootstrapVariant.Primary,
            new BootstrapRangeVisualState(true, false, true, false));
        var pressed = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            BootstrapVariant.Primary,
            new BootstrapRangeVisualState(true, false, true, true));
        var pressedWithoutHot = BootstrapRangeRenderLogic.ResolvePalette(
            theme.Colors,
            BootstrapVariant.Primary,
            new BootstrapRangeVisualState(true, false, false, true));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(hot.ThumbColor, Is.Not.EqualTo(normal.ThumbColor));
            Assert.That(pressed.ThumbColor, Is.Not.EqualTo(hot.ThumbColor));
            Assert.That(pressed.ThumbColor, Is.EqualTo(pressedWithoutHot.ThumbColor));
        }));
    }

    [Test]
    public void RailGeometryCentersFrameworkThicknessInsideNativeHorizontalRectangle()
    {
        var nativeBounds = new Rectangle(11, 17, 101, 13);
        var geometry = BootstrapRangeRenderLogic.CalculateGeometry(
            nativeBounds,
            new Rectangle(40, 10, 11, 21),
            Orientation.Horizontal,
            BootstrapThemeMetrics.Default,
            96,
            drawFocusHalo: false);

        Assert.That(geometry.RailBounds, Is.EqualTo(new Rectangle(11, 21, 101, 4)));
        Assert.That(geometry.ThumbBounds, Is.EqualTo(new Rectangle(41, 16, 9, 9)));
    }

    [Test]
    public void RailGeometryCentersFrameworkThicknessInsideNativeVerticalRectangle()
    {
        var geometry = BootstrapRangeRenderLogic.CalculateGeometry(
            new Rectangle(17, 11, 13, 101),
            new Rectangle(10, 40, 21, 11),
            Orientation.Vertical,
            BootstrapThemeMetrics.Default,
            96,
            drawFocusHalo: false);

        Assert.That(geometry.RailBounds, Is.EqualTo(new Rectangle(21, 11, 4, 101)));
        Assert.That(geometry.ThumbBounds, Is.EqualTo(new Rectangle(16, 41, 9, 9)));
    }

    [Test]
    public void TinyNativeRectanglesClampWithoutNegativeGeometry()
    {
        var geometry = BootstrapRangeRenderLogic.CalculateGeometry(
            new Rectangle(5, 6, 1, 1),
            new Rectangle(8, 9, 1, 1),
            Orientation.Horizontal,
            BootstrapThemeMetrics.Default,
            192,
            drawFocusHalo: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(geometry.RailBounds, Is.EqualTo(new Rectangle(5, 6, 1, 1)));
            Assert.That(geometry.ThumbBounds, Is.EqualTo(new Rectangle(8, 9, 1, 1)));
            Assert.That(geometry.FocusHaloBounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(geometry.FocusHaloBounds.Height, Is.GreaterThanOrEqualTo(0));
        }));
    }

    [TestCase(96, 4, 2f, 1f)]
    [TestCase(144, 6, 3f, 1.5f)]
    [TestCase(192, 8, 4f, 2f)]
    public void OnlyFrameworkMetricsScaleWithDpi(int dpi, int railThickness, float focusThickness, float tickThickness)
    {
        var nativeChannel = new Rectangle(10, 20, 100, 20);
        var nativeThumb = new Rectangle(30, 15, 20, 30);
        var geometry = BootstrapRangeRenderLogic.CalculateGeometry(
            nativeChannel,
            nativeThumb,
            Orientation.Horizontal,
            BootstrapThemeMetrics.Default,
            dpi,
            drawFocusHalo: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(geometry.RailBounds.X, Is.EqualTo(nativeChannel.X));
            Assert.That(geometry.RailBounds.Width, Is.EqualTo(nativeChannel.Width));
            Assert.That(geometry.RailBounds.Height, Is.EqualTo(railThickness));
            Assert.That(geometry.FocusThickness, Is.EqualTo(focusThickness));
            Assert.That(geometry.TickThickness, Is.EqualTo(tickThickness));
        }));
    }

    [TestCase(1UL, (int)BootstrapRangeNativePart.Ticks)]
    [TestCase(2UL, (int)BootstrapRangeNativePart.Thumb)]
    [TestCase(3UL, (int)BootstrapRangeNativePart.Channel)]
    [TestCase(0UL, (int)BootstrapRangeNativePart.Unknown)]
    [TestCase(99UL, (int)BootstrapRangeNativePart.Unknown)]
    public void NativePartClassificationKeepsUnknownValuesNative(ulong value, int expected)
    {
        Assert.That(BootstrapRangeNativeMethods.ClassifyPart(new UIntPtr(value)), Is.EqualTo((BootstrapRangeNativePart)expected));
    }
}
