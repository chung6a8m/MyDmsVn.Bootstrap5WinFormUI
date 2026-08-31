using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Formats partial time strings according to a structural component pattern.</summary>
public sealed class BootstrapTimeInputFormatter : IInputFormatter
{
    private readonly BootstrapTimeFormatOptions _options;

    /// <summary>Initializes a formatter backed by the supplied mutable options.</summary>
    /// <param name="options">The options to read for each operation.</param>
    public BootstrapTimeInputFormatter(BootstrapTimeFormatOptions options)
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
        for (var index = 0; index < widths.Length; index++) widths[index] = 2;
        return widths;
    }

    private string ShapeComponent(char component, string value)
    {
        if (value.Length != 2) return value;
        if (!InputFormatOptionValidation.IsAsciiDigit(value[0]) ||
            !InputFormatOptionValidation.IsAsciiDigit(value[1])) return value;
        var parsed = ((value[0] - '0') * 10) + (value[1] - '0');
        if (component == 'h')
        {
            var minimum = _options.TimeFormat == BootstrapTimeFormat.TwelveHour ? 1 : 0;
            var maximum = _options.TimeFormat == BootstrapTimeFormat.TwelveHour ? 12 : 23;
            parsed = Math.Max(minimum, Math.Min(maximum, parsed));
        }
        else
        {
            parsed = Math.Max(0, Math.Min(59, parsed));
        }

        return parsed.ToString("00");
    }
}
