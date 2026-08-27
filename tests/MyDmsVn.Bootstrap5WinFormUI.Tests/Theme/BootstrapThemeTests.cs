using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Theme;

[TestFixture]
public sealed class BootstrapThemeTests
{
    [Test]
    public void CreateDefaultLightUsesDocumentedSemanticPalette()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(theme.Mode, Is.EqualTo(BootstrapThemeMode.Light));
            Assert.That(theme.Colors.Primary, Is.EqualTo(Color.FromArgb(0x0D, 0x6E, 0xFD)));
            Assert.That(theme.Colors.Secondary, Is.EqualTo(Color.FromArgb(0x6C, 0x75, 0x7D)));
            Assert.That(theme.Colors.Success, Is.EqualTo(Color.FromArgb(0x19, 0x87, 0x54)));
            Assert.That(theme.Colors.Danger, Is.EqualTo(Color.FromArgb(0xDC, 0x35, 0x45)));
            Assert.That(theme.Colors.Warning, Is.EqualTo(Color.FromArgb(0xFF, 0xC1, 0x07)));
            Assert.That(theme.Colors.Info, Is.EqualTo(Color.FromArgb(0x0D, 0xCA, 0xF0)));
            Assert.That(theme.Colors.Light, Is.EqualTo(Color.FromArgb(0xF8, 0xF9, 0xFA)));
            Assert.That(theme.Colors.Dark, Is.EqualTo(Color.FromArgb(0x21, 0x25, 0x29)));
        }));
    }

    [Test]
    public void CreateDefaultDarkUsesIndependentDarkSurfacePalette()
    {
        var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(dark.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
            Assert.That(dark.Colors.Body, Is.EqualTo(Color.FromArgb(0x21, 0x25, 0x29)));
            Assert.That(dark.Colors.Surface, Is.EqualTo(Color.FromArgb(0x2B, 0x30, 0x35)));
            Assert.That(dark.Colors.Text, Is.EqualTo(Color.FromArgb(0xF8, 0xF9, 0xFA)));
            Assert.That(dark.Colors.Primary, Is.EqualTo(Color.FromArgb(0x6E, 0xA8, 0xFE)));
            Assert.That(dark.Colors.Body, Is.Not.EqualTo(light.Colors.Body));
            Assert.That(dark.Colors.Border, Is.Not.EqualTo(light.Colors.Border));
        }));
    }

    [Test]
    public void DefaultMetricsMatchDesignSystemBaseline()
    {
        var metrics = BootstrapThemeMetrics.Default;

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(metrics.ControlHeightSmall, Is.EqualTo(28));
            Assert.That(metrics.ControlHeight, Is.EqualTo(32));
            Assert.That(metrics.ControlHeightLarge, Is.EqualTo(38));
            Assert.That(metrics.RadiusSmall, Is.EqualTo(4));
            Assert.That(metrics.Radius, Is.EqualTo(6));
            Assert.That(metrics.RadiusLarge, Is.EqualTo(8));
            Assert.That(metrics.BorderWidth, Is.EqualTo(1));
            Assert.That(metrics.FocusBorderWidth, Is.EqualTo(2));
            Assert.That(metrics.SpacingXS, Is.EqualTo(4));
            Assert.That(metrics.SpacingSM, Is.EqualTo(8));
            Assert.That(metrics.SpacingMD, Is.EqualTo(12));
            Assert.That(metrics.SpacingLG, Is.EqualTo(16));
            Assert.That(metrics.SpacingXL, Is.EqualTo(24));
        }));
    }

    [Test]
    public void DefaultTypographyUsesSegoeUiRoles()
    {
        var typography = BootstrapThemeTypography.Default;

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(typography.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
            Assert.That(typography.Body.SizeInPoints, Is.EqualTo(9f));
            Assert.That(typography.BodySmall.SizeInPoints, Is.LessThan(typography.Body.SizeInPoints));
            Assert.That(typography.Label.Style, Is.EqualTo(FontStyle.Bold));
            Assert.That(typography.HeadingSmall.SizeInPoints, Is.GreaterThan(typography.Body.SizeInPoints));
            Assert.That(typography.HeadingMedium.SizeInPoints, Is.GreaterThan(typography.HeadingSmall.SizeInPoints));
        }));
    }

    [Test]
    public void CreateDefaultCarriesReducedMotionPreference()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: true);

        Assert.That(theme.ReducedMotion, Is.True);
    }
}
