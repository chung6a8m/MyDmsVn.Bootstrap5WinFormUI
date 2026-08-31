using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapFormattedTextBoxTests
{
    [Test]
    public void DefaultsMatchFormattedInputContract()
    {
        using var input = new BootstrapFormattedTextBox();
        var editor = input.Controls.OfType<TextBox>().Single();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.FormatMode, Is.EqualTo(BootstrapInputFormatMode.None));
            Assert.That(input.Text, Is.Empty);
            Assert.That(input.RawValue, Is.Empty);
            Assert.That(input.CreditCardType, Is.EqualTo(BootstrapCreditCardType.General));
            Assert.That(input.TabStop, Is.True);
            Assert.That(editor.TabStop, Is.False);
        }));
    }

    [TestCase(BootstrapInputFormatMode.General, "12345678", "1234 5678")]
    [TestCase(BootstrapInputFormatMode.Numeral, "1234567.89", "1,234,567.89")]
    [TestCase(BootstrapInputFormatMode.Date, "31082026", "31/08/2026")]
    [TestCase(BootstrapInputFormatMode.Time, "1230", "12:30")]
    [TestCase(BootstrapInputFormatMode.CreditCard, "4111111111111111", "4111 1111 1111 1111")]
    public void BuiltInModesCanonicalizeRawAndDisplay(BootstrapInputFormatMode mode, string raw, string display)
    {
        using var input = new BootstrapFormattedTextBox();
        input.GeneralOptions.Blocks = new[] { 4, 4 };
        input.FormatMode = mode;
        input.RawValue = raw;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo(raw));
            Assert.That(input.Text, Is.EqualTo(display));
        }));
    }

    [Test]
    public void TextAssignmentCanonicalizesCandidateAndRaisesStableEventsInOrder()
    {
        using var input = new BootstrapFormattedTextBox { FormatMode = BootstrapInputFormatMode.General };
        input.GeneralOptions.Blocks = new[] { 4, 4 };
        var events = new List<string>();
        input.TextChanged += (_, _) =>
        {
            events.Add("TextChanged");
            Assert.That(input.Text, Is.EqualTo("1234 5"));
            Assert.That(input.RawValue, Is.EqualTo("12345"));
        };
        input.RawValueChanged += (_, _) =>
        {
            events.Add("RawValueChanged");
            Assert.That(input.Text, Is.EqualTo("1234 5"));
            Assert.That(input.RawValue, Is.EqualTo("12345"));
        };

        input.Text = "12345";

        Assert.That(events, Is.EqualTo(new[] { "TextChanged", "RawValueChanged" }));
    }

    [Test]
    public void OptionChangesAndReformatUseCurrentCanonicalRawValue()
    {
        using var input = new BootstrapFormattedTextBox { FormatMode = BootstrapInputFormatMode.General };
        input.GeneralOptions.Blocks = new[] { 4, 4 };
        input.RawValue = "12345678";

        input.GeneralOptions.Delimiter = "-";
        Assert.That(input.Text, Is.EqualTo("1234-5678"));

        input.GeneralOptions.Prefix = "VN";
        input.Reformat();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("12345678"));
            Assert.That(input.Text, Is.EqualTo("VN1234-5678"));
        }));
    }

    [Test]
    public void CustomFormatterAndNullCustomFormatterFollowIdentityFallback()
    {
        using var input = new BootstrapFormattedTextBox
        {
            FormatMode = BootstrapInputFormatMode.Custom,
            Formatter = new UppercaseFormatter()
        };

        input.Text = "ab-cd";
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("abcd"));
            Assert.That(input.Text, Is.EqualTo("ABCD"));
        }));

        input.Formatter = null;
        input.Text = "mixed Value";
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("mixed Value"));
            Assert.That(input.Text, Is.EqualTo("mixed Value"));
        }));
    }

    [Test]
    public void CreditCardTypeTracksOnlyCreditCardMode()
    {
        using var input = new BootstrapFormattedTextBox { FormatMode = BootstrapInputFormatMode.CreditCard };
        var changes = 0;
        input.CreditCardTypeChanged += (_, _) => changes++;
        input.RawValue = "378282246310005";
        Assert.That(input.CreditCardType, Is.EqualTo(BootstrapCreditCardType.AmericanExpress));

        input.FormatMode = BootstrapInputFormatMode.None;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.CreditCardType, Is.EqualTo(BootstrapCreditCardType.General));
            Assert.That(changes, Is.EqualTo(2));
        }));
    }

    [Test]
    public void RejectedRawDigitSeparatorsDoNotReformatStableValues()
    {
        AssertRejectedSeparatorPreservesValue(
            BootstrapInputFormatMode.Date,
            "31082026",
            input => input.DateOptions.Delimiter = "1");
        AssertRejectedSeparatorPreservesValue(
            BootstrapInputFormatMode.Time,
            "1230",
            input => input.TimeOptions.Delimiter = "2");
        AssertRejectedSeparatorPreservesValue(
            BootstrapInputFormatMode.CreditCard,
            "4111111111111111",
            input => input.CreditCardOptions.Delimiter = "3");
        AssertRejectedSeparatorPreservesValue(
            BootstrapInputFormatMode.Numeral,
            "1234.56",
            input => input.NumeralOptions.Delimiter = "4");
        AssertRejectedSeparatorPreservesValue(
            BootstrapInputFormatMode.Numeral,
            "1234.56",
            input => input.NumeralOptions.DecimalMark = "5");
    }

    private static void AssertRejectedSeparatorPreservesValue(
        BootstrapInputFormatMode mode,
        string rawValue,
        Action<BootstrapFormattedTextBox> assignInvalidSeparator)
    {
        using var input = new BootstrapFormattedTextBox { FormatMode = mode };
        input.RawValue = rawValue;
        var originalRaw = input.RawValue;
        var originalText = input.Text;
        var textChanges = 0;
        var rawChanges = 0;
        input.TextChanged += (_, _) => textChanges++;
        input.RawValueChanged += (_, _) => rawChanges++;

        Assert.Throws<ArgumentException>((Action)(() => assignInvalidSeparator(input)), mode.ToString());
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo(originalRaw), mode.ToString());
            Assert.That(input.Text, Is.EqualTo(originalText), mode.ToString());
            Assert.That(textChanges, Is.Zero, mode.ToString());
            Assert.That(rawChanges, Is.Zero, mode.ToString());
        }));
    }

    private sealed class UppercaseFormatter : IInputFormatter
    {
        public string Format(string rawValue) => rawValue.ToUpperInvariant();

        public string Unformat(string formattedValue) => formattedValue.Replace("-", string.Empty).ToLowerInvariant();
    }
}
