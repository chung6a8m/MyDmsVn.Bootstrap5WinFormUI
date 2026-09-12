using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToolStripTests
{
    private BootstrapTheme _originalTheme = null!;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown() => BootstrapThemeManager.CurrentTheme = _originalTheme;

    [Test]
    public void PublicContractIsThinNativeToolStrip()
    {
        var managerRenderer = ToolStripManager.Renderer;
        using var strip = new BootstrapToolStrip();
        var button = new ToolStripButton("Action");
        strip.Items.Add(button);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapToolStrip).BaseType, Is.EqualTo(typeof(ToolStrip)));
            Assert.That(strip.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(strip.Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
            Assert.That(strip.Items[0], Is.SameAs(button));
            Assert.That(ToolStripManager.Renderer, Is.SameAs(managerRenderer));
        }));
    }

    [Test]
    public void ThemeUpdatesFrameworkFontButPreservesCallerFont()
    {
        using var strip = new BootstrapToolStrip();
        var originalFrameworkFont = strip.Font;
        BootstrapThemeManager.CurrentTheme = ThemeWithBodyFont("Arial", 11f);
        Assert.That(strip.Font, Is.Not.SameAs(originalFrameworkFont));

        using var callerFont = new Font("Tahoma", 13f);
        strip.Font = callerFont;
        BootstrapThemeManager.CurrentTheme = ThemeWithBodyFont("Segoe UI", 9f);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(strip.Font, Is.SameAs(callerFont));
            Assert.DoesNotThrow((Action)(() => _ = callerFont.Height));
        }));
    }

    [Test]
    public void CallerRendererSurvivesThemeAndVariantChanges()
    {
        using var strip = new BootstrapToolStrip();
        var renderer = new SentinelRenderer();
        strip.Renderer = renderer;

        strip.Variant = BootstrapVariant.Danger;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        Assert.That(strip.Renderer, Is.SameAs(renderer));
    }

    private static BootstrapTheme ThemeWithBodyFont(string family, float size)
    {
        var current = BootstrapThemeManager.CurrentTheme;
        var typography = new BootstrapThemeTypography(
            new BootstrapFontToken(family, size),
            current.Typography.BodySmall,
            current.Typography.Label,
            current.Typography.HeadingSmall,
            current.Typography.HeadingMedium);
        return new BootstrapTheme(current.Mode, current.Colors, current.Metrics, typography, current.ReducedMotion);
    }

    private sealed class SentinelRenderer : ToolStripRenderer
    {
    }
}
