using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

internal readonly struct FormattedTextSnapshot : IEquatable<FormattedTextSnapshot>
{
    internal FormattedTextSnapshot(string rawValue, int rawSelectionStart, int rawSelectionLength)
    {
        RawValue = rawValue ?? string.Empty;
        RawSelectionStart = Math.Max(0, Math.Min(RawValue.Length, rawSelectionStart));
        RawSelectionLength = Math.Max(0, Math.Min(RawValue.Length - RawSelectionStart, rawSelectionLength));
    }

    internal string RawValue { get; }

    internal int RawSelectionStart { get; }

    internal int RawSelectionLength { get; }

    public bool Equals(FormattedTextSnapshot other) =>
        RawValue == other.RawValue &&
        RawSelectionStart == other.RawSelectionStart &&
        RawSelectionLength == other.RawSelectionLength;

    public override bool Equals(object? obj) => obj is FormattedTextSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = RawValue.GetHashCode();
            hash = (hash * 397) ^ RawSelectionStart;
            return (hash * 397) ^ RawSelectionLength;
        }
    }
}
