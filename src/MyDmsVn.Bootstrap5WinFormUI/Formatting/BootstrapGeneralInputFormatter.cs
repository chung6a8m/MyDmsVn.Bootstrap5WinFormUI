using System;
using System.Linq;
using System.Text;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Formats general text into configurable blocks without WinForms dependencies.</summary>
public sealed class BootstrapGeneralInputFormatter : IInputFormatter
{
    private readonly BootstrapGeneralFormatOptions _options;

    /// <summary>Initializes a formatter backed by the supplied mutable options.</summary>
    /// <param name="options">The options to read for each operation.</param>
    public BootstrapGeneralInputFormatter(BootstrapGeneralFormatOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Format(string rawValue)
    {
        var raw = Normalize(rawValue);
        if (raw.Length == 0)
        {
            return string.Empty;
        }

        var blocks = _options.Blocks;
        if (blocks.Length == 0)
        {
            return _options.Prefix + raw;
        }

        var output = new StringBuilder(_options.Prefix.Length + raw.Length + blocks.Length * _options.Delimiter.Length);
        output.Append(_options.Prefix);
        var position = 0;
        for (var index = 0; index < blocks.Length && position < raw.Length; index++)
        {
            var length = Math.Min(blocks[index], raw.Length - position);
            output.Append(raw, position, length);
            position += length;

            if (index < blocks.Length - 1 && length == blocks[index] &&
                (position < raw.Length || !_options.DelimiterLazyShow))
            {
                output.Append(GetDelimiter(index));
            }
        }

        return output.ToString();
    }

    /// <inheritdoc />
    public string Unformat(string formattedValue) => Normalize(formattedValue);

    private string Normalize(string? value)
    {
        var candidate = value ?? string.Empty;
        var prefix = _options.Prefix;
        if (prefix.Length > 0 && candidate.StartsWith(prefix, StringComparison.Ordinal))
        {
            candidate = candidate.Substring(prefix.Length);
        }

        var delimiters = _options.Delimiters
            .Concat(new[] { _options.Delimiter })
            .Where(delimiter => delimiter.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(delimiter => delimiter.Length);
        foreach (var delimiter in delimiters)
        {
            candidate = candidate.Replace(delimiter, string.Empty);
        }

        if (_options.NumericOnly)
        {
            var digits = new StringBuilder(candidate.Length);
            foreach (var character in candidate)
            {
                if (char.IsDigit(character)) digits.Append(character);
            }

            candidate = digits.ToString();
        }

        if (_options.Uppercase) candidate = candidate.ToUpperInvariant();
        else if (_options.Lowercase) candidate = candidate.ToLowerInvariant();

        var blocks = _options.Blocks;
        if (blocks.Length > 0)
        {
            var capacity = blocks.Sum();
            if (candidate.Length > capacity) candidate = candidate.Substring(0, capacity);
        }

        return candidate;
    }

    private string GetDelimiter(int boundary)
    {
        var delimiters = _options.Delimiters;
        return boundary < delimiters.Length ? delimiters[boundary] : _options.Delimiter;
    }
}
