using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapBadgeRenderLogicTests
{
    [TestCase(BootstrapVariant.Primary)]
    [TestCase(BootstrapVariant.Secondary)]
    [TestCase(BootstrapVariant.Success)]
    [TestCase(BootstrapVariant.Danger)]
    [TestCase(BootstrapVariant.Warning)]
    [TestCase(BootstrapVariant.Info)]
    [TestCase(BootstrapVariant.Light)]
    [TestCase(BootstrapVariant.Dark)]
    public void SemanticPaletteUsesSharedVariantAndContrastResolvers(BootstrapVariant variant)
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var expectedBackground = BootstrapVariantColorResolver.Resolve(colors, variant);

        var palette = BootstrapBadgeRenderLogic.ResolvePalette(colors, variant, Color.Empty, enabled: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.EqualTo(expectedBackground));
            Assert.That(
                palette.Foreground,
                Is.EqualTo(ColorUtil.GetContrastingTextColor(expectedBackground, colors.Light, colors.Dark)));
        }));
    }

    [Test]
    public void CustomColorOverridesSemanticVariantAndStillUsesContrastForeground()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark).Colors;
        var custom = Color.FromArgb(245, 210, 55);

        var palette = BootstrapBadgeRenderLogic.ResolvePalette(colors, BootstrapVariant.Danger, custom, enabled: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.EqualTo(custom));
            Assert.That(
                palette.Foreground,
                Is.EqualTo(ColorUtil.GetContrastingTextColor(custom, colors.Light, colors.Dark)));
        }));
    }

    [Test]
    public void DisabledPaletteUsesMutedForegroundAndSoftenedSurface()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var semantic = BootstrapVariantColorResolver.Resolve(colors, BootstrapVariant.Success);

        var palette = BootstrapBadgeRenderLogic.ResolvePalette(colors, BootstrapVariant.Success, Color.Empty, enabled: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.Not.EqualTo(semantic));
            Assert.That(palette.Foreground, Is.EqualTo(colors.MutedText));
        }));
    }

    [TestCase(96, 8, 4)]
    [TestCase(120, 10, 5)]
    [TestCase(144, 12, 6)]
    [TestCase(168, 14, 7)]
    [TestCase(192, 16, 8)]
    public void PaddingScalesAcrossSupportedDpiMatrix(int dpi, int expectedHorizontal, int expectedVertical)
    {
        var padding = BootstrapBadgeRenderLogic.GetPadding(BootstrapThemeMetrics.Default, dpi);

        Assert.That(padding, Is.EqualTo(new Padding(expectedHorizontal, expectedVertical, expectedHorizontal, expectedVertical)));
    }

    [TestCase(0, 14, 16, 22)]
    [TestCase(24, 14, 40, 22)]
    [TestCase(160, 14, 176, 22)]
    public void PreferredSizeAddsScaledPaddingForEmptyShortAndLongText(
        int textWidth,
        int textHeight,
        int expectedWidth,
        int expectedHeight)
    {
        var preferred = BootstrapBadgeRenderLogic.GetPreferredSize(
            new Size(textWidth, textHeight),
            BootstrapThemeMetrics.Default,
            DpiScaler.DefaultDpi);

        Assert.That(preferred, Is.EqualTo(new Size(expectedWidth, expectedHeight)));
    }

    [Test]
    public void PillRadiusUsesHalfPhysicalHeight()
    {
        var radius = BootstrapBadgeRenderLogic.GetRadius(
            physicalHeight: 24,
            BootstrapThemeMetrics.Default,
            pill: true,
            borderRadius: -1,
            dpi: DpiScaler.DefaultDpi);

        Assert.That(radius, Is.EqualTo(12f));
    }

    [Test]
    public void ThemeAndExplicitRadiusUseLogicalDpiScaling()
    {
        var themeRadius = BootstrapBadgeRenderLogic.GetRadius(
            physicalHeight: 40,
            BootstrapThemeMetrics.Default,
            pill: false,
            borderRadius: -1,
            dpi: 192);
        var explicitRadius = BootstrapBadgeRenderLogic.GetRadius(
            physicalHeight: 40,
            BootstrapThemeMetrics.Default,
            pill: false,
            borderRadius: 10,
            dpi: 192);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(themeRadius, Is.EqualTo(12f));
            Assert.That(explicitRadius, Is.EqualTo(20f));
        }));
    }
}
