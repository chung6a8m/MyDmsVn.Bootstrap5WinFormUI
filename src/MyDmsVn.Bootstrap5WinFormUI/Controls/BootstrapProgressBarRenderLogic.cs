using System;
using System.Drawing;
using System.Globalization;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapProgressBarRenderLogic
{
    public static double GetFraction(int minimum, int maximum, int value)
    {
        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Maximum must be greater than Minimum.");
        }

        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be inside the configured range.");
        }

        return ((double)value - minimum) / ((double)maximum - minimum);
    }

    public static int GetPercentage(int minimum, int maximum, int value)
    {
        return (int)Math.Round(
            GetFraction(minimum, maximum, value) * 100.0,
            MidpointRounding.AwayFromZero);
    }

    public static int InterpolateValue(int startValue, int targetValue, double progress)
    {
        if (double.IsNaN(progress) || double.IsInfinity(progress))
        {
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Progress must be finite.");
        }

        progress = Math.Max(0.0, Math.Min(1.0, progress));
        return (int)Math.Round(
            (double)startValue + (((double)targetValue - startValue) * progress),
            MidpointRounding.AwayFromZero);
    }

    public static Color ResolveFillColor(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        Color customColor)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        return customColor.IsEmpty
            ? BootstrapVariantColorResolver.Resolve(colors, variant)
            : customColor;
    }

    public static string FormatText(
        string format,
        int minimum,
        int maximum,
        int value)
    {
        if (format is null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            format,
            GetPercentage(minimum, maximum, value),
            value,
            minimum,
            maximum);
    }

    public static float ResolveRadius(
        BootstrapThemeMetrics metrics,
        int borderRadius,
        int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (borderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(borderRadius), borderRadius, "BorderRadius must be -1 or non-negative.");
        }

        var logicalRadius = borderRadius >= 0 ? borderRadius : metrics.Radius;
        return DpiScaler.Scale((float)logicalRadius, dpi);
    }

    public static RectangleF GetDeterminateFillBounds(RectangleF trackBounds, double fraction)
    {
        fraction = Math.Max(0.0, Math.Min(1.0, fraction));
        return new RectangleF(
            trackBounds.Left,
            trackBounds.Top,
            (float)(trackBounds.Width * fraction),
            trackBounds.Height);
    }

    public static RectangleF GetIndeterminateFillBounds(RectangleF trackBounds, double progress)
    {
        progress = Math.Max(0.0, Math.Min(1.0, progress));
        var segmentWidth = trackBounds.Width * 0.35f;
        var travel = trackBounds.Width + segmentWidth;
        var left = trackBounds.Left - segmentWidth + ((float)progress * travel);
        return new RectangleF(left, trackBounds.Top, segmentWidth, trackBounds.Height);
    }
}
