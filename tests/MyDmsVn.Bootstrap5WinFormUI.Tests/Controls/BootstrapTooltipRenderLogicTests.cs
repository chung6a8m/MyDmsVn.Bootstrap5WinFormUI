using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapTooltipRenderLogicTests
{
    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void SemanticVariantsResolveThroughSharedVariantResolver(BootstrapThemeMode mode)
    {
        var colors = BootstrapThemeColors.CreateDefault(mode);
        var variants = Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>();

        foreach (var variant in variants)
        {
            var palette = BootstrapTooltipRenderLogic.ResolvePalette(colors, variant, Color.Empty);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(palette.Background, Is.EqualTo(BootstrapVariantColorResolver.Resolve(colors, variant)), variant.ToString());
                Assert.That(palette.Border, Is.EqualTo(colors.Border), variant.ToString());
                Assert.That(
                    palette.Foreground,
                    Is.EqualTo(ColorUtil.GetContrastingTextColor(palette.Background, colors.Light, colors.Dark)),
                    variant.ToString());
            }));
        }
    }

    [Test]
    public void CustomColorOverridesSemanticBackgroundAndRetainsContrastSelection()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);
        var custom = Color.FromArgb(111, 66, 193);

        var palette = BootstrapTooltipRenderLogic.ResolvePalette(colors, BootstrapVariant.Warning, custom);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.EqualTo(custom));
            Assert.That(palette.Border, Is.EqualTo(colors.Border));
            Assert.That(palette.Foreground, Is.EqualTo(ColorUtil.GetContrastingTextColor(custom, colors.Light, colors.Dark)));
        }));
    }

    [Test]
    public void ResolvePaletteValidatesInputsBeforeReturningCustomPresentation()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapTooltipRenderLogic.ResolvePalette(null!, BootstrapVariant.Dark, Color.Empty)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTooltipRenderLogic.ResolvePalette(colors, (BootstrapVariant)99, Color.Red)));
    }

    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(168)]
    [TestCase(192)]
    public void ResolveMetricsScalesPaddingBorderAndThemeRadiusAcrossDpiMatrix(int dpi)
    {
        var themeMetrics = BootstrapThemeMetrics.Default;
        var logicalPadding = new Padding(themeMetrics.SpacingSM, themeMetrics.SpacingXS, themeMetrics.SpacingSM, themeMetrics.SpacingXS);

        var metrics = BootstrapTooltipRenderLogic.ResolveMetrics(themeMetrics, logicalPadding, -1, dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.Padding, Is.EqualTo(DpiScaler.Scale(logicalPadding, dpi)));
            Assert.That(metrics.BorderWidth, Is.EqualTo(DpiScaler.Scale(themeMetrics.BorderWidth, dpi)));
            Assert.That(metrics.Radius, Is.EqualTo(DpiScaler.Scale(themeMetrics.Radius, dpi)));
        }));
    }

    [Test]
    public void ResolveMetricsSupportsExplicitRadiusAndRejectsInvalidInputs()
    {
        var metrics = BootstrapThemeMetrics.Default;
        var padding = new Padding(8, 4, 8, 4);

        var resolved = BootstrapTooltipRenderLogic.ResolveMetrics(metrics, padding, 0, 144);

        Assert.That(resolved.Radius, Is.Zero);
        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapTooltipRenderLogic.ResolveMetrics(null!, padding, -1, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTooltipRenderLogic.ResolveMetrics(metrics, new Padding(-1, 0, 0, 0), -1, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTooltipRenderLogic.ResolveMetrics(metrics, padding, -2, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapTooltipRenderLogic.ResolveMetrics(metrics, padding, -1, 0)));
    }

    [Test]
    public void PopupSizeAddsScaledPaddingAndBorderWithoutWrappingPolicy()
    {
        var metrics = new BootstrapTooltipRenderMetrics(new Padding(8, 4, 8, 4), 1, 6);

        var result = BootstrapTooltipRenderLogic.CalculatePopupSize(new Size(40, 10), metrics);

        Assert.That(result, Is.EqualTo(new Size(58, 20)));
    }

    [Test]
    public void PopupSizeClampsNegativeTextAndSaturatesOverflow()
    {
        var metrics = new BootstrapTooltipRenderMetrics(new Padding(int.MaxValue, 0, int.MaxValue, 0), int.MaxValue, 0);

        var result = BootstrapTooltipRenderLogic.CalculatePopupSize(new Size(-5, int.MaxValue), metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.Width, Is.EqualTo(int.MaxValue));
            Assert.That(result.Height, Is.EqualTo(int.MaxValue));
        }));
    }

    [Test]
    public void TextBoundsInsetBorderAndAsymmetricContentPadding()
    {
        var metrics = new BootstrapTooltipRenderMetrics(new Padding(8, 4, 6, 2), 1, 6);

        var result = BootstrapTooltipRenderLogic.CalculateTextBounds(new Rectangle(10, 20, 100, 30), metrics);

        Assert.That(result, Is.EqualTo(new Rectangle(19, 25, 84, 22)));
    }

    [Test]
    public void TextBoundsClampTinyAndMalformedOuterBoundsWithoutNegativeGeometry()
    {
        var metrics = new BootstrapTooltipRenderMetrics(new Padding(8, 4, 8, 4), 1, 6);

        var tiny = BootstrapTooltipRenderLogic.CalculateTextBounds(new Rectangle(5, 7, 3, 2), metrics);
        var malformed = BootstrapTooltipRenderLogic.CalculateTextBounds(new Rectangle(5, 7, -3, -2), metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tiny.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(tiny.Height, Is.GreaterThanOrEqualTo(0));
            Assert.That(malformed.Width, Is.Zero);
            Assert.That(malformed.Height, Is.Zero);
        }));
    }
}
