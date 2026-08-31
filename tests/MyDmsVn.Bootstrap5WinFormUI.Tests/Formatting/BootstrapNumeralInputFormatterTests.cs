using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class BootstrapNumeralInputFormatterTests
{
    [TestCase("1234567.89", "1,234,567.89")]
    [TestCase("-1234.5", "-1,234.5")]
    [TestCase("000123.4", "123.4")]
    [TestCase("-", "-")]
    [TestCase("", "")]
    public void DefaultFormattingUsesInvariantCanonicalRaw(string raw, string expected)
    {
        var formatter = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions());
        Assert.That(formatter.Format(raw), Is.EqualTo(expected));
        Assert.That(formatter.Unformat(expected), Is.EqualTo(raw == "000123.4" ? "123.4" : raw));
    }

    [Test]
    public void AlternateDisplaySeparatorsRoundTripToInvariantRaw()
    {
        var options = new BootstrapNumeralFormatOptions { Delimiter = string.Empty, DecimalMark = "," };
        options.Delimiter = ".";
        var formatter = new BootstrapNumeralInputFormatter(options);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(formatter.Format("1234567.89"), Is.EqualTo("1.234.567,89"));
            Assert.That(formatter.Unformat("1.234.567,89"), Is.EqualTo("1234567.89"));
        }));
    }

    [Test]
    public void SignPrefixAndTailPrefixFollowConfiguredPlacement()
    {
        var leading = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions { Prefix = "$" });
        var signBefore = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions { Prefix = "$", SignBeforePrefix = true });
        var tail = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions { Prefix = " kg", TailPrefix = true });
        var positive = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions { PositiveOnly = true });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(leading.Format("1234"), Is.EqualTo("$1,234"));
            Assert.That(signBefore.Format("-1234"), Is.EqualTo("-$1,234"));
            Assert.That(tail.Format("-1234"), Is.EqualTo("-1,234 kg"));
            Assert.That(positive.Format("-1234"), Is.EqualTo("1,234"));
            Assert.That(tail.Format(string.Empty), Is.Empty);
        }));
    }

    [TestCase(BootstrapNumeralGroupStyle.Lakh, "1,23,45,678")]
    [TestCase(BootstrapNumeralGroupStyle.Wan, "1234,5678")]
    [TestCase(BootstrapNumeralGroupStyle.None, "12345678")]
    public void SupportsAllGroupingStyles(BootstrapNumeralGroupStyle style, string expected)
    {
        var formatter = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions { ThousandsGroupStyle = style });
        Assert.That(formatter.Format("12345678"), Is.EqualTo(expected));
    }

    [Test]
    public void ScalesTruncateStringsWithoutDecimalOverflow()
    {
        var formatter = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions
        {
            IntegerScale = 20,
            DecimalScale = 2,
            ThousandsGroupStyle = BootstrapNumeralGroupStyle.None
        });
        const string candidate = "1234567890123456789012345.3456";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(formatter.Unformat(candidate), Is.EqualTo("12345678901234567890.34"));
            Assert.That(formatter.Format(candidate), Is.EqualTo("12345678901234567890.34"));
        }));
    }

    [Test]
    public void CanonicalRawKeepsOnlyAsciiDigits()
    {
        var formatter = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions());

        Assert.Multiple((Action)(() =>
        {
            Assert.That(formatter.Format("１２3٤.٥6"), Is.EqualTo("3.6"));
            Assert.That(formatter.Unformat("１２3٤.٥6"), Is.EqualTo("3.6"));
        }));
    }
}
