using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapPlaceholderRenderLogic
{
    internal const int DefaultLogicalWidth = 100;
    internal const double OpacityMax = 0.5;
    internal const double OpacityMin = 0.2;
    internal const double WaveMaskMax = 1.0;
    internal const double WaveMaskMin = 0.8;
    internal const float WaveAngleDegrees = 130f;

    internal static Size GetPreferredSize(
        int fontHeight,
        BootstrapPlaceholderSize size,
        int dpi,
        Size proposedSize)
    {
        if (fontHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontHeight), fontHeight, "Font height must be greater than zero.");
        }

        var multiplier = GetSizeMultiplier(size);
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        var baseWidth = DpiScaler.Scale(DefaultLogicalWidth, dpi);
        var width = proposedSize.Width > 0
            ? Math.Min(baseWidth, proposedSize.Width)
            : baseWidth;
        var height = Math.Max(1, (int)Math.Ceiling(fontHeight * multiplier));
        return new Size(width, height);
    }

    internal static Color ResolveBaseColor(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        Color customColor,
        bool enabled)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        ValidateCustomColor(customColor);
        var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
        if (!enabled)
        {
            return colors.Disabled;
        }

        return customColor.IsEmpty ? semantic : customColor;
    }

    internal static double GetGlowOpacity(double progress)
    {
        progress = ClampNormalizedFinite(progress, nameof(progress));
        var wave = 0.5 + (0.5 * Math.Cos(2.0 * Math.PI * progress));
        return OpacityMin + ((OpacityMax - OpacityMin) * wave);
    }

    internal static double GetWaveOpacity(double position, double progress)
    {
        position = ClampNormalizedFinite(position, nameof(position));
        progress = ClampNormalizedFinite(progress, nameof(progress));

        const double startCenter = -0.25;
        const double travel = 1.50;
        const double halfWidth = 0.20;

        var center = startCenter + (travel * progress);
        var distance = Math.Abs(position - center);
        if (distance >= halfWidth)
        {
            return OpacityMax;
        }

        var t = distance / halfWidth;
        var smooth = t * t * (3.0 - (2.0 * t));
        var mask = WaveMaskMin + ((WaveMaskMax - WaveMaskMin) * smooth);
        return OpacityMax * mask;
    }

    internal static Color ApplyOpacity(Color color, double opacity)
    {
        opacity = ClampNormalizedFinite(opacity, nameof(opacity));
        var alpha = Convert.ToByte(Math.Round(255.0 * opacity, MidpointRounding.AwayFromZero));
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    internal static float GetRadius(BootstrapThemeMetrics metrics, int borderRadius, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (borderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(borderRadius), borderRadius, "Border radius must be -1 or a non-negative value.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        var logicalRadius = borderRadius == -1 ? metrics.Radius : borderRadius;
        return DpiScaler.Scale((float)logicalRadius, dpi);
    }

    private static double GetSizeMultiplier(BootstrapPlaceholderSize size)
    {
        switch (size)
        {
            case BootstrapPlaceholderSize.ExtraSmall:
                return 0.6;
            case BootstrapPlaceholderSize.Small:
                return 0.8;
            case BootstrapPlaceholderSize.Default:
                return 1.0;
            case BootstrapPlaceholderSize.Large:
                return 1.2;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported placeholder size.");
        }
    }

    private static double ClampNormalizedFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
        }

        if (value < 0.0)
        {
            return 0.0;
        }

        return value > 1.0 ? 1.0 : value;
    }

    private static void ValidateCustomColor(Color color)
    {
        if (!color.IsEmpty && color.A != byte.MaxValue)
        {
            throw new ArgumentException("Custom color must be Color.Empty or fully opaque.", nameof(color));
        }
    }
}
