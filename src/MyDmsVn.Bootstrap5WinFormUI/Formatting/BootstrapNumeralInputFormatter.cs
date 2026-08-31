using System;
using System.Collections.Generic;
using System.Text;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Formats numeral strings without converting them to a numeric runtime type.</summary>
public sealed class BootstrapNumeralInputFormatter : IInputFormatter
{
    private const char DecimalMarker = '\u001f';
    private readonly BootstrapNumeralFormatOptions _options;

    /// <summary>Initializes a formatter backed by the supplied mutable options.</summary>
    /// <param name="options">The options to read for each operation.</param>
    public BootstrapNumeralInputFormatter(BootstrapNumeralFormatOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Format(string rawValue)
    {
        var raw = NormalizeCanonical(rawValue);
        if (raw.Length == 0) return string.Empty;
        if (raw == "-") return _options.PositiveOnly ? string.Empty : "-";

        var negative = raw[0] == '-';
        var unsigned = negative ? raw.Substring(1) : raw;
        var decimalIndex = unsigned.IndexOf('.');
        var integer = decimalIndex >= 0 ? unsigned.Substring(0, decimalIndex) : unsigned;
        var fraction = decimalIndex >= 0 ? unsigned.Substring(decimalIndex + 1) : string.Empty;
        var grouped = Group(integer);
        var number = decimalIndex >= 0 && _options.DecimalScale > 0
            ? grouped + _options.DecimalMark + fraction
            : grouped;
        var sign = negative ? "-" : string.Empty;

        if (_options.TailPrefix) return sign + number + _options.Prefix;
        return negative && _options.SignBeforePrefix
            ? sign + _options.Prefix + number
            : _options.Prefix + sign + number;
    }

    /// <inheritdoc />
    public string Unformat(string formattedValue)
    {
        var candidate = formattedValue ?? string.Empty;
        var prefix = _options.Prefix;
        if (prefix.Length > 0)
        {
            if (_options.TailPrefix && candidate.EndsWith(prefix, StringComparison.Ordinal))
            {
                candidate = candidate.Substring(0, candidate.Length - prefix.Length);
            }
            else if (candidate.StartsWith(prefix, StringComparison.Ordinal))
            {
                candidate = candidate.Substring(prefix.Length);
            }
            else if (candidate.StartsWith("-" + prefix, StringComparison.Ordinal))
            {
                candidate = "-" + candidate.Substring(prefix.Length + 1);
            }
        }

        if (_options.DecimalMark.Length > 0)
        {
            candidate = candidate.Replace(_options.DecimalMark, DecimalMarker.ToString());
        }

        if (_options.Delimiter.Length > 0)
        {
            candidate = candidate.Replace(_options.Delimiter, string.Empty);
        }

        var negative = !_options.PositiveOnly && candidate.StartsWith("-", StringComparison.Ordinal);
        var raw = new StringBuilder(candidate.Length);
        var hasDecimal = false;
        foreach (var character in candidate)
        {
            if (char.IsDigit(character)) raw.Append(character);
            else if (character == DecimalMarker && !hasDecimal && _options.DecimalScale > 0)
            {
                raw.Append('.');
                hasDecimal = true;
            }
        }

        return NormalizeParts(raw.ToString(), negative);
    }

    private string NormalizeCanonical(string? value)
    {
        var candidate = value ?? string.Empty;
        var negative = !_options.PositiveOnly && candidate.StartsWith("-", StringComparison.Ordinal);
        var raw = new StringBuilder(candidate.Length);
        var hasDecimal = false;
        foreach (var character in candidate)
        {
            if (char.IsDigit(character)) raw.Append(character);
            else if (character == '.' && !hasDecimal && _options.DecimalScale > 0)
            {
                raw.Append('.');
                hasDecimal = true;
            }
        }

        return NormalizeParts(raw.ToString(), negative);
    }

    private string NormalizeParts(string unsigned, bool negative)
    {
        var decimalIndex = unsigned.IndexOf('.');
        var integer = decimalIndex >= 0 ? unsigned.Substring(0, decimalIndex) : unsigned;
        var fraction = decimalIndex >= 0 ? unsigned.Substring(decimalIndex + 1) : string.Empty;

        if (_options.StripLeadingZeroes && integer.Length > 0)
        {
            var firstNonZero = 0;
            while (firstNonZero < integer.Length - 1 && integer[firstNonZero] == '0') firstNonZero++;
            integer = integer.Substring(firstNonZero);
        }

        if (decimalIndex >= 0 && integer.Length == 0) integer = "0";
        if (_options.IntegerScale > 0 && integer.Length > _options.IntegerScale)
        {
            integer = integer.Substring(0, _options.IntegerScale);
        }

        if (fraction.Length > _options.DecimalScale)
        {
            fraction = fraction.Substring(0, _options.DecimalScale);
        }

        var result = integer;
        if (decimalIndex >= 0 && _options.DecimalScale > 0) result += "." + fraction;
        if (negative && result.Length > 0) result = "-" + result;
        else if (negative && unsigned.Length == 0) result = "-";
        return result;
    }

    private string Group(string digits)
    {
        if (_options.Delimiter.Length == 0 || digits.Length == 0 || _options.ThousandsGroupStyle == BootstrapNumeralGroupStyle.None)
        {
            return digits;
        }

        switch (_options.ThousandsGroupStyle)
        {
            case BootstrapNumeralGroupStyle.Thousand:
                return GroupFromRight(digits, 3, 3);
            case BootstrapNumeralGroupStyle.Lakh:
                return GroupFromRight(digits, 3, 2);
            case BootstrapNumeralGroupStyle.Wan:
                return GroupFromRight(digits, 4, 4);
            default:
                throw new InvalidOperationException("Unsupported numeral grouping style.");
        }
    }

    private string GroupFromRight(string digits, int finalWidth, int repeatedWidth)
    {
        var groups = new List<string>();
        var end = digits.Length;
        var width = finalWidth;
        while (end > 0)
        {
            var start = Math.Max(0, end - width);
            groups.Add(digits.Substring(start, end - start));
            end = start;
            width = repeatedWidth;
        }

        groups.Reverse();
        return string.Join(_options.Delimiter, groups);
    }
}
