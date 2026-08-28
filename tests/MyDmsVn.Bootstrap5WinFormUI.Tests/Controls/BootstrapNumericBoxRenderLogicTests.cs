using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapNumericBoxRenderLogicTests
{
    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(168)]
    [TestCase(192)]
    public void ResolveMetricsScalesThemeTokens(int dpi)
    {
        var metrics = BootstrapThemeMetrics.Default;

        var actual = BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, dpi, borderRadius: -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.HorizontalPadding, Is.EqualTo(DpiScaler.Scale(metrics.SpacingSM, dpi)));
            Assert.That(actual.BorderWidth, Is.EqualTo(DpiScaler.Scale((float)metrics.BorderWidth, dpi)));
            Assert.That(actual.FocusBorderWidth, Is.EqualTo(DpiScaler.Scale((float)metrics.FocusBorderWidth, dpi)));
            Assert.That(actual.Radius, Is.EqualTo(DpiScaler.Scale((float)metrics.Radius, dpi)));
        }));
    }

    [Test]
    public void ResolveMetricsUsesExplicitRadiusAndRejectsInvalidInputs()
    {
        var metrics = BootstrapThemeMetrics.Default;

        Assert.That(
            BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, 192, 6).Radius,
            Is.EqualTo(DpiScaler.Scale(6f, 192)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, 96, -2)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, 0, -1)));
        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapNumericBoxRenderLogic.ResolveMetrics(null!, 96, -1)));
    }

    [Test]
    public void ResolvePalettePreservesValidationFocusAndReadOnlyPriority()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        var normal = BootstrapNumericBoxRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.None, containsFocus: false, enabled: true, readOnly: false);
        var focused = BootstrapNumericBoxRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.None, containsFocus: true, enabled: true, readOnly: false);
        var valid = BootstrapNumericBoxRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.Valid, containsFocus: true, enabled: true, readOnly: false);
        var invalid = BootstrapNumericBoxRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: true, readOnly: false);
        var readOnly = BootstrapNumericBoxRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.None, containsFocus: true, enabled: true, readOnly: true);
        var disabled = BootstrapNumericBoxRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: false, readOnly: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(normal.Background, Is.EqualTo(colors.Surface));
            Assert.That(normal.Foreground, Is.EqualTo(colors.Text));
            Assert.That(normal.Border, Is.EqualTo(colors.Border));
            Assert.That(focused.Border, Is.EqualTo(colors.Focus));
            Assert.That(valid.Border, Is.EqualTo(colors.Success));
            Assert.That(invalid.Border, Is.EqualTo(colors.Danger));
            Assert.That(readOnly.Background, Is.EqualTo(colors.SurfaceSecondary));
            Assert.That(readOnly.Foreground, Is.EqualTo(colors.Text));
            Assert.That(disabled.Background, Is.EqualTo(colors.SurfaceSecondary));
            Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
            Assert.That(disabled.Border, Is.EqualTo(colors.Disabled));
        }));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapNumericBoxRenderLogic.ResolvePalette(
                colors, (BootstrapValidationState)999, containsFocus: false, enabled: true, readOnly: false)));
    }

    [Test]
    public void CalculateNativeBoundsCentersEditorAndKeepsItInsideShell()
    {
        var metrics = new BootstrapNumericBoxMetrics(
            horizontalPadding: 8,
            borderWidth: 1f,
            focusBorderWidth: 2f,
            radius: 4f);

        var bounds = BootstrapNumericBoxRenderLogic.CalculateNativeBounds(
            new Size(160, 32),
            nativePreferredHeight: 20,
            metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bounds.Left, Is.EqualTo(8));
            Assert.That(bounds.Right, Is.EqualTo(152));
            Assert.That(bounds.Height, Is.EqualTo(20));
            Assert.That(bounds.Top, Is.EqualTo(6));
            Assert.That(new Rectangle(Point.Empty, new Size(160, 32)).Contains(bounds), Is.True);
        }));
    }

    [Test]
    public void CalculateNativeBoundsHandlesTinyAndEmptyClients()
    {
        var metrics = new BootstrapNumericBoxMetrics(8, 1f, 2f, 4f);

        Assert.That(
            BootstrapNumericBoxRenderLogic.CalculateNativeBounds(Size.Empty, 20, metrics),
            Is.EqualTo(Rectangle.Empty));

        var tiny = BootstrapNumericBoxRenderLogic.CalculateNativeBounds(new Size(5, 10), 20, metrics);
        Assert.That(tiny.Width, Is.GreaterThan(0));
        Assert.That(tiny.Height, Is.EqualTo(10));
        Assert.That(new Rectangle(0, 0, 5, 10).Contains(tiny), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapNumericBoxRenderLogic.CalculateNativeBounds(new Size(160, 32), 0, metrics)));
    }
}
