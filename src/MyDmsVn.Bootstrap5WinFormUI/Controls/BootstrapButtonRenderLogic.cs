using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal enum BootstrapButtonVisualState
{
    Normal,
    Hover,
    Pressed
}

internal readonly struct BootstrapButtonPalette
{
    public BootstrapButtonPalette(Color background, Color border, Color foreground)
    {
        Background = background;
        Border = border;
        Foreground = foreground;
    }

    public Color Background { get; }

    public Color Border { get; }

    public Color Foreground { get; }
}

internal static class BootstrapButtonRenderLogic
{
    public static int GetLogicalHeight(BootstrapThemeMetrics metrics, BootstrapButtonSize size)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        switch (size)
        {
            case BootstrapButtonSize.Small:
                return metrics.ControlHeightSmall;
            case BootstrapButtonSize.Default:
                return metrics.ControlHeight;
            case BootstrapButtonSize.Large:
                return metrics.ControlHeightLarge;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported button size.");
        }
    }

    public static int GetLogicalHorizontalPadding(BootstrapThemeMetrics metrics, BootstrapButtonSize size)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        switch (size)
        {
            case BootstrapButtonSize.Small:
                return metrics.SpacingSM;
            case BootstrapButtonSize.Default:
                return metrics.SpacingMD;
            case BootstrapButtonSize.Large:
                return metrics.SpacingLG;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported button size.");
        }
    }

    public static int GetLogicalIconSize(BootstrapThemeMetrics metrics, BootstrapButtonSize size)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        switch (size)
        {
            case BootstrapButtonSize.Small:
                return metrics.SpacingMD;
            case BootstrapButtonSize.Default:
                return metrics.SpacingLG;
            case BootstrapButtonSize.Large:
                return metrics.SpacingXL;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported button size.");
        }
    }

    public static int GetLogicalContentSpacing(BootstrapThemeMetrics metrics, BootstrapButtonSize size)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        switch (size)
        {
            case BootstrapButtonSize.Small:
                return metrics.SpacingXS;
            case BootstrapButtonSize.Default:
            case BootstrapButtonSize.Large:
                return metrics.SpacingSM;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported button size.");
        }
    }

    public static int GetThemeBorderRadius(BootstrapThemeMetrics metrics, BootstrapButtonSize size)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        switch (size)
        {
            case BootstrapButtonSize.Small:
                return metrics.RadiusSmall;
            case BootstrapButtonSize.Default:
                return metrics.Radius;
            case BootstrapButtonSize.Large:
                return metrics.RadiusLarge;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported button size.");
        }
    }

    public static BootstrapButtonPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool outline,
        bool enabled,
        bool selected,
        BootstrapButtonVisualState visualState)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        if (visualState < BootstrapButtonVisualState.Normal || visualState > BootstrapButtonVisualState.Pressed)
        {
            throw new ArgumentOutOfRangeException(nameof(visualState), visualState, "Unsupported button visual state.");
        }

        if (!enabled)
        {
            return new BootstrapButtonPalette(colors.Surface, colors.Disabled, colors.MutedText);
        }

        var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
        var active = selected || visualState == BootstrapButtonVisualState.Pressed;

        if (outline && !active)
        {
            var background = visualState == BootstrapButtonVisualState.Hover
                ? ColorUtil.Blend(semantic, colors.Surface, 0.12f)
                : colors.Surface;
            return new BootstrapButtonPalette(background, semantic, semantic);
        }

        var backgroundColor = semantic;
        if (active)
        {
            backgroundColor = ColorUtil.Blend(colors.Dark, semantic, 0.18f);
        }
        else if (visualState == BootstrapButtonVisualState.Hover)
        {
            backgroundColor = ColorUtil.Blend(colors.Dark, semantic, 0.10f);
        }

        var foreground = ColorUtil.GetContrastingTextColor(backgroundColor, colors.Light, colors.Dark);
        return new BootstrapButtonPalette(backgroundColor, backgroundColor, foreground);
    }
}
