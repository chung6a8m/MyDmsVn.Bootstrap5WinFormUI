using System;
using System.Drawing;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSpinnerTests
{
    [Test]
    public void DefaultsMatchPhase5Contract()
    {
        using var spinner = new BootstrapSpinner();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(spinner.Type, Is.EqualTo(BootstrapSpinnerType.Border));
            Assert.That(spinner.SpinnerSize, Is.EqualTo(BootstrapSpinnerSize.Default));
            Assert.That(spinner.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(spinner.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(spinner.AnimationDuration, Is.EqualTo(TimeSpan.FromMilliseconds(750)));
            Assert.That(spinner.Spinning, Is.True);
            Assert.That(spinner.TabStop, Is.False);
        }));
    }

    [Test]
    public void StartAndStopUpdateSpinningState()
    {
        using var spinner = new BootstrapSpinner();

        spinner.Stop();
        Assert.That(spinner.Spinning, Is.False);

        spinner.Start();
        Assert.That(spinner.Spinning, Is.True);
    }

    [Test]
    public void AnimationDurationRejectsNonPositiveValues()
    {
        using var spinner = new BootstrapSpinner();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => spinner.AnimationDuration = TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => spinner.AnimationDuration = TimeSpan.FromMilliseconds(-1)));
    }

    [Test]
    public void SemanticVariantsResolveThroughThemeTokens()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Primary, Color.Empty), Is.EqualTo(colors.Primary));
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Secondary, Color.Empty), Is.EqualTo(colors.Secondary));
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Success, Color.Empty), Is.EqualTo(colors.Success));
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Danger, Color.Empty), Is.EqualTo(colors.Danger));
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Warning, Color.Empty), Is.EqualTo(colors.Warning));
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Info, Color.Empty), Is.EqualTo(colors.Info));
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Light, Color.Empty), Is.EqualTo(colors.Light));
            Assert.That(BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Dark, Color.Empty), Is.EqualTo(colors.Dark));
        }));
    }

    [Test]
    public void CustomColorOverridesSemanticVariant()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);
        var customColor = Color.Magenta;

        var resolved = BootstrapSpinnerRenderLogic.ResolveColor(colors, BootstrapVariant.Danger, customColor);

        Assert.That(resolved, Is.EqualTo(customColor));
    }

    [Test]
    public void ResolveColorRejectsNullThemeEvenWithCustomColor()
    {
        Assert.Throws<ArgumentNullException>((Action)(() =>
            BootstrapSpinnerRenderLogic.ResolveColor(null!, BootstrapVariant.Primary, Color.Magenta)));
    }

    [Test]
    public void LogicalDiameterUsesExistingThemeMetrics()
    {
        var metrics = BootstrapThemeMetrics.Default;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapSpinnerRenderLogic.GetLogicalDiameter(metrics, BootstrapSpinnerSize.Small), Is.EqualTo(metrics.SpacingLG));
            Assert.That(BootstrapSpinnerRenderLogic.GetLogicalDiameter(metrics, BootstrapSpinnerSize.Default), Is.EqualTo(metrics.SpacingXL));
            Assert.That(BootstrapSpinnerRenderLogic.GetLogicalDiameter(metrics, BootstrapSpinnerSize.Large), Is.EqualTo(metrics.ControlHeight));
        }));
    }

    [Test]
    public void GrowScaleHasVisibleStableFrameAtZeroProgress()
    {
        var zero = BootstrapSpinnerRenderLogic.GetGrowScale(0.0);
        var middle = BootstrapSpinnerRenderLogic.GetGrowScale(0.5);
        var end = BootstrapSpinnerRenderLogic.GetGrowScale(1.0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(zero, Is.GreaterThan(0.0));
            Assert.That(zero, Is.LessThan(1.0));
            Assert.That(middle, Is.EqualTo(1.0).Within(0.000001));
            Assert.That(end, Is.EqualTo(zero).Within(0.000001));
        }));
    }
}
