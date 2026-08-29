using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDatePickerCultureRegressionTests
{
    [Test]
    public void NativeMinimumDateTracksCurrentCultureCalendar()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("ja-JP").Clone();
            culture.DateTimeFormat.Calendar = new JapaneseCalendar();
            CultureInfo.CurrentCulture = culture;

            using var native = new DateTimePicker();

            Assert.That(native.MinDate, Is.EqualTo(DateTimePicker.MinimumDateTime));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
