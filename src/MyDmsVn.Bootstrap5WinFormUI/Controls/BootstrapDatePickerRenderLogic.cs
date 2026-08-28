using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapDatePickerMetrics
{
    public BootstrapDatePickerMetrics(
        int shellPadding,
        float borderWidth,
        float focusBorderWidth,
        float radius)
    {
        ShellPadding = shellPadding;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int ShellPadding { get; }

    public float BorderWidth { get; }

    public float FocusBorderWidth { get; }

    public float Radius { get; }
}

internal readonly struct BootstrapDatePickerPalette
{
    public BootstrapDatePickerPalette(Color surface, Color foreground, Color border)
    {
        Surface = surface;
        Foreground = foreground;
        Border = border;
    }

    public Color Surface { get; }

    public Color Foreground { get; }

    public Color Border { get; }
}

internal static class BootstrapDatePickerRenderLogic
{
    public static BootstrapDatePickerMetrics ResolveMetrics(
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
        return new BootstrapDatePickerMetrics(
            DpiScaler.Scale(metrics.SpacingXS, dpi),
            DpiScaler.Scale((float)metrics.BorderWidth, dpi),
            DpiScaler.Scale((float)metrics.FocusBorderWidth, dpi),
            DpiScaler.Scale((float)logicalRadius, dpi));
    }

    public static BootstrapDatePickerPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapValidationState validationState,
        bool containsFocus,
        bool enabled)
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

        return new BootstrapDatePickerPalette(
            enabled ? colors.Surface : colors.SurfaceSecondary,
            enabled ? colors.Text : colors.MutedText,
            border);
    }

    public static Rectangle CalculateNativeBounds(
        Size clientSize,
        int nativePreferredHeight,
        BootstrapDatePickerMetrics metrics)
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

        var padding = Math.Max(0, metrics.ShellPadding);
        var horizontalPadding = Math.Min(padding, clientSize.Width / 2);
        var verticalPadding = Math.Min(padding, clientSize.Height / 2);
        var width = Math.Max(0, clientSize.Width - (horizontalPadding * 2));
        var availableHeight = Math.Max(0, clientSize.Height - (verticalPadding * 2));
        var height = Math.Min(nativePreferredHeight, availableHeight);
        var top = Math.Max(0, (clientSize.Height - height) / 2);

        return new Rectangle(horizontalPadding, top, width, height);
    }
}
