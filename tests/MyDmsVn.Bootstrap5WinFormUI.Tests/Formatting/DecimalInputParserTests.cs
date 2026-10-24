using System;
using System.Globalization;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class DecimalInputParserTests
{
    private static DecimalInputProfile Quantity(CultureInfo culture) =>
        new DecimalInputProfile(culture, false, 4, 0.0001m, 99999999999999.9999m);
    private static DecimalInputProfile Cost(CultureInfo culture) =>
        new DecimalInputProfile(culture, false, 4, 0m, 999999999999999.9999m);

    [TestCase("en-US", ".5", 0.5)]
    [TestCase("en-US", "1.", 1.0)]
    [TestCase("en-US", "1.23000", 1.23)]
    [TestCase("vi-VN", ",5", 0.5)]
    [TestCase("vi-VN", "1,", 1.0)]
    [TestCase("vi-VN", "1,23000", 1.23)]
    public void Leading_trailing_and_zero_padded_forms_are_exact(string cultureName, string raw, double expected)
    {
        var result = DecimalInputParser.TryParsePending(raw, Quantity(CultureInfo.GetCultureInfo(cultureName)));
        Assert.That(result.State, Is.EqualTo(InputParseState.Valid));
        Assert.That(result.Value, Is.EqualTo((decimal)expected));
        Assert.That(result.Failure, Is.Null);
    }

    [Test]
    public void Outer_trim_and_long_zero_padding_do_not_change_the_candidate()
    {
        var profile = Quantity(CultureInfo.GetCultureInfo("en-US"));
        var raw = "\u00a0\t00000000000000000000000000000000001.2345" + new string('0', 80) + "\r\n";
        var result = DecimalInputParser.TryParsePending(raw, profile);
        Assert.That(result.State, Is.EqualTo(InputParseState.Valid));
        Assert.That(result.Value, Is.EqualTo(1.2345m));
        Assert.That(raw.StartsWith("\u00a0\t"), Is.True);
        Assert.That(raw.EndsWith("\r\n"), Is.True);
    }

    [Test]
    public void Meaningful_far_tail_digit_rejects_before_decimal_can_round_it_away()
    {
        var profile = Quantity(CultureInfo.GetCultureInfo("en-US"));
        var raw = "1.2300" + new string('0', 80) + "1";
        var result = DecimalInputParser.TryParsePending(raw, profile);
        Assert.That(result.State, Is.EqualTo(InputParseState.Invalid));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Failure?.Code, Is.EqualTo(InputParseFailureCode.Precision));
    }

    [Test]
    public void Bounds_sign_and_decimal_representability_are_distinct_failures()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        AssertValid("99999999999999.9999", Quantity(culture), 99999999999999.9999m);
        AssertInvalid("100000000000000", Quantity(culture), InputParseFailureCode.OutOfRange);
        AssertInvalid("0", Quantity(culture), InputParseFailureCode.OutOfRange);
        AssertValid("-0", Cost(culture), 0m);
        AssertInvalid("-1", Cost(culture), InputParseFailureCode.OutOfRange);
        AssertValid("999999999999999.9999", Cost(culture), 999999999999999.9999m);
        AssertInvalid("9999999999999999999999999999999999999999", Cost(culture), InputParseFailureCode.Unrepresentable);
        AssertInvalid("0.00000000000000000000000000001",
            new DecimalInputProfile(culture, false, 28, 0m, decimal.MaxValue), InputParseFailureCode.Precision);
        AssertValid("0.0000000000000000000000000001",
            new DecimalInputProfile(culture, false, 28, 0m, decimal.MaxValue), 0.0000000000000000000000000001m);
    }

    [Test]
    public void External_grouping_exponent_currency_and_unicode_digits_are_not_accepted()
    {
        foreach (var cultureName in new[] { "en-US", "vi-VN" })
        {
            var profile = Quantity(CultureInfo.GetCultureInfo(cultureName));
            foreach (var raw in new[] { "1,234.5", "1.234,5", "1 234", "1\u00a0234", "1e2", "$1", "1đ", "١٢", "１２", "1\0", "1-2" })
                AssertInvalid(raw, profile, InputParseFailureCode.Syntax);
        }
    }

    [Test]
    public void Literal_culture_tokens_are_cloned_and_not_reduced_to_single_characters()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.PositiveSign = "plus";
        culture.NumberFormat.NegativeSign = "minus";
        culture.NumberFormat.NumberDecimalSeparator = "::";
        var profile = Cost(culture);
        culture.NumberFormat.NumberDecimalSeparator = ".";
        AssertValid("plus1::23000", profile, 1.23m);
        AssertValid("minus0::0", profile, 0m);
        AssertInvalid("+1::23", profile, InputParseFailureCode.Syntax);
        AssertInvalid("1:23", profile, InputParseFailureCode.Syntax);
        Assert.That(profile.Culture.NumberFormat.NumberDecimalSeparator, Is.EqualTo("::"));
        Assert.Throws<InvalidOperationException>((Action)(() => profile.Culture.NumberFormat.NumberDecimalSeparator = "."));
    }

    [Test]
    public void Blank_and_prefixes_have_explicit_noncommittable_states()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var required = DecimalInputParser.TryParsePending(" \t ", Quantity(culture));
        Assert.That(required.State, Is.EqualTo(InputParseState.Empty));
        Assert.That(required.Failure?.Code, Is.EqualTo(InputParseFailureCode.Required));
        var optional = DecimalInputParser.TryParsePending("\u00a0", new DecimalInputProfile(culture, true, 4, 0m, 100m));
        Assert.That(optional.State, Is.EqualTo(InputParseState.Valid));
        Assert.That(optional.Value, Is.Null);
        Assert.That(optional.Failure, Is.Null);
        foreach (var raw in new[] { "+", "-", ".", "+." })
        {
            var result = DecimalInputParser.TryParsePending(raw, Cost(culture));
            Assert.That(result.State, Is.EqualTo(InputParseState.Incomplete), raw);
            Assert.That(result.Failure?.Code, Is.EqualTo(InputParseFailureCode.Incomplete), raw);
        }
    }

    [Test]
    public void Invalid_profile_arguments_are_rejected()
    {
        var culture = CultureInfo.InvariantCulture;
        Assert.Throws<ArgumentNullException>((Action)(() => new DecimalInputProfile(null!, false, 4, 0m, 1m)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new DecimalInputProfile(culture, false, -1, 0m, 1m)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new DecimalInputProfile(culture, false, 29, 0m, 1m)));
        Assert.Throws<ArgumentException>((Action)(() => new DecimalInputProfile(culture, false, 4, 2m, 1m)));
        Assert.Throws<ArgumentNullException>((Action)(() => DecimalInputParser.TryParsePending(null!, Cost(culture))));
        Assert.Throws<ArgumentNullException>((Action)(() => DecimalInputParser.TryParsePending("1", null!)));
    }

    private static void AssertValid(string raw, DecimalInputProfile profile, decimal expected)
    {
        var result = DecimalInputParser.TryParsePending(raw, profile);
        Assert.That(result.State, Is.EqualTo(InputParseState.Valid), raw);
        Assert.That(result.Value, Is.EqualTo(expected), raw);
        Assert.That(result.Failure, Is.Null, raw);
    }

    private static void AssertInvalid(string raw, DecimalInputProfile profile, InputParseFailureCode code)
    {
        var result = DecimalInputParser.TryParsePending(raw, profile);
        Assert.That(result.State, Is.EqualTo(InputParseState.Invalid), raw);
        Assert.That(result.Value, Is.Null, raw);
        Assert.That(result.Failure?.Code, Is.EqualTo(code), raw);
    }
}
