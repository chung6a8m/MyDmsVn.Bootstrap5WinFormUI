using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapModalRenderLogicTests
{
    [TestCase(96, 1, 48, 56, 32)]
    [TestCase(192, 2, 96, 112, 64)]
    public void MetricsScaleFromThemeTokens(int dpi, int border, int header, int footer, int closeTarget)
    {
        var metrics = BootstrapModalLayoutLogic.ResolveMetrics(BootstrapThemeMetrics.Default, -1, dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.BorderWidth, Is.EqualTo(border));
            Assert.That(metrics.HeaderHeight, Is.EqualTo(header));
            Assert.That(metrics.FooterHeight, Is.EqualTo(footer));
            Assert.That(metrics.CloseTargetSize, Is.EqualTo(closeTarget));
        }));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void VisualStateUsesSemanticThemeColors(BootstrapThemeMode mode)
    {
        var theme = BootstrapTheme.CreateDefault(mode);

        var state = BootstrapModalRenderLogic.Resolve(theme);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(state.Surface, Is.EqualTo(theme.Colors.Surface));
            Assert.That(state.Text, Is.EqualTo(theme.Colors.Text));
            Assert.That(state.Border, Is.EqualTo(theme.Colors.Border));
            Assert.That(state.Backdrop, Is.EqualTo(theme.Colors.Dark));
            Assert.That(state.BackdropOpacity, Is.GreaterThan(0d).And.LessThan(1d));
        }));
    }
}
