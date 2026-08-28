using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDatePickerTests
{
    [Test]
    public void NativeDefaultsAreCharacterizedForStage9()
    {
        var before = DateTime.Now;
        using var native = new DateTimePicker();
        var after = DateTime.Now;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Value, Is.InRange(before, after));
            Assert.That(native.MinDate, Is.EqualTo(new DateTime(1753, 1, 1)));
            Assert.That(native.Format, Is.EqualTo(DateTimePickerFormat.Long));
            Assert.That(native.CustomFormat, Is.Null);
            Assert.That(native.ShowCheckBox, Is.False);
            Assert.That(native.Checked, Is.True);
            Assert.That(native.ShowUpDown, Is.False);
        }));
    }

    [Test]
    public void NativeRangeFormatAndCheckboxSemanticsAreCharacterizedForStage9()
    {
        var minimum = new DateTime(2020, 1, 1);
        var maximum = new DateTime(2030, 12, 31);
        var sample = new DateTime(2026, 8, 28, 10, 30, 0);
        using var native = new DateTimePicker
        {
            MinDate = minimum,
            MaxDate = maximum,
            Value = sample,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            ShowCheckBox = true,
            Checked = false
        };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.MinDate, Is.EqualTo(minimum));
            Assert.That(native.MaxDate, Is.EqualTo(maximum));
            Assert.That(native.Value, Is.EqualTo(sample));
            Assert.That(native.Format, Is.EqualTo(DateTimePickerFormat.Custom));
            Assert.That(native.CustomFormat, Is.EqualTo("yyyy-MM-dd HH:mm"));
            Assert.That(native.ShowCheckBox, Is.True);
            Assert.That(native.Checked, Is.False);
        }));

        native.Checked = true;
        Assert.That(native.Checked, Is.True);
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => native.Value = maximum.AddDays(1)));
        Assert.Throws<InvalidEnumArgumentException>((Action)(() => native.Format = (DateTimePickerFormat)999));
    }
}
