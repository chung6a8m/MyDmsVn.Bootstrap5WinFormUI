using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Reason a decimal candidate cannot yet be accepted.</summary>
public enum InputParseFailureCode
{
    /// <summary>Required input is blank.</summary>
    Required,
    /// <summary>Input is a valid prefix without digits.</summary>
    Incomplete,
    /// <summary>Input contains unsupported lexical content.</summary>
    Syntax,
    /// <summary>Meaningful fractional precision exceeds the profile.</summary>
    Precision,
    /// <summary>The exact value is outside the profile's bounds.</summary>
    OutOfRange,
    /// <summary>The exact value cannot be represented by decimal.</summary>
    Unrepresentable
}

/// <summary>Structured failure with a code and candidate-text offset.</summary>
public readonly struct InputParseFailure
{
    /// <summary>Creates a failure at a zero-based offset.</summary>
    public InputParseFailure(InputParseFailureCode code, int position)
    {
        Code = code;
        Position = position;
    }
    /// <summary>Failure category.</summary>
    public InputParseFailureCode Code { get; }
    /// <summary>Zero-based offset in the original pending text.</summary>
    public int Position { get; }
}

/// <summary>Pure parsing result; it does not commit a host value.</summary>
public readonly struct DecimalParseResult
{
    /// <summary>Creates a result.</summary>
    public DecimalParseResult(InputParseState state, decimal? value, InputParseFailure? failure)
    {
        State = state;
        Value = value;
        Failure = failure;
    }
    /// <summary>Lexical and policy state.</summary>
    public InputParseState State { get; }
    /// <summary>Exact value only when valid; null also represents an allowed blank.</summary>
    public decimal? Value { get; }
    /// <summary>Failure when input cannot be accepted.</summary>
    public InputParseFailure? Failure { get; }
}

/// <summary>Pure, lossless decimal candidate parser.</summary>
public static class DecimalInputParser
{
    /// <summary>Parses pending text under a captured profile without modifying it.</summary>
    public static DecimalParseResult TryParsePending(string pendingText, DecimalInputProfile profile)
    {
        if (pendingText == null) throw new ArgumentNullException(nameof(pendingText));
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        var candidate = pendingText.Trim();
        var offset = pendingText.Length - pendingText.TrimStart().Length;
        if (candidate.Length == 0)
            return profile.IsNullable
                ? new DecimalParseResult(InputParseState.Valid, null, null)
                : Fail(InputParseState.Empty, InputParseFailureCode.Required, offset);

        var format = profile.Culture.NumberFormat;
        var separator = format.NumberDecimalSeparator;
        var index = 0;
        var negative = false;
        if (!string.IsNullOrEmpty(format.NegativeSign) && candidate.StartsWith(format.NegativeSign, StringComparison.Ordinal))
        {
            negative = true;
            index = format.NegativeSign.Length;
        }
        else if (!string.IsNullOrEmpty(format.PositiveSign) && candidate.StartsWith(format.PositiveSign, StringComparison.Ordinal))
            index = format.PositiveSign.Length;

        var integerStart = index;
        while (index < candidate.Length && IsDigit(candidate[index])) index++;
        var integerEnd = index;
        var fractionalStart = -1;
        if (!string.IsNullOrEmpty(separator) && candidate.Length - index >= separator.Length &&
            string.CompareOrdinal(candidate, index, separator, 0, separator.Length) == 0)
        {
            index += separator.Length;
            fractionalStart = index;
            while (index < candidate.Length && IsDigit(candidate[index])) index++;
        }

        if (index != candidate.Length)
            return Fail(InputParseState.Invalid, InputParseFailureCode.Syntax, offset + index);
        if (integerEnd == integerStart && (fractionalStart < 0 || index == fractionalStart))
            return Fail(InputParseState.Incomplete, InputParseFailureCode.Incomplete, offset + index);

        var effectiveScale = 0;
        if (fractionalStart >= 0)
        {
            for (var digit = index - 1; digit >= fractionalStart; digit--)
            {
                if (candidate[digit] == '0') continue;
                effectiveScale = digit - fractionalStart + 1;
                break;
            }
            if (effectiveScale > profile.Scale || effectiveScale > 28)
                return Fail(InputParseState.Invalid, InputParseFailureCode.Precision, offset + fractionalStart + profile.Scale);
        }

        // decimal.TryParse may round a far-tail digit. Build the coefficient exactly,
        // then attach the proven scale through decimal's 96-bit constructor.
        decimal coefficient = 0m;
        try
        {
            for (var digit = integerStart; digit < integerEnd; digit++)
                coefficient = checked(coefficient * 10m + (candidate[digit] - '0'));
            for (var digit = fractionalStart; digit >= 0 && digit < fractionalStart + effectiveScale; digit++)
                coefficient = checked(coefficient * 10m + (candidate[digit] - '0'));
        }
        catch (OverflowException)
        {
            return Fail(InputParseState.Invalid, InputParseFailureCode.Unrepresentable, offset + integerStart);
        }

        var bits = decimal.GetBits(coefficient);
        var value = new decimal(bits[0], bits[1], bits[2], negative, (byte)effectiveScale);
        if (value < profile.Minimum || value > profile.Maximum)
            return Fail(InputParseState.Invalid, InputParseFailureCode.OutOfRange, offset + integerStart);
        return new DecimalParseResult(InputParseState.Valid, value, null);
    }

    private static DecimalParseResult Fail(InputParseState state, InputParseFailureCode code, int position) =>
        new DecimalParseResult(state, null, new InputParseFailure(code, position));

    private static bool IsDigit(char value) => value >= '0' && value <= '9';
}
