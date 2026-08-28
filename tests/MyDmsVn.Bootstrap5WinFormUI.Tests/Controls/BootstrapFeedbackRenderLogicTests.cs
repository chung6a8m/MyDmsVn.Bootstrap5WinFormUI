using System;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapFeedbackRenderLogicTests
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

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void SharedPalettePreservesTheStage2AlertFormula(BootstrapThemeMode mode)
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

                var shared = BootstrapFeedbackRenderLogic.ResolvePalette(colors, variant, enabled: true);
                var alert = BootstrapAlertRenderLogic.ResolvePalette(colors, variant, enabled: true);

                Assert.That(shared.Surface, Is.EqualTo(expectedSurface), $"{mode}/{variant} surface");
                Assert.That(shared.Border, Is.EqualTo(expectedBorder), $"{mode}/{variant} border");
                Assert.That(shared.Foreground, Is.EqualTo(expectedForeground), $"{mode}/{variant} foreground");
                Assert.That(shared.Focus, Is.EqualTo(colors.Focus), $"{mode}/{variant} focus");
                Assert.That(alert.Surface, Is.EqualTo(shared.Surface), $"{mode}/{variant} alert surface");
                Assert.That(alert.Border, Is.EqualTo(shared.Border), $"{mode}/{variant} alert border");
                Assert.That(alert.Foreground, Is.EqualTo(shared.Foreground), $"{mode}/{variant} alert foreground");
                Assert.That(alert.Focus, Is.EqualTo(shared.Focus), $"{mode}/{variant} alert focus");
            }
        }));
    }

    [Test]
    public void SharedDisabledPaletteIgnoresSemanticVariant()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark).Colors;

        foreach (var variant in Variants)
        {
            var actual = BootstrapFeedbackRenderLogic.ResolvePalette(colors, variant, enabled: false);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(actual.Surface, Is.EqualTo(colors.SurfaceSecondary), $"{variant} surface");
                Assert.That(actual.Border, Is.EqualTo(colors.Border), $"{variant} border");
                Assert.That(actual.Foreground, Is.EqualTo(colors.MutedText), $"{variant} foreground");
                Assert.That(actual.Focus, Is.EqualTo(colors.Disabled), $"{variant} focus");
            }));
        }
    }

    [Test]
    public void SharedResolverRejectsInvalidInputs()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;

        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentNullException>((Action)(() => BootstrapFeedbackRenderLogic.ResolvePalette(null!, BootstrapVariant.Primary, true)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapFeedbackRenderLogic.ResolvePalette(colors, (BootstrapVariant)999, true)));
        }));
    }
}
