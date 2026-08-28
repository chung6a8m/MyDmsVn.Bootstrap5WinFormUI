using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapComboBoxRenderLogicTests
{
    [TestCase(96, 8, 4, 16, 4, 32, 1f, 2f, 6f)]
    [TestCase(120, 10, 5, 20, 5, 40, 1.25f, 2.5f, 7.5f)]
    [TestCase(144, 12, 6, 24, 6, 48, 1.5f, 3f, 9f)]
    [TestCase(168, 14, 7, 28, 7, 56, 1.75f, 3.5f, 10.5f)]
    [TestCase(192, 16, 8, 32, 8, 64, 2f, 4f, 12f)]
    public void ResolveMetricsScalesThemeTokensAndKeepsFontHeightPhysical(
        int dpi,
        int horizontalPadding,
        int verticalPadding,
        int iconSize,
        int iconGap,
        int controlHeight,
        float borderWidth,
        float focusBorderWidth,
        float radius)
    {
        var fontHeight = DpiScaler.Scale(15, dpi);

        var metrics = BootstrapComboBoxRenderLogic.ResolveMetrics(
            BootstrapThemeMetrics.Default,
            fontHeight,
            dpi,
            borderRadius: -1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.HorizontalPadding, Is.EqualTo(horizontalPadding));
            Assert.That(metrics.VerticalPadding, Is.EqualTo(verticalPadding));
            Assert.That(metrics.IconSize, Is.EqualTo(iconSize));
            Assert.That(metrics.IconGap, Is.EqualTo(iconGap));
            Assert.That(metrics.ItemHeight, Is.EqualTo(Math.Max(controlHeight, fontHeight + (verticalPadding * 2))));
            Assert.That(metrics.BorderWidth, Is.EqualTo(borderWidth).Within(0.001f));
            Assert.That(metrics.FocusBorderWidth, Is.EqualTo(focusBorderWidth).Within(0.001f));
            Assert.That(metrics.Radius, Is.EqualTo(radius).Within(0.001f));
        }));
    }

    [Test]
    public void ResolveMetricsHonorsExplicitRadiusAndRejectsInvalidArguments()
    {
        var metrics = BootstrapComboBoxRenderLogic.ResolveMetrics(
            BootstrapThemeMetrics.Default,
            fontHeight: 15,
            dpi: 144,
            borderRadius: 8);

        Assert.That(metrics.Radius, Is.EqualTo(12f).Within(0.001f));
        Assert.Throws<ArgumentNullException>((Action)(() =>
            BootstrapComboBoxRenderLogic.ResolveMetrics(null!, 15, 96, -1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapComboBoxRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 0, 96, -1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapComboBoxRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 15, 0, -1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapComboBoxRenderLogic.ResolveMetrics(BootstrapThemeMetrics.Default, 15, 96, -2)));
    }

    [Test]
    public void ResolvePaletteUsesEstablishedValidationFocusDisabledAndSelectionTokens()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        var normal = BootstrapComboBoxRenderLogic.ResolvePalette(
            colors,
            BootstrapValidationState.None,
            containsFocus: false,
            enabled: true);
        var focused = BootstrapComboBoxRenderLogic.ResolvePalette(
            colors,
            BootstrapValidationState.None,
            containsFocus: true,
            enabled: true);
        var valid = BootstrapComboBoxRenderLogic.ResolvePalette(
            colors,
            BootstrapValidationState.Valid,
            containsFocus: true,
            enabled: true);
        var invalid = BootstrapComboBoxRenderLogic.ResolvePalette(
            colors,
            BootstrapValidationState.Invalid,
            containsFocus: true,
            enabled: true);
        var disabled = BootstrapComboBoxRenderLogic.ResolvePalette(
            colors,
            BootstrapValidationState.Invalid,
            containsFocus: true,
            enabled: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(normal.Background, Is.EqualTo(colors.Surface));
            Assert.That(normal.Foreground, Is.EqualTo(colors.Text));
            Assert.That(normal.Border, Is.EqualTo(colors.Border));
            Assert.That(normal.SelectedBackground, Is.EqualTo(colors.Primary));
            Assert.That(
                normal.SelectedForeground,
                Is.EqualTo(ColorUtil.GetContrastingTextColor(colors.Primary, colors.Light, colors.Dark)));

            Assert.That(focused.Border, Is.EqualTo(colors.Focus));
            Assert.That(valid.Border, Is.EqualTo(colors.Success));
            Assert.That(invalid.Border, Is.EqualTo(colors.Danger));

            Assert.That(disabled.Background, Is.EqualTo(colors.SurfaceSecondary));
            Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
            Assert.That(disabled.Border, Is.EqualTo(colors.Disabled));
            Assert.That(disabled.SelectedBackground, Is.EqualTo(colors.SurfaceSecondary));
            Assert.That(disabled.SelectedForeground, Is.EqualTo(colors.MutedText));
        }));
    }

    [Test]
    public void ResolvePaletteRejectsInvalidStateAndNullColors()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        Assert.Throws<ArgumentNullException>((Action)(() =>
            BootstrapComboBoxRenderLogic.ResolvePalette(null!, BootstrapValidationState.None, false, true)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapComboBoxRenderLogic.ResolvePalette(colors, (BootstrapValidationState)999, false, true)));
    }

    [Test]
    public void CalculateItemLayoutReservesIconOnlyWhenRequestedAndKeepsGeometryContained()
    {
        var metrics = BootstrapComboBoxRenderLogic.ResolveMetrics(
            BootstrapThemeMetrics.Default,
            fontHeight: 15,
            dpi: 96,
            borderRadius: -1);
        var bounds = new Rectangle(10, 20, 180, 32);

        var withoutIcon = BootstrapComboBoxRenderLogic.CalculateItemLayout(
            bounds,
            metrics,
            showLeadingIcon: false,
            trailingReserve: 24);
        var withIcon = BootstrapComboBoxRenderLogic.CalculateItemLayout(
            bounds,
            metrics,
            showLeadingIcon: true,
            trailingReserve: 24);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(withoutIcon.IconBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(bounds.Contains(withoutIcon.TextBounds), Is.True);
            Assert.That(bounds.Contains(withIcon.IconBounds), Is.True);
            Assert.That(bounds.Contains(withIcon.TextBounds), Is.True);
            Assert.That(withIcon.IconBounds.Width, Is.EqualTo(metrics.IconSize));
            Assert.That(withIcon.IconBounds.Height, Is.EqualTo(metrics.IconSize));
            Assert.That(withIcon.TextBounds.Left, Is.GreaterThan(withIcon.IconBounds.Right));
            Assert.That(withIcon.TextBounds.Right, Is.LessThanOrEqualTo(bounds.Right - metrics.HorizontalPadding - 24));
            Assert.That(withoutIcon.TextBounds.Width, Is.GreaterThan(withIcon.TextBounds.Width));
        }));
    }

    [Test]
    public void CalculateItemLayoutClampsTinyAndMalformedBoundsWithoutNegativeRectangles()
    {
        var metrics = BootstrapComboBoxRenderLogic.ResolveMetrics(
            BootstrapThemeMetrics.Default,
            fontHeight: 15,
            dpi: 96,
            borderRadius: -1);

        var tiny = BootstrapComboBoxRenderLogic.CalculateItemLayout(
            new Rectangle(0, 0, 6, 4),
            metrics,
            showLeadingIcon: true,
            trailingReserve: 20);
        var empty = BootstrapComboBoxRenderLogic.CalculateItemLayout(
            Rectangle.Empty,
            metrics,
            showLeadingIcon: true,
            trailingReserve: 0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tiny.IconBounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(tiny.IconBounds.Height, Is.GreaterThanOrEqualTo(0));
            Assert.That(tiny.TextBounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(tiny.TextBounds.Height, Is.GreaterThanOrEqualTo(0));
            Assert.That(empty.IconBounds, Is.EqualTo(Rectangle.Empty));
            Assert.That(empty.TextBounds, Is.EqualTo(Rectangle.Empty));
        }));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapComboBoxRenderLogic.CalculateItemLayout(
                new Rectangle(0, 0, 100, 30),
                metrics,
                showLeadingIcon: false,
                trailingReserve: -1)));
    }
}
