using System;
using System.Drawing;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapButtonTests
{
    [Test]
    public void DefaultsMatchPhase6Contract()
    {
        using var button = new BootstrapButton();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(button.Outline, Is.False);
            Assert.That(button.ButtonSize, Is.EqualTo(BootstrapButtonSize.Default));
            Assert.That(button.Icon, Is.Null);
            Assert.That(button.IconPosition, Is.EqualTo(BootstrapIconPosition.Left));
            Assert.That(button.BorderRadius, Is.EqualTo(-1));
            Assert.That(button.Loading, Is.False);
            Assert.That(button.LoadingText, Is.EqualTo(string.Empty));
            Assert.That(button.Selected, Is.False);
            Assert.That(button.TabStop, Is.True);
        }));
    }

    [Test]
    public void LoadingSuppressesPerformClick()
    {
        using var button = new BootstrapButton();
        var clickCount = 0;
        button.Click += (_, _) => clickCount++;

        button.PerformClick();
        button.Loading = true;
        button.PerformClick();

        Assert.That(clickCount, Is.EqualTo(1));
    }

    [Test]
    public void PreferredSizeDoesNotChangeWhenLoadingToggles()
    {
        using var button = new BootstrapButton
        {
            Text = "Save",
            LoadingText = "Saving changes...",
            AutoSize = true
        };

        var normal = button.GetPreferredSize(Size.Empty);
        button.Loading = true;
        var loading = button.GetPreferredSize(Size.Empty);

        Assert.That(loading, Is.EqualTo(normal));
    }

    [Test]
    public void ButtonSizesUseThemeControlHeights()
    {
        var metrics = BootstrapThemeMetrics.Default;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapButtonRenderLogic.GetLogicalHeight(metrics, BootstrapButtonSize.Small), Is.EqualTo(metrics.ControlHeightSmall));
            Assert.That(BootstrapButtonRenderLogic.GetLogicalHeight(metrics, BootstrapButtonSize.Default), Is.EqualTo(metrics.ControlHeight));
            Assert.That(BootstrapButtonRenderLogic.GetLogicalHeight(metrics, BootstrapButtonSize.Large), Is.EqualTo(metrics.ControlHeightLarge));
        }));
    }

    [Test]
    public void OutlineNormalStateUsesVariantForBorderAndForeground()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        var palette = BootstrapButtonRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Success,
            outline: true,
            enabled: true,
            selected: false,
            BootstrapButtonVisualState.Normal);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Border, Is.EqualTo(colors.Success));
            Assert.That(palette.Foreground, Is.EqualTo(colors.Success));
            Assert.That(palette.Background, Is.EqualTo(colors.Surface));
        }));
    }

    [Test]
    public void SelectedOutlineStateBecomesFilled()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        var palette = BootstrapButtonRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Warning,
            outline: true,
            enabled: true,
            selected: true,
            BootstrapButtonVisualState.Normal);

        Assert.That(palette.Background, Is.Not.EqualTo(colors.Surface));
        Assert.That(palette.Border, Is.EqualTo(palette.Background));
    }

    [Test]
    public void DisabledPaletteUsesDisabledThemeTokens()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Dark);

        var palette = BootstrapButtonRenderLogic.ResolvePalette(
            colors,
            BootstrapVariant.Danger,
            outline: false,
            enabled: false,
            selected: false,
            BootstrapButtonVisualState.Normal);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Foreground, Is.EqualTo(colors.MutedText));
            Assert.That(palette.Border, Is.EqualTo(colors.Disabled));
        }));
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var button = new BootstrapButton();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => button.BorderRadius = -2));
        Assert.DoesNotThrow((Action)(() => button.BorderRadius = -1));
        Assert.DoesNotThrow((Action)(() => button.BorderRadius = 0));
    }

    [Test]
    public void GroupCornerRadiusOverrideDoesNotChangePublicRadius()
    {
        using var button = new BootstrapButton { BorderRadius = 12 };
        var groupRadius = new CornerRadius(12f, 0f, 0f, 12f);

        button.GroupCornerRadius = groupRadius;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.BorderRadius, Is.EqualTo(12));
            Assert.That(button.GetEffectiveCornerRadius(BootstrapThemeMetrics.Default), Is.EqualTo(groupRadius));
        }));
    }
}
