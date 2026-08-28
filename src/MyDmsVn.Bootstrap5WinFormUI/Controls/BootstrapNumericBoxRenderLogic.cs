using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapNumericBoxMetrics
{
    public BootstrapNumericBoxMetrics(
        int horizontalPadding,
        float borderWidth,
        float focusBorderWidth,
        float radius)
    {
        HorizontalPadding = horizontalPadding;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int HorizontalPadding { get; }

    public float BorderWidth { get; }

    public float FocusBorderWidth { get; }

    public float Radius { get; }
}

internal readonly struct BootstrapNumericBoxPalette
{
    public BootstrapNumericBoxPalette(Color background, Color foreground, Color border)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
    }

    public Color Background { get; }

    public Color Foreground { get; }

    public Color Border { get; }
}

internal static class BootstrapNumericBoxRenderLogic
{
    public static BootstrapNumericBoxMetrics ResolveMetrics(
        BootstrapThemeMetrics metrics,
        int dpi,
        int borderRadius)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        if (borderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(borderRadius), borderRadius, "Border radius must be -1 or a non-negative value.");
        }

        var logicalRadius = borderRadius >= 0 ? borderRadius : metrics.Radius;
        return new BootstrapNumericBoxMetrics(
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale((float)metrics.BorderWidth, dpi),
            DpiScaler.Scale((float)metrics.FocusBorderWidth, dpi),
            DpiScaler.Scale((float)logicalRadius, dpi));
    }

    public static BootstrapNumericBoxPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapValidationState validationState,
        bool containsFocus,
        bool enabled,
        bool readOnly)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        var border = BootstrapTextBoxRenderLogic.ResolveBorderColor(
            colors,
            validationState,
            containsFocus,
            enabled);

        return new BootstrapNumericBoxPalette(
            enabled && !readOnly ? colors.Surface : colors.SurfaceSecondary,
            enabled ? colors.Text : colors.MutedText,
            border);
    }

    public static Rectangle CalculateNativeBounds(
        Size clientSize,
        int nativePreferredHeight,
        BootstrapNumericBoxMetrics metrics)
    {
        if (nativePreferredHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nativePreferredHeight),
                nativePreferredHeight,
                "Native preferred height must be greater than zero.");
        }

        if (clientSize.Width <= 0 || clientSize.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var padding = Math.Max(0, metrics.HorizontalPadding);
        var maximumPadding = Math.Max(0, (clientSize.Width - 1) / 2);
        var effectivePadding = Math.Min(padding, maximumPadding);
        var width = Math.Max(1, clientSize.Width - (effectivePadding * 2));
        var height = Math.Min(nativePreferredHeight, clientSize.Height);
        var top = Math.Max(0, (clientSize.Height - height) / 2);

        return new Rectangle(effectivePadding, top, width, height);
    }
}
