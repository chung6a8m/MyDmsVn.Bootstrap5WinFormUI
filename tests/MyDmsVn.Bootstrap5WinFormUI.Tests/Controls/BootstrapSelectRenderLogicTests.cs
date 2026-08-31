using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectRenderLogicTests
{
    [TestCase(96, 1f, 2f, 8f)]
    [TestCase(120, 1.25f, 2.5f, 10f)]
    [TestCase(144, 1.5f, 3f, 12f)]
    [TestCase(192, 2f, 4f, 16f)]
    public void ResolveMetricsScalesBorderFocusAndExplicitRadius(
        int dpi,
        float expectedBorder,
        float expectedFocus,
        float expectedRadius)
    {
        var metrics = BootstrapSelectRenderLogic.ResolveMetrics(
            new Size(340, 40),
            BootstrapThemeMetrics.Default,
            dpi,
            borderRadius: 8,
            containsFocus: false);
        var focused = BootstrapSelectRenderLogic.ResolveMetrics(
            new Size(340, 40),
            BootstrapThemeMetrics.Default,
            dpi,
            borderRadius: 8,
            containsFocus: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.BorderWidth, Is.EqualTo(expectedBorder).Within(0.001f));
            Assert.That(focused.BorderWidth, Is.EqualTo(expectedFocus).Within(0.001f));
            Assert.That(metrics.Radius, Is.EqualTo(expectedRadius).Within(0.001f));
        }));
    }

    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(192)]
    public void ResolveMetricsInsetsPathByHalfOfActualStroke(int dpi)
    {
        var clientSize = new Size(340, 40);
        var metrics = BootstrapSelectRenderLogic.ResolveMetrics(
            clientSize,
            BootstrapThemeMetrics.Default,
            dpi,
            borderRadius: -1,
            containsFocus: true);
        var expectedInset = metrics.BorderWidth / 2f;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.BorderBounds.Left, Is.EqualTo(expectedInset).Within(0.001f));
            Assert.That(metrics.BorderBounds.Top, Is.EqualTo(expectedInset).Within(0.001f));
            Assert.That(metrics.BorderBounds.Right, Is.EqualTo(clientSize.Width - expectedInset).Within(0.001f));
            Assert.That(metrics.BorderBounds.Bottom, Is.EqualTo(clientSize.Height - expectedInset).Within(0.001f));
        }));
    }

    [Test]
    public void ResolveMetricsRejectsInvalidInputs()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                () => BootstrapSelectRenderLogic.ResolveMetrics(new Size(10, 10), null!, 96, -1, false),
                Throws.ArgumentNullException);
            Assert.That(
                () => BootstrapSelectRenderLogic.ResolveMetrics(new Size(10, 10), BootstrapThemeMetrics.Default, 0, -1, false),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => BootstrapSelectRenderLogic.ResolveMetrics(new Size(10, 10), BootstrapThemeMetrics.Default, 96, -2, false),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }));
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(-10, 5)]
    [TestCase(5, -10)]
    public void ResolveMetricsClampsMalformedClientGeometry(int width, int height)
    {
        var metrics = BootstrapSelectRenderLogic.ResolveMetrics(
            new Size(width, height),
            BootstrapThemeMetrics.Default,
            192,
            borderRadius: -1,
            containsFocus: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.BorderBounds.Width, Is.GreaterThanOrEqualTo(0f));
            Assert.That(metrics.BorderBounds.Height, Is.GreaterThanOrEqualTo(0f));
        }));
    }
}
