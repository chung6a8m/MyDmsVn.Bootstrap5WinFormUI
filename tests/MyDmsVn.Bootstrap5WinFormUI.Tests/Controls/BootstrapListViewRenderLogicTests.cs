using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapListViewRenderLogicTests
{
    [TestCase(false, false, false, false, false, (int)BootstrapListViewItemVisualState.Disabled)]
    [TestCase(true, true, true, false, true, (int)BootstrapListViewItemVisualState.SelectedActive)]
    [TestCase(true, true, false, false, true, (int)BootstrapListViewItemVisualState.SelectedInactive)]
    [TestCase(true, true, false, true, true, (int)BootstrapListViewItemVisualState.Hovered)]
    [TestCase(true, false, true, false, true, (int)BootstrapListViewItemVisualState.Hovered)]
    [TestCase(true, false, true, false, false, (int)BootstrapListViewItemVisualState.Neutral)]
    public void ResolveStateAppliesDocumentedPrecedence(
        bool enabled,
        bool selected,
        bool focused,
        bool hideSelection,
        bool hovered,
        int expected)
    {
        Assert.That(
            BootstrapListViewRenderLogic.ResolveState(enabled, selected, focused, hideSelection, hovered),
            Is.EqualTo((BootstrapListViewItemVisualState)expected));
    }

    [TestCase(View.Details, true, 1, true)]
    [TestCase(View.List, true, 3, true)]
    [TestCase(View.Details, true, 2, false)]
    [TestCase(View.List, false, 1, false)]
    [TestCase(View.SmallIcon, true, 1, false)]
    [TestCase(View.LargeIcon, true, 1, false)]
    [TestCase(View.Tile, true, 1, false)]
    public void ShouldUseStripeIsLimitedToOddRowOrientedItems(
        View view,
        bool striped,
        int index,
        bool expected)
    {
        Assert.That(BootstrapListViewRenderLogic.ShouldUseStripe(view, striped, index), Is.EqualTo(expected));
    }

    [Test]
    public void EffectiveOverrideUsesObservableArgbDifference()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapListViewRenderLogic.HasEffectiveColorOverride(Color.Red, Color.White), Is.True);
            Assert.That(BootstrapListViewRenderLogic.HasEffectiveColorOverride(Color.White, Color.White), Is.False);
            Assert.That(
                BootstrapListViewRenderLogic.HasEffectiveColorOverride(Color.FromArgb(255, 1, 2, 3), Color.FromArgb(255, 1, 2, 3)),
                Is.False);
        }));
    }

    [Test]
    public void PaletteUsesVariantSelectionContrastAndDistinctInactiveSelection()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var accent = BootstrapVariantColorResolver.Resolve(theme.Colors, BootstrapVariant.Success);
        var active = Resolve(theme, BootstrapListViewItemVisualState.SelectedActive);
        var inactive = Resolve(theme, BootstrapListViewItemVisualState.SelectedInactive);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(active.BackColor, Is.EqualTo(accent));
            Assert.That(active.ForeColor, Is.EqualTo(ColorUtil.GetContrastingTextColor(accent, theme.Colors.Light, theme.Colors.Dark)));
            Assert.That(inactive.BackColor, Is.Not.EqualTo(active.BackColor));
            Assert.That(inactive.ForeColor, Is.EqualTo(theme.Colors.Text));
        }));
    }

    [Test]
    public void NeutralPaletteHonorsCallerColorsBeforeStripe()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        var palette = BootstrapListViewRenderLogic.ResolvePalette(
            theme,
            BootstrapVariant.Primary,
            BootstrapListViewItemVisualState.Neutral,
            striped: true,
            hasCallerBackColor: true,
            callerBackColor: Color.MistyRose,
            hasCallerForeColor: true,
            callerForeColor: Color.Maroon);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(palette.BackColor, Is.EqualTo(Color.MistyRose));
            Assert.That(palette.ForeColor, Is.EqualTo(Color.Maroon));
        }));
    }

    [Test]
    public void DisabledPaletteIgnoresCallerAndUsesMutedText()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var palette = BootstrapListViewRenderLogic.ResolvePalette(
            theme,
            BootstrapVariant.Danger,
            BootstrapListViewItemVisualState.Disabled,
            striped: true,
            hasCallerBackColor: true,
            callerBackColor: Color.Red,
            hasCallerForeColor: true,
            callerForeColor: Color.Yellow);

        Assert.That(palette.ForeColor, Is.EqualTo(theme.Colors.MutedText));
        Assert.That(palette.BackColor, Is.Not.EqualTo(Color.Red));
    }

    private static BootstrapListViewItemPalette Resolve(
        BootstrapTheme theme,
        BootstrapListViewItemVisualState state)
    {
        return BootstrapListViewRenderLogic.ResolvePalette(
            theme,
            BootstrapVariant.Success,
            state,
            striped: false,
            hasCallerBackColor: false,
            callerBackColor: Color.Empty,
            hasCallerForeColor: false,
            callerForeColor: Color.Empty);
    }
}
