using MyDmsVn.Bootstrap5WinFormUI.Compatibility;

namespace MyDmsVn.Bootstrap5WinFormUI.Animation;

/// <summary>
/// Provides dependency-free easing functions for normalized animation progress.
/// </summary>
public static class BootstrapEasing
{
    /// <summary>Returns linear normalized progress.</summary>
    public static double Linear(double progress)
    {
        return Normalize(progress);
    }

    /// <summary>Applies a quadratic ease-in curve.</summary>
    public static double EaseIn(double progress)
    {
        var value = Normalize(progress);
        return value * value;
    }

    /// <summary>Applies a quadratic ease-out curve.</summary>
    public static double EaseOut(double progress)
    {
        var value = Normalize(progress);
        var inverse = 1.0 - value;
        return 1.0 - (inverse * inverse);
    }

    /// <summary>Applies a symmetric quadratic ease-in/ease-out curve.</summary>
    public static double EaseInOut(double progress)
    {
        var value = Normalize(progress);
        if (value < 0.5)
        {
            return 2.0 * value * value;
        }

        var inverse = -2.0 * value + 2.0;
        return 1.0 - ((inverse * inverse) / 2.0);
    }

    internal static double Normalize(double progress)
    {
        return NumericUtil.Clamp(progress, 0.0, 1.0);
    }
}
