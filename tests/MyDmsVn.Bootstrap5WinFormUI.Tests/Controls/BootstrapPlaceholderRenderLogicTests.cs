using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapPlaceholderRenderLogicTests
{
    [TestCase(96, 100)]
    [TestCase(144, 150)]
    [TestCase(192, 200)]
    public void PreferredWidthScalesOneHundredLogicalPixels(int dpi, int expectedWidth)
    {
        var actual = BootstrapPlaceholderRenderLogic.GetPreferredSize(20, BootstrapPlaceholderSize.Default, dpi, Size.Empty);

        Assert.That(actual.Width, Is.EqualTo(expectedWidth));
    }

    [TestCase(40, 40)]
    [TestCase(100, 100)]
    [TestCase(140, 100)]
    public void ProposedPositiveWidthCapsIntrinsicWidth(int proposedWidth, int expectedWidth)
    {
        var actual = BootstrapPlaceholderRenderLogic.GetPreferredSize(
            20,
            BootstrapPlaceholderSize.Default,
            96,
            new Size(proposedWidth, 999));

        Assert.That(actual.Width, Is.EqualTo(expectedWidth));
    }

    [TestCase(BootstrapPlaceholderSize.ExtraSmall, 0.6)]
    [TestCase(BootstrapPlaceholderSize.Small, 0.8)]
    [TestCase(BootstrapPlaceholderSize.Default, 1.0)]
    [TestCase(BootstrapPlaceholderSize.Large, 1.2)]
    public void PreferredHeightMatchesBootstrapEmScale(BootstrapPlaceholderSize size, double multiplier)
    {
        var actual = BootstrapPlaceholderRenderLogic.GetPreferredSize(20, size, 96, Size.Empty);

        Assert.That(actual.Height, Is.EqualTo((int)Math.Ceiling(20.0 * multiplier)));
    }

    [Test]
    public void PreferredHeightHasOnePixelMinimum()
    {
        var actual = BootstrapPlaceholderRenderLogic.GetPreferredSize(1, BootstrapPlaceholderSize.ExtraSmall, 96, Size.Empty);

        Assert.That(actual.Height, Is.EqualTo(1));
    }

    [Test]
    public void PreferredSizeRejectsInvalidInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetPreferredSize(0, BootstrapPlaceholderSize.Default, 96, Size.Empty)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetPreferredSize(20, (BootstrapPlaceholderSize)99, 96, Size.Empty)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetPreferredSize(20, BootstrapPlaceholderSize.Default, 0, Size.Empty)));
    }

    [TestCase(BootstrapVariant.Primary)]
    [TestCase(BootstrapVariant.Secondary)]
    [TestCase(BootstrapVariant.Success)]
    [TestCase(BootstrapVariant.Danger)]
    [TestCase(BootstrapVariant.Warning)]
    [TestCase(BootstrapVariant.Info)]
    [TestCase(BootstrapVariant.Light)]
    [TestCase(BootstrapVariant.Dark)]
    public void BaseColorUsesSharedSemanticVariantResolver(BootstrapVariant variant)
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        var actual = BootstrapPlaceholderRenderLogic.ResolveBaseColor(colors, variant, Color.Empty, enabled: true);

        Assert.That(actual, Is.EqualTo(BootstrapVariantColorResolver.Resolve(colors, variant)));
    }

    [Test]
    public void BaseColorPrecedenceIsDisabledThenCustomThenSemantic()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Dark);
        var custom = Color.MediumPurple;

        Assert.Multiple((Action)delegate
        {
            Assert.That(
                BootstrapPlaceholderRenderLogic.ResolveBaseColor(colors, BootstrapVariant.Danger, custom, enabled: false),
                Is.EqualTo(colors.Disabled));
            Assert.That(
                BootstrapPlaceholderRenderLogic.ResolveBaseColor(colors, BootstrapVariant.Danger, custom, enabled: true),
                Is.EqualTo(custom));
        });
    }

    [Test]
    public void BaseColorRejectsInvalidInputs()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapPlaceholderRenderLogic.ResolveBaseColor(null!, BootstrapVariant.Primary, Color.Empty, true)));
        Assert.Throws<ArgumentException>((Action)(() => BootstrapPlaceholderRenderLogic.ResolveBaseColor(colors, BootstrapVariant.Primary, Color.FromArgb(128, 1, 2, 3), true)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.ResolveBaseColor(colors, (BootstrapVariant)99, Color.Empty, true)));
    }

    [TestCase(0.00, 0.50)]
    [TestCase(0.25, 0.35)]
    [TestCase(0.50, 0.20)]
    [TestCase(0.75, 0.35)]
    [TestCase(1.00, 0.50)]
    public void GlowOpacityMatchesBootstrapCycle(double progress, double expected)
    {
        Assert.That(BootstrapPlaceholderRenderLogic.GetGlowOpacity(progress), Is.EqualTo(expected).Within(0.000001));
    }

    [Test]
    public void GlowClampsFiniteProgressAndRejectsNonFiniteProgress()
    {
        Assert.Multiple((Action)delegate
        {
            Assert.That(BootstrapPlaceholderRenderLogic.GetGlowOpacity(-1), Is.EqualTo(0.5).Within(0.000001));
            Assert.That(BootstrapPlaceholderRenderLogic.GetGlowOpacity(2), Is.EqualTo(0.5).Within(0.000001));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetGlowOpacity(double.NaN)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetGlowOpacity(double.PositiveInfinity)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetGlowOpacity(double.NegativeInfinity)));
        });
    }

    [Test]
    public void WaveProgressZeroKeepsVisiblePositionsAtBaseOpacity()
    {
        foreach (var position in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            Assert.That(BootstrapPlaceholderRenderLogic.GetWaveOpacity(position, 0), Is.EqualTo(0.5).Within(0.000001));
        }
    }

    [Test]
    public void WaveUsesBootstrapMaskTroughRatherThanGlowMinimum()
    {
        var center = BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.5, 0.5);

        Assert.Multiple((Action)delegate
        {
            Assert.That(BootstrapPlaceholderRenderLogic.OpacityMax * BootstrapPlaceholderRenderLogic.WaveMaskMin, Is.EqualTo(0.4).Within(0.000001));
            Assert.That(center, Is.EqualTo(0.4).Within(0.000001));
            Assert.That(center, Is.Not.EqualTo(BootstrapPlaceholderRenderLogic.OpacityMin).Within(0.000001));
            Assert.That(BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.1, 0.5), Is.EqualTo(0.5).Within(0.000001));
        });
    }

    [Test]
    public void WaveIsSymmetricAndAlwaysStaysWithinEffectiveOpacityRange()
    {
        Assert.That(
            BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.4, 0.5),
            Is.EqualTo(BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.6, 0.5)).Within(0.000001));

        for (var progressIndex = 0; progressIndex <= 20; progressIndex++)
        {
            for (var positionIndex = 0; positionIndex <= 20; positionIndex++)
            {
                var actual = BootstrapPlaceholderRenderLogic.GetWaveOpacity(positionIndex / 20.0, progressIndex / 20.0);
                Assert.That(actual, Is.InRange(0.4, 0.5));
            }
        }
    }

    [Test]
    public void WaveClampsFiniteInputsAndRejectsNonFiniteInputs()
    {
        Assert.Multiple((Action)delegate
        {
            Assert.That(BootstrapPlaceholderRenderLogic.GetWaveOpacity(-1, 0.5), Is.EqualTo(BootstrapPlaceholderRenderLogic.GetWaveOpacity(0, 0.5)));
            Assert.That(BootstrapPlaceholderRenderLogic.GetWaveOpacity(2, 0.5), Is.EqualTo(BootstrapPlaceholderRenderLogic.GetWaveOpacity(1, 0.5)));
            Assert.That(BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.5, -1), Is.EqualTo(BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.5, 0)));
            Assert.That(BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.5, 2), Is.EqualTo(BootstrapPlaceholderRenderLogic.GetWaveOpacity(0.5, 1)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetWaveOpacity(double.NaN, 0)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetWaveOpacity(0, double.PositiveInfinity)));
        });
    }

    [TestCase(0.5, 128)]
    [TestCase(0.4, 102)]
    [TestCase(0.2, 51)]
    public void ApplyOpacityUsesExpectedAlphaAndPreservesRgb(double opacity, int expectedAlpha)
    {
        var actual = BootstrapPlaceholderRenderLogic.ApplyOpacity(Color.FromArgb(255, 12, 34, 56), opacity);

        Assert.That(actual, Is.EqualTo(Color.FromArgb(expectedAlpha, 12, 34, 56)));
    }

    [Test]
    public void ApplyOpacityClampsFiniteValuesAndRejectsNonFiniteValues()
    {
        var color = Color.CornflowerBlue;

        Assert.Multiple((Action)delegate
        {
            Assert.That(BootstrapPlaceholderRenderLogic.ApplyOpacity(color, -1).A, Is.Zero);
            Assert.That(BootstrapPlaceholderRenderLogic.ApplyOpacity(color, 2).A, Is.EqualTo(255));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.ApplyOpacity(color, double.NaN)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.ApplyOpacity(color, double.NegativeInfinity)));
        });
    }

    [TestCase(0, 96, 0f)]
    [TestCase(8, 96, 8f)]
    [TestCase(8, 144, 12f)]
    [TestCase(8, 192, 16f)]
    public void RadiusScalesLogicalValues(int logicalRadius, int dpi, float expected)
    {
        Assert.That(BootstrapPlaceholderRenderLogic.GetRadius(BootstrapThemeMetrics.Default, logicalRadius, dpi), Is.EqualTo(expected));
    }

    [Test]
    public void ThemeRadiusUsesCurrentThemeMetric()
    {
        Assert.That(
            BootstrapPlaceholderRenderLogic.GetRadius(BootstrapThemeMetrics.Default, -1, 192),
            Is.EqualTo(BootstrapThemeMetrics.Default.Radius * 2f));
    }

    [Test]
    public void RadiusRejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapPlaceholderRenderLogic.GetRadius(null!, 0, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetRadius(BootstrapThemeMetrics.Default, -2, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapPlaceholderRenderLogic.GetRadius(BootstrapThemeMetrics.Default, 0, 0)));
    }
}
