using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Immutable date-only nullability and inclusive bounds.</summary>
public sealed class DateInputProfile
{
    /// <summary>Captures nullability and date-only inclusive bounds.</summary>
    public DateInputProfile(bool isNullable, DateTime minimum, DateTime maximum)
    {
        if (minimum.Date > maximum.Date) throw new ArgumentException("Minimum must not exceed maximum.", nameof(minimum));
        IsNullable = isNullable;
        Minimum = DateTime.SpecifyKind(minimum.Date, DateTimeKind.Unspecified);
        Maximum = DateTime.SpecifyKind(maximum.Date, DateTimeKind.Unspecified);
    }

    /// <summary>Whether blank input can produce null.</summary>
    public bool IsNullable { get; }
    /// <summary>Inclusive date-only lower bound.</summary>
    public DateTime Minimum { get; }
    /// <summary>Inclusive date-only upper bound.</summary>
    public DateTime Maximum { get; }
}
