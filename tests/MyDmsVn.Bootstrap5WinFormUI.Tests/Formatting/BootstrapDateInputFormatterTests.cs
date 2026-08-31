using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class BootstrapDateInputFormatterTests
{
    [TestCase("31082026", "31/08/2026")]
    [TestCase("3", "3")]
    [TestCase("310", "31/0")]
    public void DefaultPatternSupportsCompleteAndPartialInput(string raw, string expected)
    {
        var formatter = new BootstrapDateInputFormatter(new BootstrapDateFormatOptions());
        Assert.That(formatter.Format(raw), Is.EqualTo(expected));
    }

    [Test]
    public void AlternatePatternsAndSanitizationRoundTrip()
    {
        var ymd = new BootstrapDateInputFormatter(new BootstrapDateFormatOptions { Pattern = "Ymd", Delimiter = "-" });
        var my = new BootstrapDateInputFormatter(new BootstrapDateFormatOptions { Pattern = "my" });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ymd.Format("20260831"), Is.EqualTo("2026-08-31"));
            Assert.That(my.Format("0826"), Is.EqualTo("08/26"));
            Assert.That(ymd.Unformat("2026a-08b-31"), Is.EqualTo("20260831"));
        }));
    }

    [Test]
    public void DelimiterEagernessAndComponentShapingAreStructuralOnly()
    {
        var eager = new BootstrapDateInputFormatter(new BootstrapDateFormatOptions());
        var lazy = new BootstrapDateInputFormatter(new BootstrapDateFormatOptions { DelimiterLazyShow = true });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(eager.Format("31"), Is.EqualTo("31/"));
            Assert.That(lazy.Format("31"), Is.EqualTo("31"));
            Assert.That(eager.Format("99002026"), Is.EqualTo("31/01/2026"));
            Assert.That(eager.Format("31022026"), Is.EqualTo("31/02/2026"), "Cross-component calendar validation is outside formatting.");
        }));
    }

    [TestCase("３１", "")]
    [TestCase("٣1", "1")]
    public void UnicodeDecimalDigitsAreIgnoredWithoutThrowing(string candidate, string expected)
    {
        var formatter = new BootstrapDateInputFormatter(new BootstrapDateFormatOptions());

        Assert.That(formatter.Format(candidate), Is.EqualTo(expected));
        Assert.That(formatter.Unformat(candidate), Is.EqualTo(expected));
    }
}
