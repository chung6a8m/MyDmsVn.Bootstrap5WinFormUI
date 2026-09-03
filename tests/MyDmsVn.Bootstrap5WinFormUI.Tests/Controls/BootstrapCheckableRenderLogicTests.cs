using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapCheckableRenderLogicTests
{
    [TestCase(96, 16, 8, 1, 2, 32)]
    [TestCase(120, 20, 10, 1, 3, 40)]
    [TestCase(144, 24, 12, 2, 3, 48)]
    [TestCase(168, 28, 14, 2, 4, 56)]
    [TestCase(192, 32, 16, 2, 4, 64)]
    public void MetricsScaleFromThemeTokens(int dpi, int indicator, int gap, int border, int focus, int switchWidth)
    {
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.Switch, BootstrapThemeMetrics.Default, dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.IndicatorSize, Is.EqualTo(indicator));
            Assert.That(metrics.TextGap, Is.EqualTo(gap));
            Assert.That(metrics.BorderWidth, Is.EqualTo(border));
            Assert.That(metrics.FocusWidth, Is.EqualTo(focus));
            Assert.That(metrics.IndicatorBoundsSize, Is.EqualTo(new Size(switchWidth, indicator)));
            Assert.That(metrics.Radius, Is.EqualTo(indicator / 2f));
        }));
    }

    [Test]
    public void CheckAndRadioCharacteristicRadiiAreClampedToIndicator()
    {
        var check = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, BootstrapThemeMetrics.Default, 96);
        var radio = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.RadioButton, BootstrapThemeMetrics.Default, 96);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(check.Radius, Is.EqualTo(4f));
            Assert.That(radio.Radius, Is.EqualTo(8f));
        }));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void PaletteAppliesNeutralVariantValidationDisabledAndFocusPrecedence(BootstrapThemeMode mode)
    {
        var colors = BootstrapTheme.CreateDefault(mode).Colors;
        var neutral = BootstrapCheckableRenderLogic.ResolvePalette(colors, BootstrapVariant.Warning, BootstrapValidationState.None, CheckState.Unchecked, true);
        var active = BootstrapCheckableRenderLogic.ResolvePalette(colors, BootstrapVariant.Warning, BootstrapValidationState.None, CheckState.Checked, true);
        var valid = BootstrapCheckableRenderLogic.ResolvePalette(colors, BootstrapVariant.Warning, BootstrapValidationState.Valid, CheckState.Unchecked, true);
        var invalid = BootstrapCheckableRenderLogic.ResolvePalette(colors, BootstrapVariant.Warning, BootstrapValidationState.Invalid, CheckState.Indeterminate, true);
        var disabled = BootstrapCheckableRenderLogic.ResolvePalette(colors, BootstrapVariant.Danger, BootstrapValidationState.Invalid, CheckState.Checked, false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(neutral.Border, Is.EqualTo(colors.Border));
            Assert.That(neutral.Text, Is.EqualTo(colors.Text));
            Assert.That(active.Fill, Is.EqualTo(colors.Warning));
            Assert.That(valid.Border, Is.EqualTo(colors.Success));
            Assert.That(valid.Text, Is.EqualTo(colors.Success));
            Assert.That(invalid.Fill, Is.EqualTo(colors.Danger));
            Assert.That(invalid.Text, Is.EqualTo(colors.Danger));
            Assert.That(disabled.Border, Is.EqualTo(colors.Disabled));
            Assert.That(disabled.Text, Is.EqualTo(colors.MutedText));
            Assert.That(disabled.Focus, Is.EqualTo(colors.Focus));
        }));
    }

    [TestCase(BootstrapVariant.Primary)]
    [TestCase(BootstrapVariant.Secondary)]
    [TestCase(BootstrapVariant.Success)]
    [TestCase(BootstrapVariant.Danger)]
    [TestCase(BootstrapVariant.Warning)]
    [TestCase(BootstrapVariant.Info)]
    [TestCase(BootstrapVariant.Light)]
    [TestCase(BootstrapVariant.Dark)]
    public void CheckedFillUsesEverySemanticVariant(BootstrapVariant variant)
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        var palette = BootstrapCheckableRenderLogic.ResolvePalette(colors, variant, BootstrapValidationState.None, CheckState.Checked, true);
        Assert.That(palette.Fill, Is.EqualTo(BootstrapVariantColorResolver.Resolve(colors, variant)));
    }

    [TestCase(CheckState.Unchecked, false, 2)]
    [TestCase(CheckState.Checked, false, 18)]
    [TestCase(CheckState.Indeterminate, false, 10)]
    [TestCase(CheckState.Unchecked, true, 18)]
    [TestCase(CheckState.Checked, true, 2)]
    [TestCase(CheckState.Indeterminate, true, 10)]
    public void SwitchThumbUsesActualCheckStateAndMirrorsOnlyWithinTrack(CheckState state, bool rtl, int expectedX)
    {
        var thumb = BootstrapCheckableRenderLogic.GetSwitchThumbBounds(new Rectangle(0, 0, 32, 16), 2, state, rtl);
        Assert.That(thumb, Is.EqualTo(new Rectangle(expectedX, 2, 12, 12)));
    }

    [TestCase(ContentAlignment.MiddleLeft, false, true)]
    [TestCase(ContentAlignment.MiddleRight, false, false)]
    [TestCase(ContentAlignment.MiddleLeft, true, false)]
    [TestCase(ContentAlignment.MiddleRight, true, true)]
    public void NativeCompatibleSlotMirrorsCheckAlignExactlyOnce(ContentAlignment align, bool rtl, bool expectedLeft)
    {
        Assert.That(BootstrapCheckableRenderLogic.IsIndicatorOnLeft(align, rtl), Is.EqualTo(expectedLeft));
    }

    [Test]
    public void LayoutAndPreferredSizeStayContainedForTinyAndNormalBounds()
    {
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, BootstrapThemeMetrics.Default, 96);
        var tiny = BootstrapCheckableRenderLogic.GetLayout(new Rectangle(0, 0, 3, 2), Padding.Empty, metrics, ContentAlignment.MiddleLeft, false);
        var normal = BootstrapCheckableRenderLogic.GetLayout(new Rectangle(0, 0, 120, 30), new Padding(2), metrics, ContentAlignment.MiddleRight, false);
        var preferred = BootstrapCheckableRenderLogic.GetPreferredSize(new Size(60, 14), new Padding(2), metrics);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tiny.IndicatorBounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(tiny.TextBounds.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(normal.IndicatorBounds.Right, Is.LessThanOrEqualTo(118));
            Assert.That(normal.TextBounds.Right, Is.LessThanOrEqualTo(118));
            Assert.That(preferred, Is.EqualTo(new Size(88, 22)));
        }));
    }

    [TestCase(Appearance.Button, false, false, -1, null, true)]
    [TestCase(Appearance.Normal, true, false, -1, null, true)]
    [TestCase(Appearance.Normal, false, true, 0, null, true)]
    [TestCase(Appearance.Normal, false, true, -1, "icon", true)]
    [TestCase(Appearance.Normal, false, false, -1, null, false)]
    public void NativeFallbackRecognizesButtonAndEffectiveImagePresentation(Appearance appearance, bool hasImage, bool hasImageList, int imageIndex, string? imageKey, bool expected)
    {
        Assert.That(BootstrapCheckableRenderLogic.ShouldUseNativeFallback(appearance, hasImage, hasImageList, imageIndex, imageKey), Is.EqualTo(expected));
    }

    [Test]
    public void InvalidInputsAreRejectedBeforeReturningUsableState()
    {
        var colors = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Colors;
        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, null!, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapCheckableRenderLogic.GetMetrics((BootstrapCheckableKind)99, BootstrapThemeMetrics.Default, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, BootstrapThemeMetrics.Default, 0)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapCheckableRenderLogic.ResolvePalette(colors, (BootstrapVariant)99, BootstrapValidationState.None, CheckState.Checked, true)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapCheckableRenderLogic.ResolvePalette(colors, BootstrapVariant.Primary, (BootstrapValidationState)99, CheckState.Checked, true)));
    }
}
