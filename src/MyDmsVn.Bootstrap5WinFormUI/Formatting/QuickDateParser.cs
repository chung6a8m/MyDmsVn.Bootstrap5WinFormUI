using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Calendar component targeted by one keyboard step.</summary>
public enum DateSegment
{
    /// <summary>Day of month.</summary>
    Day,
    /// <summary>Month of year.</summary>
    Month,
    /// <summary>Four-digit year.</summary>
    Year
}

/// <summary>Pure date-only parse/step result; no host model mutation.</summary>
public readonly struct DateParseResult
{
    /// <summary>Creates a result.</summary>
    public DateParseResult(InputParseState state, DateTime? value, InputParseFailure? failure)
    {
        State = state;
        Value = value;
        Failure = failure;
    }
    /// <summary>Candidate classification.</summary>
    public InputParseState State { get; }
    /// <summary>Accepted date at midnight with unspecified kind, or allowed null.</summary>
    public DateTime? Value { get; }
    /// <summary>Failure when the candidate cannot be accepted.</summary>
    public InputParseFailure? Failure { get; }
}

/// <summary>Strict fixed-DMY parser and calendar-component stepping.</summary>
public static class QuickDateParser
{
    /// <summary>Completes d or d/M from the supplied captured local reference date.</summary>
    public static DateParseResult TryParsePending(string pendingText, DateInputProfile profile, DateTime referenceDate)
    {
        if (pendingText == null) throw new ArgumentNullException(nameof(pendingText));
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        var candidate = pendingText.Trim();
        var offset = pendingText.Length - pendingText.TrimStart().Length;
        if (candidate.Length == 0)
            return profile.IsNullable ? Valid(null) : Fail(InputParseState.Empty, InputParseFailureCode.Required, offset);

        var parts = candidate.Split('/');
        if (parts.Length < 1 || parts.Length > 3)
            return Fail(InputParseState.Invalid, InputParseFailureCode.Syntax, offset);
        var position = offset;
        if (!TryComponent(parts[0], 1, 2, out var day))
            return Fail(parts[0].Length == 0 ? InputParseState.Incomplete : InputParseState.Invalid,
                parts[0].Length == 0 ? InputParseFailureCode.Incomplete : InputParseFailureCode.Syntax, position);
        position += parts[0].Length + 1;
        var month = referenceDate.Month;
        if (parts.Length > 1 && !TryComponent(parts[1], 1, 2, out month))
            return Fail(parts[1].Length == 0 ? InputParseState.Incomplete : InputParseState.Invalid,
                parts[1].Length == 0 ? InputParseFailureCode.Incomplete : InputParseFailureCode.Syntax, position);
        position += parts.Length > 1 ? parts[1].Length + 1 : 0;
        var year = referenceDate.Year;
        if (parts.Length > 2 && !TryComponent(parts[2], 4, 4, out year))
            return Fail(parts[2].Length == 0 ? InputParseState.Incomplete : InputParseState.Invalid,
                parts[2].Length == 0 ? InputParseFailureCode.Incomplete : InputParseFailureCode.Syntax, position);
        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return Fail(InputParseState.Invalid, InputParseFailureCode.OutOfRange, offset);
        return WithinBounds(new DateTime(year, month, day), profile);
    }

    /// <summary>Steps one selected component, preserving day for month/year changes.</summary>
    public static DateParseResult Step(DateTime current, DateSegment segment, int direction, DateInputProfile profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (direction != -1 && direction != 1) throw new ArgumentOutOfRangeException(nameof(direction));
        var date = current.Date;
        try
        {
            switch (segment)
            {
                case DateSegment.Day:
                    date = date.AddDays(direction);
                    break;
                case DateSegment.Month:
                {
                    var monthIndex = (date.Year - 1) * 12 + date.Month - 1 + direction;
                    if (monthIndex < 0 || monthIndex >= 9999 * 12)
                        return Fail(InputParseState.Invalid, InputParseFailureCode.OutOfRange, 0);
                    var year = monthIndex / 12 + 1;
                    var month = monthIndex % 12 + 1;
                    if (date.Day > DateTime.DaysInMonth(year, month))
                        return Fail(InputParseState.Invalid, InputParseFailureCode.OutOfRange, 0);
                    date = new DateTime(year, month, date.Day);
                    break;
                }
                case DateSegment.Year:
                {
                    var year = date.Year + direction;
                    if (year < 1 || year > 9999 || date.Day > DateTime.DaysInMonth(year, date.Month))
                        return Fail(InputParseState.Invalid, InputParseFailureCode.OutOfRange, 0);
                    date = new DateTime(year, date.Month, date.Day);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(segment));
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return Fail(InputParseState.Invalid, InputParseFailureCode.OutOfRange, 0);
        }
        return WithinBounds(date, profile);
    }

    private static bool TryComponent(string part, int minLength, int maxLength, out int value)
    {
        value = 0;
        if (part.Length < minLength || part.Length > maxLength) return false;
        foreach (var ch in part)
        {
            if (ch < '0' || ch > '9') return false;
            value = value * 10 + ch - '0';
        }
        return true;
    }

    private static DateParseResult WithinBounds(DateTime value, DateInputProfile profile)
    {
        var date = DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);
        return date < profile.Minimum || date > profile.Maximum
            ? Fail(InputParseState.Invalid, InputParseFailureCode.OutOfRange, 0)
            : Valid(date);
    }

    private static DateParseResult Valid(DateTime? value) => new DateParseResult(InputParseState.Valid, value, null);
    private static DateParseResult Fail(InputParseState state, InputParseFailureCode code, int position) =>
        new DateParseResult(state, null, new InputParseFailure(code, position));
}
