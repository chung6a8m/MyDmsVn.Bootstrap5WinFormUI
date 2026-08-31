using System;
using System.Collections.Generic;
using System.Text;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Formats partial date strings according to a structural component pattern.</summary>
public sealed class BootstrapDateInputFormatter : IInputFormatter
{
    private readonly BootstrapDateFormatOptions _options;

    /// <summary>Initializes a formatter backed by the supplied mutable options.</summary>
    /// <param name="options">The options to read for each operation.</param>
    public BootstrapDateInputFormatter(BootstrapDateFormatOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Format(string rawValue)
    {
        var raw = Normalize(rawValue);
        return StructuredInputFormatterLogic.Format(raw, GetWidths(), _options.Delimiter, _options.DelimiterLazyShow);
    }

    /// <inheritdoc />
    public string Unformat(string formattedValue) => Normalize(formattedValue);

    private string Normalize(string? value)
    {
        return StructuredInputFormatterLogic.Normalize(value, _options.Pattern, GetWidths(), ShapeComponent);
    }

    private int[] GetWidths()
    {
        var widths = new int[_options.Pattern.Length];
        for (var index = 0; index < widths.Length; index++) widths[index] = _options.Pattern[index] == 'Y' ? 4 : 2;
        return widths;
    }

    private static string ShapeComponent(char component, string value)
    {
        if (value.Length != 2 || (component != 'd' && component != 'm')) return value;
        if (!InputFormatOptionValidation.IsAsciiDigit(value[0]) ||
            !InputFormatOptionValidation.IsAsciiDigit(value[1])) return value;
        var parsed = ((value[0] - '0') * 10) + (value[1] - '0');
        var maximum = component == 'd' ? 31 : 12;
        parsed = Math.Max(1, Math.Min(maximum, parsed));
        return parsed.ToString("00");
    }
}

internal static class StructuredInputFormatterLogic
{
    internal static string Normalize(string? value, string pattern, int[] widths, Func<char, string, string> shape)
    {
        var candidate = value ?? string.Empty;
        var digits = new StringBuilder(candidate.Length);
        foreach (var character in candidate)
        {
            if (InputFormatOptionValidation.IsAsciiDigit(character)) digits.Append(character);
        }

        var capacity = 0;
        foreach (var width in widths) capacity += width;
        if (digits.Length > capacity) digits.Length = capacity;

        var result = new StringBuilder(digits.Length);
        var position = 0;
        for (var index = 0; index < widths.Length && position < digits.Length; index++)
        {
            var length = Math.Min(widths[index], digits.Length - position);
            var component = digits.ToString(position, length);
            result.Append(shape(pattern[index], component));
            position += length;
        }

        return result.ToString();
    }

    internal static string Format(string raw, IReadOnlyList<int> widths, string delimiter, bool lazy)
    {
        if (raw.Length == 0) return string.Empty;
        var result = new StringBuilder(raw.Length + delimiter.Length * widths.Count);
        var position = 0;
        for (var index = 0; index < widths.Count && position < raw.Length; index++)
        {
            var length = Math.Min(widths[index], raw.Length - position);
            result.Append(raw, position, length);
            position += length;
            if (index < widths.Count - 1 && length == widths[index] && (position < raw.Length || !lazy))
            {
                result.Append(delimiter);
            }
        }

        return result.ToString();
    }
}
