using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapListGroupVisualState
{
    Neutral,
    Hover,
    Pressed,
    Active,
    Disabled
}

internal readonly struct BootstrapListGroupPalette
{
    public BootstrapListGroupPalette(Color surface, Color border, Color foreground, Color focus)
    {
        Surface = surface;
        Border = border;
        Foreground = foreground;
        Focus = focus;
    }

    public Color Surface { get; }
    public Color Border { get; }
    public Color Foreground { get; }
    public Color Focus { get; }
}

internal static class BootstrapListGroupRenderLogic
{
    private const float ContextualSurfaceAmount = 0.14f;
    private const float ContextualBorderAmount = 0.42f;

    public static BootstrapListGroupVisualState ResolveState(
        bool enabled,
        bool active,
        bool actionable,
        bool pressed,
        bool hovered)
    {
        if (!enabled)
        {
            return BootstrapListGroupVisualState.Disabled;
        }

        if (active)
        {
            return BootstrapListGroupVisualState.Active;
        }

        if (actionable && pressed)
        {
            return BootstrapListGroupVisualState.Pressed;
        }

        return actionable && hovered
            ? BootstrapListGroupVisualState.Hover
            : BootstrapListGroupVisualState.Neutral;
    }

    public static BootstrapListGroupPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant? variant,
        BootstrapListGroupVisualState state)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        if (variant.HasValue && (variant.Value < BootstrapVariant.Primary || variant.Value > BootstrapVariant.Dark))
        {
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported Bootstrap variant.");
        }

        Color surface;
        Color border;
        switch (state)
        {
            case BootstrapListGroupVisualState.Disabled:
                return new BootstrapListGroupPalette(colors.SurfaceSecondary, colors.Border, colors.MutedText, colors.Disabled);
            case BootstrapListGroupVisualState.Active:
                surface = colors.Primary;
                border = colors.Primary;
                break;
            case BootstrapListGroupVisualState.Pressed:
                surface = colors.Active;
                border = colors.Border;
                break;
            case BootstrapListGroupVisualState.Hover:
                surface = colors.Hover;
                border = colors.Border;
                break;
            default:
                if (variant.HasValue)
                {
                    var semantic = BootstrapVariantColorResolver.Resolve(colors, variant.Value);
                    surface = ColorUtil.Blend(semantic, colors.Surface, ContextualSurfaceAmount);
                    border = ColorUtil.Blend(semantic, colors.Border, ContextualBorderAmount);
                }
                else
                {
                    surface = colors.Surface;
                    border = colors.Border;
                }

                break;
        }

        var themeForeground = ColorUtil.GetContrastingTextColor(surface, colors.Light, colors.Dark);
        var strictForeground = ColorUtil.GetContrastingTextColor(surface, Color.White, Color.Black);
        var foreground = ColorUtil.GetContrastRatio(themeForeground, surface) >= 4.5d
            ? themeForeground
            : strictForeground;
        return new BootstrapListGroupPalette(surface, border, foreground, colors.Focus);
    }

    public static Padding GetContentPadding(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        return DpiScaler.Scale(new Padding(metrics.SpacingMD, metrics.SpacingSM, metrics.SpacingMD, metrics.SpacingSM), dpi);
    }

    public static Size GetPreferredSize(Size textSize, Size contentSize, Padding padding)
    {
        return new Size(
            Math.Max(0, Math.Max(textSize.Width, contentSize.Width)) + Math.Max(0, padding.Horizontal),
            Math.Max(0, Math.Max(textSize.Height, contentSize.Height)) + Math.Max(0, padding.Vertical));
    }

    public static int ResolveRadius(BootstrapThemeMetrics metrics, int borderRadius, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (borderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(borderRadius));
        }

        return DpiScaler.Scale(borderRadius < 0 ? metrics.Radius : borderRadius, dpi);
    }

    public static int GetSeamOverlap(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        return Math.Max(0, DpiScaler.Scale(metrics.BorderWidth, dpi));
    }

    public static CornerRadius GetCornerRadius(
        Orientation orientation,
        int index,
        int count,
        bool flush,
        float radius)
    {
        if (count <= 0 || index < 0 || index >= count || radius <= 0f)
        {
            return CornerRadius.Empty;
        }

        if (orientation == Orientation.Vertical && flush)
        {
            return CornerRadius.Empty;
        }

        if (count == 1)
        {
            return new CornerRadius(radius);
        }

        if (orientation == Orientation.Vertical)
        {
            if (index == 0)
            {
                return new CornerRadius(radius, radius, 0f, 0f);
            }

            return index == count - 1
                ? new CornerRadius(0f, 0f, radius, radius)
                : CornerRadius.Empty;
        }

        if (index == 0)
        {
            return new CornerRadius(radius, 0f, 0f, radius);
        }

        return index == count - 1
            ? new CornerRadius(0f, radius, radius, 0f)
            : CornerRadius.Empty;
    }

    public static CornerRadius NormalizeCorners(CornerRadius corners, Size size)
    {
        return corners.NormalizeTo(new RectangleF(0f, 0f, Math.Max(0, size.Width), Math.Max(0, size.Height)));
    }
}
