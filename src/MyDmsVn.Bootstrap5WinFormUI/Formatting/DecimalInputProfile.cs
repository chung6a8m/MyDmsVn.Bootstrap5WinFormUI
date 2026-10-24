using System;
using System.Globalization;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Immutable culture, nullability, effective scale and value bounds for decimal input.</summary>
public sealed class DecimalInputProfile
{
    private readonly CultureInfo _culture;

    /// <summary>Creates a profile with caller-owned field limits.</summary>
    public DecimalInputProfile(CultureInfo culture, bool isNullable, int scale, decimal minimum, decimal maximum)
    {
        if (culture == null) throw new ArgumentNullException(nameof(culture));
        if (scale < 0 || scale > 28) throw new ArgumentOutOfRangeException(nameof(scale));
        if (minimum > maximum) throw new ArgumentException("Minimum must not exceed maximum.", nameof(minimum));
        _culture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        IsNullable = isNullable;
        Scale = scale;
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>A defensive read-only clone of the culture captured at construction.</summary>
    public CultureInfo Culture => CultureInfo.ReadOnly((CultureInfo)_culture.Clone());
    /// <summary>Whether blank input is a valid null.</summary>
    public bool IsNullable { get; }
    /// <summary>Maximum effective fractional decimal places.</summary>
    public int Scale { get; }
    /// <summary>Inclusive lower value bound.</summary>
    public decimal Minimum { get; }
    /// <summary>Inclusive upper value bound.</summary>
    public decimal Maximum { get; }
}
