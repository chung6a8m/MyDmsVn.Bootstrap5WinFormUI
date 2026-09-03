using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
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

    [Test]
    public void NarrowFixedWidthTextMeasurementWrapsInsteadOfForcingSingleLine()
    {
        using var font = new Font("Segoe UI", 9f);
        const string text = "This label should wrap inside a narrow host-assigned width";
        var flags = BootstrapCheckableRenderLogic.GetTextFormatFlags(
            ContentAlignment.TopLeft,
            useMnemonic: true,
            showKeyboardCues: true,
            autoEllipsis: false,
            rightToLeft: false);
        var wrapped = TextRenderer.MeasureText(text, font, new Size(72, 200), flags);
        var oneLineHeight = TextRenderer.MeasureText("Single line", font).Height;

        Assert.That(wrapped.Height, Is.GreaterThan(oneLineHeight));
    }
}
