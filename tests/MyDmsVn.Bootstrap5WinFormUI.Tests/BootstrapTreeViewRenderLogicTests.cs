using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests;

[TestFixture]
public sealed class BootstrapTreeViewRenderLogicTests
{
    [Test]
    public void SelectedEnabledNode_UsesVariantFillAndContrastingText()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var state = new BootstrapTreeNodeVisualState(selected: true, hot: false, enabled: true);

        var palette = BootstrapTreeViewRenderLogic.ResolvePalette(colors, BootstrapVariant.Warning, state);
        var variantColor = BootstrapVariantColorResolver.Resolve(colors, BootstrapVariant.Warning);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.EqualTo(variantColor));
            Assert.That(palette.Foreground, Is.EqualTo(ColorUtil.GetContrastingTextColor(variantColor, colors.Light, colors.Dark)));
            Assert.That(palette.AccentBorder, Is.EqualTo(variantColor));
        }));
    }

    [Test]
    public void HotEnabledNode_UsesNeutralHoverSurfaceAndNormalText()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var state = new BootstrapTreeNodeVisualState(selected: false, hot: true, enabled: true);

        var palette = BootstrapTreeViewRenderLogic.ResolvePalette(colors, BootstrapVariant.Danger, state);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.EqualTo(colors.Hover));
            Assert.That(palette.Foreground, Is.EqualTo(colors.Text));
            Assert.That(palette.AccentBorder, Is.EqualTo(Color.Transparent));
        }));
    }

    [Test]
    public void DisabledNode_TakesPrecedenceOverSelectedAndHotStates()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var state = new BootstrapTreeNodeVisualState(selected: true, hot: true, enabled: false);

        var palette = BootstrapTreeViewRenderLogic.ResolvePalette(colors, BootstrapVariant.Danger, state);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.EqualTo(colors.Surface));
            Assert.That(palette.Background, Is.Not.EqualTo(colors.Danger));
            Assert.That(palette.Foreground, Is.EqualTo(colors.MutedText));
            Assert.That(palette.AccentBorder, Is.EqualTo(Color.Transparent));
        }));
    }

    [Test]
    public void NormalNode_UsesNeutralSurfaceAndThemeText()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var state = new BootstrapTreeNodeVisualState(selected: false, hot: false, enabled: true);

        var palette = BootstrapTreeViewRenderLogic.ResolvePalette(colors, BootstrapVariant.Primary, state);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.Background, Is.EqualTo(colors.Surface));
            Assert.That(palette.Foreground, Is.EqualTo(colors.Text));
            Assert.That(palette.AccentBorder, Is.EqualTo(Color.Transparent));
        }));
    }

    [Test]
    public void DarkTheme_NormalNode_RemainsNeutralInsteadOfUsingVariantFill()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark).Colors;
        var state = new BootstrapTreeNodeVisualState(selected: false, hot: false, enabled: true);

        var palette = BootstrapTreeViewRenderLogic.ResolvePalette(colors, BootstrapVariant.Primary, state);

        Assert.That(palette.Background, Is.EqualTo(colors.Surface));
        Assert.That(palette.Background, Is.Not.EqualTo(colors.Primary));
    }

    [Test]
    public void ResolvePalette_DoesNotDependOnControlState()
    {
        var method = typeof(BootstrapTreeViewRenderLogic).GetMethod(
            nameof(BootstrapTreeViewRenderLogic.ResolvePalette),
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        foreach (var parameter in method!.GetParameters())
        {
            Assert.That(
                typeof(Control).IsAssignableFrom(parameter.ParameterType),
                Is.False,
                $"ResolvePalette must receive pure state, not {parameter.ParameterType.Name}.");
        }
    }
}
