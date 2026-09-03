using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapCheckableMultilineTests
{
    [Test]
    public void TextFlagsPreserveNativeMultilineWrappingContract()
    {
        var flags = BootstrapCheckableRenderLogic.GetTextFormatFlags(
            ContentAlignment.MiddleLeft,
            useMnemonic: true,
            showKeyboardCues: true,
            autoEllipsis: false,
            rightToLeft: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(flags.HasFlag(TextFormatFlags.WordBreak), Is.True);
            Assert.That(flags.HasFlag(TextFormatFlags.TextBoxControl), Is.True);
            Assert.That(flags.HasFlag(TextFormatFlags.SingleLine), Is.False);
        }));
    }

    [TestCase(typeof(BootstrapCheckBox))]
    [TestCase(typeof(BootstrapRadioButton))]
    [TestCase(typeof(BootstrapSwitch))]
    public void ExplicitNewlineIncreasesPreferredHeight(Type controlType)
    {
        using var multiline = (ButtonBase)Activator.CreateInstance(controlType)!;
        using var singleLine = (ButtonBase)Activator.CreateInstance(controlType)!;
        multiline.Text = "First line\nSecond line";
        singleLine.Text = "First line Second line";

        Assert.That(
            multiline.GetPreferredSize(Size.Empty).Height,
            Is.GreaterThan(singleLine.GetPreferredSize(Size.Empty).Height),
            $"{controlType.Name} must preserve explicit line breaks in preferred-size measurement.");
    }

    [TestCase(typeof(BootstrapCheckBox))]
    [TestCase(typeof(BootstrapRadioButton))]
    [TestCase(typeof(BootstrapSwitch))]
    public void AutoSizeFalseNarrowBoundsKeepNativeWordWrapping(Type controlType)
    {
        using var control = (ButtonBase)Activator.CreateInstance(controlType)!;
        control.AutoSize = false;
        control.Size = new Size(120, 64);
        control.Padding = Padding.Empty;
        control.Text = "This label should wrap inside a narrow host-assigned width";
        control.TextAlign = ContentAlignment.TopLeft;

        BootstrapCheckableKind kind;
        ContentAlignment checkAlign;
        if (control is BootstrapRadioButton radio)
        {
            kind = BootstrapCheckableKind.RadioButton;
            radio.CheckAlign = ContentAlignment.MiddleLeft;
            checkAlign = radio.CheckAlign;
        }
        else
        {
            var checkBox = (CheckBox)control;
            kind = control is BootstrapSwitch ? BootstrapCheckableKind.Switch : BootstrapCheckableKind.CheckBox;
            checkBox.CheckAlign = ContentAlignment.MiddleLeft;
            checkAlign = checkBox.CheckAlign;
        }

        var dpi = control.DeviceDpi > 0 ? control.DeviceDpi : 96;
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(kind, BootstrapThemeManager.CurrentTheme.Metrics, dpi);
        var layout = BootstrapCheckableRenderLogic.GetLayout(control.ClientRectangle, control.Padding, metrics, checkAlign, rightToLeft: false);
        var flags = BootstrapCheckableRenderLogic.GetTextFormatFlags(control.TextAlign, control.UseMnemonic, showKeyboardCues: true, control.AutoEllipsis, rightToLeft: false);
        var wrapped = TextRenderer.MeasureText(control.Text, control.Font, layout.TextBounds.Size, flags);
        var oneLineHeight = TextRenderer.MeasureText(control.Text, control.Font, Size.Empty, flags & ~TextFormatFlags.WordBreak).Height;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(control.AutoSize, Is.False);
            Assert.That(control.Size, Is.EqualTo(new Size(120, 64)));
            Assert.That(layout.TextBounds.Width, Is.GreaterThan(0));
            Assert.That(wrapped.Height, Is.GreaterThan(oneLineHeight), $"{controlType.Name} should wrap text inside a fixed narrow text field.");
        }));
    }
}
