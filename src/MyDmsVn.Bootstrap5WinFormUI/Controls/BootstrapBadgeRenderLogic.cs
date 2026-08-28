using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapBadgePalette
{
    public BootstrapBadgePalette(Color background, Color foreground)
    {
        Background = background;
        Foreground = foreground;
    }

    public Color Background { get; }

    public Color Foreground { get; }
}

internal static class BootstrapBadgeRenderLogic
{
    public static BootstrapBadgePalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        Color customColor,
        bool enabled)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        var background = customColor.IsEmpty
            ? BootstrapVariantColorResolver.Resolve(colors, variant)
            : customColor;

        if (!enabled)
        {
            return new BootstrapBadgePalette(
                ColorUtil.Blend(background, colors.Surface, 0.45f),
                colors.MutedText);
        }

        var foreground = ColorUtil.GetContrastingTextColor(background, colors.Light, colors.Dark);
        return new BootstrapBadgePalette(background, foreground);
    }

    public static Padding GetPadding(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        return DpiScaler.Scale(
            new Padding(metrics.SpacingSM, metrics.SpacingXS, metrics.SpacingSM, metrics.SpacingXS),
            dpi);
    }

    public static Size GetPreferredSize(Size textSize, BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (textSize.Width < 0 || textSize.Height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textSize), textSize, "Text size cannot contain negative dimensions.");
        }

        var padding = GetPadding(metrics, dpi);
        return new Size(
            textSize.Width + padding.Horizontal,
            textSize.Height + padding.Vertical);
    }

    public static float GetRadius(
        int physicalHeight,
        BootstrapThemeMetrics metrics,
        bool pill,
        int borderRadius,
        int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (physicalHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(physicalHeight), physicalHeight, "Physical height cannot be negative.");
        }

        if (borderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(borderRadius), borderRadius, "Border radius must be -1 or a non-negative value.");
        }

        if (pill)
        {
            return physicalHeight / 2f;
        }

        var logicalRadius = borderRadius >= 0 ? borderRadius : metrics.Radius;
        return DpiScaler.Scale((float)logicalRadius, dpi);
    }
}
