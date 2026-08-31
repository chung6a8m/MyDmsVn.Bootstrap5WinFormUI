using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class BootstrapGeneralInputFormatterTests
{
    [TestCase("1234567890123456", "1234 5678 9012 3456")]
    [TestCase("123456789012345678", "1234 5678 9012 3456")]
    public void FourDigitBlocksFormatAndTruncate(string raw, string expected)
    {
        AssertRoundTrip(new BootstrapGeneralFormatOptions { Blocks = new[] { 4, 4, 4, 4 } }, raw, expected, expected.Replace(" ", string.Empty));
    }

    [Test]
    public void SupportsUnevenBlocksAndPerBoundaryDelimiters()
    {
        AssertRoundTrip(
            new BootstrapGeneralFormatOptions { Blocks = new[] { 4, 3, 3, 4 }, Delimiter = "-" },
            "12345678901234", "1234-567-890-1234", "12345678901234");
        AssertRoundTrip(
            new BootstrapGeneralFormatOptions { Blocks = new[] { 3, 3, 3, 2 }, Delimiters = new[] { ".", ".", "-" } },
            "12345678901", "123.456.789-01", "12345678901");
    }

    [Test]
    public void FilteringCaseAndPrefixProduceCanonicalRawText()
    {
        var numeric = new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions { NumericOnly = true });
        var upper = new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions { Delimiter = "-", Uppercase = true });
        var prefixed = new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions { Prefix = "VN", Blocks = new[] { 4, 4 } });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(numeric.Unformat("AB12-C3"), Is.EqualTo("123"));
            Assert.That(upper.Format("ab-cd"), Is.EqualTo("ABCD"));
            Assert.That(prefixed.Format("12345678"), Is.EqualTo("VN1234 5678"));
            Assert.That(prefixed.Unformat("VN1234 5678"), Is.EqualTo("12345678"));
            Assert.That(prefixed.Format(string.Empty), Is.Empty);
        }));
    }

    [Test]
    public void EagerDelimiterAppearsAtNonFinalExactBoundaryOnly()
    {
        var eager = new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions { Blocks = new[] { 4, 4 }, Delimiter = "-" });
        var lazy = new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions { Blocks = new[] { 4, 4 }, Delimiter = "-", DelimiterLazyShow = true });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(eager.Format("1234"), Is.EqualTo("1234-"));
            Assert.That(lazy.Format("1234"), Is.EqualTo("1234"));
            Assert.That(eager.Format("12345678"), Is.EqualTo("1234-5678"));
        }));
    }

    private static void AssertRoundTrip(BootstrapGeneralFormatOptions options, string raw, string display, string canonical)
    {
        var formatter = new BootstrapGeneralInputFormatter(options);
        Assert.That(formatter.Format(raw), Is.EqualTo(display));
        Assert.That(formatter.Unformat(display), Is.EqualTo(canonical));
        Assert.That(formatter.Format(formatter.Unformat(display)), Is.EqualTo(display));
    }
}
