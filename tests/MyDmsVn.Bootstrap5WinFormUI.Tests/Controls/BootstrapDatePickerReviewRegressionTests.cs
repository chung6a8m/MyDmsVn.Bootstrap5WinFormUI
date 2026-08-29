using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDatePickerReviewRegressionTests
{
    [Test]
    public void DesignerSerializationDelegatesDynamicNativeDefaultsAndResetSemantics()
    {
        using var input = new BootstrapDatePicker();
        var properties = TypeDescriptor.GetProperties(input);
        var valueProperty = properties[nameof(BootstrapDatePicker.Value)]!;
        var minDateProperty = properties[nameof(BootstrapDatePicker.MinDate)]!;
        var maxDateProperty = properties[nameof(BootstrapDatePicker.MaxDate)]!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(valueProperty.ShouldSerializeValue(input), Is.False);
            Assert.That(minDateProperty.ShouldSerializeValue(input), Is.False);
            Assert.That(maxDateProperty.ShouldSerializeValue(input), Is.False);
            Assert.That(valueProperty.CanResetValue(input), Is.False);
            Assert.That(minDateProperty.CanResetValue(input), Is.False);
            Assert.That(maxDateProperty.CanResetValue(input), Is.False);
        }));

        input.MinDate = new DateTime(2020, 1, 1);
        input.MaxDate = new DateTime(2030, 12, 31);
        input.Value = new DateTime(2026, 8, 29, 9, 30, 0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(valueProperty.ShouldSerializeValue(input), Is.True);
            Assert.That(minDateProperty.ShouldSerializeValue(input), Is.True);
            Assert.That(maxDateProperty.ShouldSerializeValue(input), Is.True);
            Assert.That(valueProperty.CanResetValue(input), Is.True);
            Assert.That(minDateProperty.CanResetValue(input), Is.True);
            Assert.That(maxDateProperty.CanResetValue(input), Is.True);
        }));

        valueProperty.ResetValue(input);
        minDateProperty.ResetValue(input);
        maxDateProperty.ResetValue(input);

        using var nativePeer = new DateTimePicker();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(valueProperty.ShouldSerializeValue(input), Is.False);
            Assert.That(minDateProperty.ShouldSerializeValue(input), Is.False);
            Assert.That(maxDateProperty.ShouldSerializeValue(input), Is.False);
            Assert.That(input.MinDate, Is.EqualTo(nativePeer.MinDate));
            Assert.That(input.MaxDate, Is.EqualTo(nativePeer.MaxDate));
            Assert.That(input.Value, Is.EqualTo(nativePeer.Value).Within(TimeSpan.FromSeconds(2)));
        }));
    }

    [Test]
    public void LayoutExpandsWrapperBeforeLayingOutFixedHeightNativePicker()
    {
        using var largeFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 24f);
        using var input = new BootstrapDatePicker
        {
            Font = largeFont,
            Size = new Size(280, 1)
        };
        var native = input.Controls.OfType<DateTimePicker>().Single();

        input.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.ClientRectangle.Contains(native.Bounds), Is.True);
            Assert.That(input.ClientSize.Height, Is.GreaterThanOrEqualTo(native.PreferredHeight));
            Assert.That(native.Height, Is.EqualTo(native.PreferredHeight));
        }));
    }
}
