using System;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

/// <summary>
/// Provides color interpolation and WCAG-style luminance/contrast calculations.
/// </summary>
public static class ColorUtil
{
    /// <summary>
    /// Returns the relative luminance of an sRGB color in the range 0..1.
    /// Alpha is not composited and does not affect the result.
    /// </summary>
    public static double GetRelativeLuminance(Color color)
    {
        var red = ToLinear(color.R / 255d);
        var green = ToLinear(color.G / 255d);
        var blue = ToLinear(color.B / 255d);
        return (0.2126d * red) + (0.7152d * green) + (0.0722d * blue);
    }

    /// <summary>
    /// Returns the contrast ratio between two colors in the range 1..21.
    /// </summary>
    public static double GetContrastRatio(Color first, Color second)
    {
        var firstLuminance = GetRelativeLuminance(first);
        var secondLuminance = GetRelativeLuminance(second);
        var lighter = Math.Max(firstLuminance, secondLuminance);
        var darker = Math.Min(firstLuminance, secondLuminance);
        return (lighter + 0.05d) / (darker + 0.05d);
    }

    /// <summary>
    /// Chooses the candidate with the higher contrast ratio against the background.
    /// </summary>
    public static Color GetContrastingTextColor(Color background, Color lightCandidate, Color darkCandidate)
    {
        var lightContrast = GetContrastRatio(background, lightCandidate);
        var darkContrast = GetContrastRatio(background, darkCandidate);
        return lightContrast >= darkContrast ? lightCandidate : darkCandidate;
    }

    /// <summary>
    /// Linearly interpolates from the background color toward the foreground color.
    /// </summary>
    /// <param name="foreground">Color returned when <paramref name="foregroundAmount"/> is 1.</param>
    /// <param name="background">Color returned when <paramref name="foregroundAmount"/> is 0.</param>
    /// <param name="foregroundAmount">Interpolation amount in the inclusive range 0..1.</param>
    public static Color Blend(Color foreground, Color background, float foregroundAmount)
    {
        if (foregroundAmount < 0f || foregroundAmount > 1f || float.IsNaN(foregroundAmount) || float.IsInfinity(foregroundAmount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(foregroundAmount),
                foregroundAmount,
                "Blend amount must be a finite value between zero and one.");
        }

        return Color.FromArgb(
            Interpolate(background.A, foreground.A, foregroundAmount),
            Interpolate(background.R, foreground.R, foregroundAmount),
            Interpolate(background.G, foreground.G, foregroundAmount),
            Interpolate(background.B, foreground.B, foregroundAmount));
    }

    private static double ToLinear(double channel)
    {
        return channel <= 0.04045d
            ? channel / 12.92d
            : Math.Pow((channel + 0.055d) / 1.055d, 2.4d);
    }

    private static int Interpolate(int start, int end, float amount)
    {
        return (int)Math.Round(start + ((end - start) * amount), MidpointRounding.AwayFromZero);
    }
}
