using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class BootstrapTimeInputFormatterTests
{
    [TestCase("1230", "12:30")]
    [TestCase("12", "12:")]
    public void DefaultPatternFormatsCompleteAndPartialInput(string raw, string expected)
    {
        var formatter = new BootstrapTimeInputFormatter(new BootstrapTimeFormatOptions());
        Assert.That(formatter.Format(raw), Is.EqualTo(expected));
    }

    [Test]
    public void SecondsAndLazyDelimiterAreSupported()
    {
        var seconds = new BootstrapTimeInputFormatter(new BootstrapTimeFormatOptions { Pattern = "hms" });
        var lazy = new BootstrapTimeInputFormatter(new BootstrapTimeFormatOptions { DelimiterLazyShow = true });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(seconds.Format("123045"), Is.EqualTo("12:30:45"));
            Assert.That(lazy.Format("12"), Is.EqualTo("12"));
        }));
    }

    [Test]
    public void CompleteComponentsAreConstrainedByClockMode()
    {
        var twentyFour = new BootstrapTimeInputFormatter(new BootstrapTimeFormatOptions { Pattern = "hms" });
        var twelve = new BootstrapTimeInputFormatter(new BootstrapTimeFormatOptions { Pattern = "hms", TimeFormat = BootstrapTimeFormat.TwelveHour });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(twentyFour.Format("996099"), Is.EqualTo("23:59:59"));
            Assert.That(twelve.Format("006099"), Is.EqualTo("01:59:59"));
            Assert.That(twentyFour.Unformat("23:59:59"), Is.EqualTo("235959"));
        }));
    }

    [TestCase("３１", "")]
    [TestCase("٣1", "1")]
    public void UnicodeDecimalDigitsAreIgnoredWithoutThrowing(string candidate, string expected)
    {
        var formatter = new BootstrapTimeInputFormatter(new BootstrapTimeFormatOptions());

        Assert.That(formatter.Format(candidate), Is.EqualTo(expected));
        Assert.That(formatter.Unformat(candidate), Is.EqualTo(expected));
    }
}
