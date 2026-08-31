using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class BootstrapCreditCardInputFormatterTests
{
    [TestCase("123456", BootstrapCreditCardType.Uatp)]
    [TestCase("34", BootstrapCreditCardType.AmericanExpress)]
    [TestCase("37", BootstrapCreditCardType.AmericanExpress)]
    [TestCase("300", BootstrapCreditCardType.Diners)]
    [TestCase("305", BootstrapCreditCardType.Diners)]
    [TestCase("309", BootstrapCreditCardType.Diners)]
    [TestCase("36", BootstrapCreditCardType.Diners)]
    [TestCase("38", BootstrapCreditCardType.Diners)]
    [TestCase("39", BootstrapCreditCardType.Diners)]
    [TestCase("6011", BootstrapCreditCardType.Discover)]
    [TestCase("65", BootstrapCreditCardType.Discover)]
    [TestCase("644", BootstrapCreditCardType.Discover)]
    [TestCase("649", BootstrapCreditCardType.Discover)]
    [TestCase("51", BootstrapCreditCardType.Mastercard)]
    [TestCase("55", BootstrapCreditCardType.Mastercard)]
    [TestCase("2221", BootstrapCreditCardType.Mastercard)]
    [TestCase("2720", BootstrapCreditCardType.Mastercard)]
    [TestCase("5019", BootstrapCreditCardType.Dankort)]
    [TestCase("4175", BootstrapCreditCardType.Dankort)]
    [TestCase("4571", BootstrapCreditCardType.Dankort)]
    [TestCase("637", BootstrapCreditCardType.Instapayment)]
    [TestCase("639", BootstrapCreditCardType.Instapayment)]
    [TestCase("2131", BootstrapCreditCardType.Jcb15)]
    [TestCase("1800", BootstrapCreditCardType.Jcb15)]
    [TestCase("35", BootstrapCreditCardType.Jcb)]
    [TestCase("50", BootstrapCreditCardType.Maestro)]
    [TestCase("56", BootstrapCreditCardType.Maestro)]
    [TestCase("58", BootstrapCreditCardType.Maestro)]
    [TestCase("6304", BootstrapCreditCardType.Maestro)]
    [TestCase("67", BootstrapCreditCardType.Maestro)]
    [TestCase("4", BootstrapCreditCardType.Visa)]
    [TestCase("2200", BootstrapCreditCardType.Mir)]
    [TestCase("2204", BootstrapCreditCardType.Mir)]
    [TestCase("62", BootstrapCreditCardType.UnionPay)]
    [TestCase("81", BootstrapCreditCardType.UnionPay)]
    [TestCase("9", BootstrapCreditCardType.General)]
    public void DetectsFrozenIinFamilies(string prefix, BootstrapCreditCardType expected)
    {
        var formatter = new BootstrapCreditCardInputFormatter(new BootstrapCreditCardFormatOptions());
        Assert.That(formatter.GetCardType(prefix), Is.EqualTo(expected));
    }

    [TestCase("2220", BootstrapCreditCardType.General)]
    [TestCase("2221", BootstrapCreditCardType.Mastercard)]
    [TestCase("2720", BootstrapCreditCardType.Mastercard)]
    [TestCase("2721", BootstrapCreditCardType.General)]
    public void MastercardTwoSeriesUsesExactNumericBoundaries(string prefix, BootstrapCreditCardType expected)
    {
        var formatter = new BootstrapCreditCardInputFormatter(new BootstrapCreditCardFormatOptions());
        Assert.That(formatter.GetCardType(prefix), Is.EqualTo(expected));
    }

    [TestCase("4111111111111111", "4111 1111 1111 1111")]
    [TestCase("378282246310005", "3782 822463 10005")]
    [TestCase("30569309025904", "3056 930902 5904")]
    public void FormatsTypeSpecificBlocks(string raw, string expected)
    {
        var formatter = new BootstrapCreditCardInputFormatter(new BootstrapCreditCardFormatOptions());
        Assert.That(formatter.Format(raw), Is.EqualTo(expected));
        Assert.That(formatter.Unformat(expected), Is.EqualTo(raw));
    }

    [Test]
    public void StrictModeExtendsDetectedLayoutToNineteenDigits()
    {
        var formatter = new BootstrapCreditCardInputFormatter(new BootstrapCreditCardFormatOptions { StrictMode = true, Delimiter = "-" });
        const string raw = "4111111111111111111";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(formatter.Format(raw), Is.EqualTo("4111-1111-1111-1111-111"));
            Assert.That(formatter.Unformat("4111-ab11-1111"), Is.EqualTo("4111111111"));
        }));
    }
}
