using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Compatibility;

internal static class NumericUtil
{
    internal static int Clamp(int value, int minimum, int maximum)
    {
        ValidateRange(minimum, maximum);

        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    internal static double Clamp(double value, double minimum, double maximum)
    {
        ValidateRange(minimum, maximum);

        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static void ValidateRange<T>(T minimum, T maximum)
        where T : IComparable<T>
    {
        if (minimum.CompareTo(maximum) > 0)
        {
            throw new ArgumentException("Minimum cannot be greater than maximum.", nameof(minimum));
        }
    }
}
