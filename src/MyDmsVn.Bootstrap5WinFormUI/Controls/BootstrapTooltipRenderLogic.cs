using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapTooltipPalette
{
    public BootstrapTooltipPalette(Color background, Color border, Color foreground)
    {
        Background = background;
        Border = border;
        Foreground = foreground;
    }

    public Color Background { get; }

    public Color Border { get; }

    public Color Foreground { get; }
}

internal readonly struct BootstrapTooltipRenderMetrics
{
    public BootstrapTooltipRenderMetrics(Padding padding, int borderWidth, int radius)
    {
        Padding = padding;
        BorderWidth = borderWidth;
        Radius = radius;
    }

    public Padding Padding { get; }

    public int BorderWidth { get; }

    public int Radius { get; }
}

internal static class BootstrapTooltipRenderLogic
{
    public static BootstrapTooltipPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        Color customColor)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        var semanticBackground = BootstrapVariantColorResolver.Resolve(colors, variant);
        var background = customColor.IsEmpty ? semanticBackground : customColor;
        var foreground = ColorUtil.GetContrastingTextColor(background, colors.Light, colors.Dark);
        return new BootstrapTooltipPalette(background, colors.Border, foreground);
    }

    public static BootstrapTooltipRenderMetrics ResolveMetrics(
        BootstrapThemeMetrics metrics,
        Padding logicalPadding,
        int logicalBorderRadius,
        int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        ValidatePadding(logicalPadding);
        if (logicalBorderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(logicalBorderRadius),
                logicalBorderRadius,
                "Border radius must be -1 or a non-negative value.");
        }

        var logicalRadius = logicalBorderRadius >= 0 ? logicalBorderRadius : metrics.Radius;
        return new BootstrapTooltipRenderMetrics(
            DpiScaler.Scale(logicalPadding, dpi),
            DpiScaler.Scale(metrics.BorderWidth, dpi),
            DpiScaler.Scale(logicalRadius, dpi));
    }

    public static Size CalculatePopupSize(Size measuredTextSize, BootstrapTooltipRenderMetrics metrics)
    {
        var textWidth = Math.Max(0, measuredTextSize.Width);
        var textHeight = Math.Max(0, measuredTextSize.Height);
        var horizontalChrome = SaturatingAdd(
            SaturatingAdd(metrics.Padding.Left, metrics.Padding.Right),
            SaturatingMultiplyByTwo(metrics.BorderWidth));
        var verticalChrome = SaturatingAdd(
            SaturatingAdd(metrics.Padding.Top, metrics.Padding.Bottom),
            SaturatingMultiplyByTwo(metrics.BorderWidth));

        return new Size(
            SaturatingAdd(textWidth, horizontalChrome),
            SaturatingAdd(textHeight, verticalChrome));
    }

    public static Rectangle CalculateTextBounds(Rectangle outerBounds, BootstrapTooltipRenderMetrics metrics)
    {
        var width = Math.Max(0, outerBounds.Width);
        var height = Math.Max(0, outerBounds.Height);
        var leftInset = SaturatingAdd(metrics.BorderWidth, metrics.Padding.Left);
        var topInset = SaturatingAdd(metrics.BorderWidth, metrics.Padding.Top);
        var rightInset = SaturatingAdd(metrics.BorderWidth, metrics.Padding.Right);
        var bottomInset = SaturatingAdd(metrics.BorderWidth, metrics.Padding.Bottom);

        var appliedLeft = Math.Min(width, leftInset);
        var appliedTop = Math.Min(height, topInset);
        var remainingWidth = Math.Max(0, width - appliedLeft);
        var remainingHeight = Math.Max(0, height - appliedTop);
        var appliedRight = Math.Min(remainingWidth, rightInset);
        var appliedBottom = Math.Min(remainingHeight, bottomInset);

        return new Rectangle(
            SaturatingAddCoordinate(outerBounds.X, appliedLeft),
            SaturatingAddCoordinate(outerBounds.Y, appliedTop),
            Math.Max(0, remainingWidth - appliedRight),
            Math.Max(0, remainingHeight - appliedBottom));
    }

    private static void ValidatePadding(Padding padding)
    {
        if (padding.Left < 0 || padding.Top < 0 || padding.Right < 0 || padding.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), padding, "Tooltip content padding cannot contain negative edges.");
        }
    }

    private static int SaturatingAdd(int left, int right)
    {
        var value = (long)Math.Max(0, left) + Math.Max(0, right);
        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }

    private static int SaturatingMultiplyByTwo(int value)
    {
        var doubled = (long)Math.Max(0, value) * 2L;
        return doubled >= int.MaxValue ? int.MaxValue : (int)doubled;
    }

    private static int SaturatingAddCoordinate(int coordinate, int offset)
    {
        var value = (long)coordinate + Math.Max(0, offset);
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)value;
    }
}
