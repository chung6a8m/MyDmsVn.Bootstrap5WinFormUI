using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapDatePickerRenderLogicTests
{
    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(168)]
    [TestCase(192)]
    public void ResolveMetricsScalesThemeTokens(int dpi)
    {
        var metrics = BootstrapThemeMetrics.Default;

        var actual = BootstrapDatePickerRenderLogic.ResolveMetrics(metrics, dpi, -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(actual.ShellPadding, Is.EqualTo(DpiScaler.Scale(metrics.SpacingXS, dpi)));
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
            BootstrapDatePickerRenderLogic.ResolveMetrics(metrics, 192, 6).Radius,
            Is.EqualTo(DpiScaler.Scale(6f, 192)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapDatePickerRenderLogic.ResolveMetrics(metrics, 96, -2)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapDatePickerRenderLogic.ResolveMetrics(metrics, 0, -1)));
        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapDatePickerRenderLogic.ResolveMetrics(null!, 96, -1)));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void ResolvePalettePreservesEstablishedInputPriority(BootstrapThemeMode mode)
    {
        var colors = BootstrapThemeColors.CreateDefault(mode);

        var neutral = BootstrapDatePickerRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.None, containsFocus: false, enabled: true);
        var focused = BootstrapDatePickerRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.None, containsFocus: true, enabled: true);
        var valid = BootstrapDatePickerRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.Valid, containsFocus: true, enabled: true);
        var invalid = BootstrapDatePickerRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: true);
        var disabled = BootstrapDatePickerRenderLogic.ResolvePalette(
            colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(neutral.Surface, Is.EqualTo(colors.Surface));
            Assert.That(neutral.Foreground, Is.EqualTo(colors.Text));
            Assert.That(neutral.Border, Is.EqualTo(colors.Border));
            Assert.That(focused.Border, Is.EqualTo(colors.Focus));
            Assert.That(valid.Border, Is.EqualTo(colors.Success));
            Assert.That(invalid.Border, Is.EqualTo(colors.Danger));
            Assert.That(disabled.Surface, Is.EqualTo(colors.SurfaceSecondary));
            Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
            Assert.That(disabled.Border, Is.EqualTo(colors.Disabled));
        }));
    }

    [Test]
    public void ResolvePaletteRejectsInvalidStateAndNullColors()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapDatePickerRenderLogic.ResolvePalette(
                colors, (BootstrapValidationState)999, containsFocus: false, enabled: true)));
        Assert.Throws<ArgumentNullException>((Action)(() =>
            BootstrapDatePickerRenderLogic.ResolvePalette(
                null!, BootstrapValidationState.None, containsFocus: false, enabled: true)));
    }

    [Test]
    public void CalculateNativeBoundsUsesShellPaddingAndCentersNativePicker()
    {
        var metrics = new BootstrapDatePickerMetrics(
            shellPadding: 4,
            borderWidth: 1f,
            focusBorderWidth: 2f,
            radius: 6f);

        var bounds = BootstrapDatePickerRenderLogic.CalculateNativeBounds(
            new Size(240, 32),
            nativePreferredHeight: 22,
            metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bounds, Is.EqualTo(new Rectangle(4, 5, 232, 22)));
            Assert.That(new Rectangle(Point.Empty, new Size(240, 32)).Contains(bounds), Is.True);
        }));
    }

    [Test]
    public void CalculateNativeBoundsClampsNarrowAndTinyClients()
    {
        var metrics = new BootstrapDatePickerMetrics(8, 1f, 2f, 6f);

        var narrow = BootstrapDatePickerRenderLogic.CalculateNativeBounds(new Size(9, 32), 22, metrics);
        var tiny = BootstrapDatePickerRenderLogic.CalculateNativeBounds(new Size(5, 6), 22, metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(narrow.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(narrow.Height, Is.GreaterThanOrEqualTo(0));
            Assert.That(new Rectangle(0, 0, 9, 32).Contains(narrow), Is.True);
            Assert.That(tiny.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(tiny.Height, Is.GreaterThanOrEqualTo(0));
            Assert.That(new Rectangle(0, 0, 5, 6).Contains(tiny), Is.True);
        }));
    }

    [Test]
    public void CalculateNativeBoundsHandlesEmptyClientAndRejectsInvalidPreferredHeight()
    {
        var metrics = new BootstrapDatePickerMetrics(4, 1f, 2f, 6f);

        Assert.That(
            BootstrapDatePickerRenderLogic.CalculateNativeBounds(Size.Empty, 22, metrics),
            Is.EqualTo(Rectangle.Empty));
        Assert.That(
            BootstrapDatePickerRenderLogic.CalculateNativeBounds(new Size(-1, 20), 22, metrics),
            Is.EqualTo(Rectangle.Empty));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapDatePickerRenderLogic.CalculateNativeBounds(new Size(240, 32), 0, metrics)));
    }

    [Test]
    public void CalculateNativeBoundsUsesScaledMetricsAtTwoHundredPercent()
    {
        var themeMetrics = BootstrapThemeMetrics.Default;
        var metrics = BootstrapDatePickerRenderLogic.ResolveMetrics(themeMetrics, 192, -1);
        var client = new Size(480, 64);

        var bounds = BootstrapDatePickerRenderLogic.CalculateNativeBounds(client, 44, metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bounds.Left, Is.EqualTo(metrics.ShellPadding));
            Assert.That(bounds.Right, Is.EqualTo(client.Width - metrics.ShellPadding));
            Assert.That(bounds.Height, Is.LessThanOrEqualTo(44));
            Assert.That(new Rectangle(Point.Empty, client).Contains(bounds), Is.True);
        }));
    }
}
