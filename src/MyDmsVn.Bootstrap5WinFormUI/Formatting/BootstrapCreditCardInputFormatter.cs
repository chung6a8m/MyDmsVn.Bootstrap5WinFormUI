using System;
using System.Text;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Formats digit-only credit-card input and detects IIN formatting metadata.</summary>
public sealed class BootstrapCreditCardInputFormatter : IInputFormatter
{
    private readonly BootstrapCreditCardFormatOptions _options;

    /// <summary>Initializes a formatter backed by the supplied mutable options.</summary>
    /// <param name="options">The options to read for each operation.</param>
    public BootstrapCreditCardInputFormatter(BootstrapCreditCardFormatOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Detects the card family used for formatting without performing business validation.</summary>
    /// <param name="value">Raw or formatted card-number text.</param>
    /// <returns>The detected formatting type.</returns>
    public BootstrapCreditCardType GetCardType(string value)
    {
        var digits = DigitsOnly(value);
        if (digits.StartsWith("1", StringComparison.Ordinal) && !digits.StartsWith("1800", StringComparison.Ordinal)) return BootstrapCreditCardType.Uatp;
        if (StartsWithAny(digits, "34", "37")) return BootstrapCreditCardType.AmericanExpress;
        if (digits.StartsWith("6011", StringComparison.Ordinal) || digits.StartsWith("65", StringComparison.Ordinal) || InThreeDigitRange(digits, 644, 649)) return BootstrapCreditCardType.Discover;
        if (InThreeDigitRange(digits, 300, 305) || digits.StartsWith("309", StringComparison.Ordinal) || StartsWithAny(digits, "36", "38", "39")) return BootstrapCreditCardType.Diners;
        if (InTwoDigitRange(digits, 51, 55) || IsMastercardTwoSeries(digits)) return BootstrapCreditCardType.Mastercard;
        if (StartsWithAny(digits, "5019", "4175", "4571")) return BootstrapCreditCardType.Dankort;
        if (InThreeDigitRange(digits, 637, 639)) return BootstrapCreditCardType.Instapayment;
        if (StartsWithAny(digits, "2131", "1800")) return BootstrapCreditCardType.Jcb15;
        if (digits.StartsWith("35", StringComparison.Ordinal)) return BootstrapCreditCardType.Jcb;
        if (StartsWithAny(digits, "50", "56", "57", "58", "6304", "67")) return BootstrapCreditCardType.Maestro;
        if (InFourDigitRange(digits, 2200, 2204)) return BootstrapCreditCardType.Mir;
        if (digits.StartsWith("4", StringComparison.Ordinal)) return BootstrapCreditCardType.Visa;
        if (StartsWithAny(digits, "62", "81")) return BootstrapCreditCardType.UnionPay;
        return BootstrapCreditCardType.General;
    }

    /// <inheritdoc />
    public string Format(string rawValue)
    {
        var raw = Normalize(rawValue);
        return StructuredInputFormatterLogic.Format(raw, GetBlocks(GetCardType(raw)), _options.Delimiter, _options.DelimiterLazyShow);
    }

    /// <inheritdoc />
    public string Unformat(string formattedValue) => Normalize(formattedValue);

    private string Normalize(string? value)
    {
        var digits = DigitsOnly(value);
        var blocks = GetBlocks(GetCardType(digits));
        var capacity = 0;
        foreach (var block in blocks) capacity += block;
        return digits.Length > capacity ? digits.Substring(0, capacity) : digits;
    }

    private int[] GetBlocks(BootstrapCreditCardType type)
    {
        int[] blocks;
        switch (type)
        {
            case BootstrapCreditCardType.Uatp:
                blocks = new[] { 4, 5, 6 };
                break;
            case BootstrapCreditCardType.AmericanExpress:
            case BootstrapCreditCardType.Jcb15:
                blocks = new[] { 4, 6, 5 };
                break;
            case BootstrapCreditCardType.Diners:
                blocks = new[] { 4, 6, 4 };
                break;
            default:
                blocks = new[] { 4, 4, 4, 4 };
                break;
        }

        if (!_options.StrictMode) return blocks;
        var total = 0;
        foreach (var block in blocks) total += block;
        var strict = new int[blocks.Length + 1];
        Array.Copy(blocks, strict, blocks.Length);
        strict[strict.Length - 1] = 19 - total;
        return strict;
    }

    private static string DigitsOnly(string? value)
    {
        var candidate = value ?? string.Empty;
        var digits = new StringBuilder(candidate.Length);
        foreach (var character in candidate)
        {
            if (char.IsDigit(character)) digits.Append(character);
        }

        return digits.ToString();
    }

    private static bool StartsWithAny(string value, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static bool InTwoDigitRange(string value, int minimum, int maximum) =>
        value.Length >= 2 && int.TryParse(value.Substring(0, 2), out var prefix) && prefix >= minimum && prefix <= maximum;

    private static bool InThreeDigitRange(string value, int minimum, int maximum) =>
        value.Length >= 3 && int.TryParse(value.Substring(0, 3), out var prefix) && prefix >= minimum && prefix <= maximum;

    private static bool InFourDigitRange(string value, int minimum, int maximum) =>
        value.Length >= 4 && int.TryParse(value.Substring(0, 4), out var prefix) && prefix >= minimum && prefix <= maximum;

    private static bool IsMastercardTwoSeries(string value)
    {
        if (value.Length >= 4) return InFourDigitRange(value, 2221, 2720);
        if (value.Length >= 3 && value.StartsWith("22", StringComparison.Ordinal))
        {
            return value[2] >= '2' && value[2] <= '9';
        }

        if (value.Length >= 2) return value[0] == '2' && value[1] >= '3' && value[1] <= '7';
        return false;
    }
}
