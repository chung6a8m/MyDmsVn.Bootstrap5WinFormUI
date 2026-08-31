using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

internal static class InputCaretMapper
{
    internal static int ToRawPosition(IInputFormatter formatter, string formattedText, int formattedPosition)
    {
        if (formatter is null) throw new ArgumentNullException(nameof(formatter));
        var display = formattedText ?? string.Empty;
        var position = Math.Max(0, Math.Min(display.Length, formattedPosition));
        return formatter.Unformat(display.Substring(0, position) ?? string.Empty).Length;
    }

    internal static int ToFormattedPosition(IInputFormatter formatter, string rawValue, int rawPosition)
    {
        if (formatter is null) throw new ArgumentNullException(nameof(formatter));
        var raw = rawValue ?? string.Empty;
        var position = Math.Max(0, Math.Min(raw.Length, rawPosition));
        var finalDisplay = formatter.Format(raw) ?? string.Empty;
        if (position == raw.Length) return finalDisplay.Length;
        var prefixDisplay = formatter.Format(raw.Substring(0, position)) ?? string.Empty;
        return Math.Max(0, Math.Min(finalDisplay.Length, prefixDisplay.Length));
    }
}
