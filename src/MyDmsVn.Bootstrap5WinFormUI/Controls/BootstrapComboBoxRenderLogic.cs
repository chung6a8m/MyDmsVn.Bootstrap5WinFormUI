using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapComboBoxMetrics
{
    public BootstrapComboBoxMetrics(
        int horizontalPadding,
        int verticalPadding,
        int iconSize,
        int iconGap,
        int itemHeight,
        float borderWidth,
        float focusBorderWidth,
        float radius)
    {
        HorizontalPadding = horizontalPadding;
        VerticalPadding = verticalPadding;
        IconSize = iconSize;
        IconGap = iconGap;
        ItemHeight = itemHeight;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int HorizontalPadding { get; }

    public int VerticalPadding { get; }

    public int IconSize { get; }

    public int IconGap { get; }

    public int ItemHeight { get; }

    public float BorderWidth { get; }

    public float FocusBorderWidth { get; }

    public float Radius { get; }
}

internal readonly struct BootstrapComboBoxPalette
{
    public BootstrapComboBoxPalette(
        Color background,
        Color foreground,
        Color border,
        Color selectedBackground,
        Color selectedForeground)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
        SelectedBackground = selectedBackground;
        SelectedForeground = selectedForeground;
    }

    public Color Background { get; }

    public Color Foreground { get; }

    public Color Border { get; }

    public Color SelectedBackground { get; }

    public Color SelectedForeground { get; }
}

internal readonly struct BootstrapComboBoxItemLayout
{
    public BootstrapComboBoxItemLayout(Rectangle iconBounds, Rectangle textBounds)
    {
        IconBounds = iconBounds;
        TextBounds = textBounds;
    }

    public Rectangle IconBounds { get; }

    public Rectangle TextBounds { get; }
}

internal static class BootstrapComboBoxRenderLogic
{
    public static BootstrapComboBoxMetrics ResolveMetrics(
        BootstrapThemeMetrics metrics,
        int fontHeight,
        int dpi,
        int borderRadius)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (fontHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontHeight), fontHeight, "Font height must be greater than zero.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        if (borderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(borderRadius), borderRadius, "Border radius must be -1 or a non-negative value.");
        }

        var horizontalPadding = DpiScaler.Scale(metrics.SpacingSM, dpi);
        var verticalPadding = DpiScaler.Scale(metrics.SpacingXS, dpi);
        var iconSize = DpiScaler.Scale(metrics.SpacingLG, dpi);
        var iconGap = DpiScaler.Scale(metrics.SpacingXS, dpi);
        var controlHeight = DpiScaler.Scale(metrics.ControlHeight, dpi);
        var itemHeight = Math.Max(controlHeight, fontHeight + (verticalPadding * 2));
        var logicalRadius = borderRadius >= 0 ? borderRadius : metrics.Radius;

        return new BootstrapComboBoxMetrics(
            horizontalPadding,
            verticalPadding,
            iconSize,
            iconGap,
            itemHeight,
            DpiScaler.Scale((float)metrics.BorderWidth, dpi),
            DpiScaler.Scale((float)metrics.FocusBorderWidth, dpi),
            DpiScaler.Scale((float)logicalRadius, dpi));
    }

    public static BootstrapComboBoxPalette ResolvePalette(
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
        var background = enabled ? colors.Surface : colors.SurfaceSecondary;
        var foreground = enabled ? colors.Text : colors.MutedText;
        var selectedBackground = enabled ? colors.Primary : colors.SurfaceSecondary;
        var selectedForeground = enabled
            ? ColorUtil.GetContrastingTextColor(selectedBackground, colors.Light, colors.Dark)
            : colors.MutedText;

        return new BootstrapComboBoxPalette(
            background,
            foreground,
            border,
            selectedBackground,
            selectedForeground);
    }

    public static BootstrapComboBoxItemLayout CalculateItemLayout(
        Rectangle bounds,
        BootstrapComboBoxMetrics metrics,
        bool showLeadingIcon,
        int trailingReserve)
    {
        if (trailingReserve < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trailingReserve), trailingReserve, "Trailing reserve cannot be negative.");
        }

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new BootstrapComboBoxItemLayout(Rectangle.Empty, Rectangle.Empty);
        }

        var horizontalPadding = Math.Min(metrics.HorizontalPadding, bounds.Width);
        var left = bounds.Left + horizontalPadding;
        var rightInset = SaturatingAdd(metrics.HorizontalPadding, trailingReserve);
        var right = bounds.Right - Math.Min(rightInset, bounds.Width);
        if (right < left)
        {
            right = left;
        }

        if (!showLeadingIcon)
        {
            return new BootstrapComboBoxItemLayout(
                Rectangle.Empty,
                new Rectangle(left, bounds.Top, Math.Max(0, right - left), bounds.Height));
        }

        var availableWidth = Math.Max(0, right - left);
        var iconExtent = Math.Min(metrics.IconSize, Math.Min(bounds.Height, availableWidth));
        if (iconExtent <= 0)
        {
            return new BootstrapComboBoxItemLayout(
                Rectangle.Empty,
                new Rectangle(left, bounds.Top, availableWidth, bounds.Height));
        }

        var iconTop = bounds.Top + Math.Max(0, (bounds.Height - iconExtent) / 2);
        var iconBounds = new Rectangle(left, iconTop, iconExtent, iconExtent);
        var remainingAfterIcon = Math.Max(0, right - iconBounds.Right);
        var gap = Math.Min(metrics.IconGap, remainingAfterIcon);
        var textLeft = iconBounds.Right + gap;
        var textBounds = new Rectangle(
            textLeft,
            bounds.Top,
            Math.Max(0, right - textLeft),
            bounds.Height);

        return new BootstrapComboBoxItemLayout(iconBounds, textBounds);
    }

    private static int SaturatingAdd(int left, int right)
    {
        if (left > int.MaxValue - right)
        {
            return int.MaxValue;
        }

        return left + right;
    }
}
