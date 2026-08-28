using System;
using System.Collections.Generic;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapTabHeaderMetrics
{
    public BootstrapTabHeaderMetrics(
        int height,
        int horizontalPadding,
        int contentSpacing,
        int minimumWidth,
        int borderWidth,
        int focusBorderWidth,
        int underlineHeight,
        int radius)
    {
        Height = height;
        HorizontalPadding = horizontalPadding;
        ContentSpacing = contentSpacing;
        MinimumWidth = minimumWidth;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        UnderlineHeight = underlineHeight;
        Radius = radius;
    }

    public int Height { get; }

    public int HorizontalPadding { get; }

    public int ContentSpacing { get; }

    public int MinimumWidth { get; }

    public int BorderWidth { get; }

    public int FocusBorderWidth { get; }

    public int UnderlineHeight { get; }

    public int Radius { get; }
}

internal readonly struct BootstrapTabHeaderPalette
{
    public BootstrapTabHeaderPalette(Color background, Color border, Color foreground, Color accent, Color focus)
    {
        Background = background;
        Border = border;
        Foreground = foreground;
        Accent = accent;
        Focus = focus;
    }

    public Color Background { get; }

    public Color Border { get; }

    public Color Foreground { get; }

    public Color Accent { get; }

    public Color Focus { get; }
}

internal readonly struct BootstrapTabHeaderLayout
{
    public BootstrapTabHeaderLayout(
        Rectangle surfaceBounds,
        Rectangle contentBounds,
        Rectangle imageBounds,
        Rectangle textBounds,
        Rectangle underlineBounds,
        Rectangle focusBounds,
        CornerRadius cornerRadius)
    {
        SurfaceBounds = surfaceBounds;
        ContentBounds = contentBounds;
        ImageBounds = imageBounds;
        TextBounds = textBounds;
        UnderlineBounds = underlineBounds;
        FocusBounds = focusBounds;
        CornerRadius = cornerRadius;
    }

    public Rectangle SurfaceBounds { get; }

    public Rectangle ContentBounds { get; }

    public Rectangle ImageBounds { get; }

    public Rectangle TextBounds { get; }

    public Rectangle UnderlineBounds { get; }

    public Rectangle FocusBounds { get; }

    public CornerRadius CornerRadius { get; }
}

internal static class BootstrapTabControlRenderLogic
{
    public static void ValidateStyle(BootstrapTabStyle style)
    {
        if (style < BootstrapTabStyle.Tabs || style > BootstrapTabStyle.Underline)
        {
            throw new ArgumentOutOfRangeException(nameof(style), style, "Unsupported Bootstrap tab style.");
        }
    }

    public static void ValidateVariant(BootstrapVariant variant)
    {
        if (variant < BootstrapVariant.Primary || variant > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported Bootstrap variant.");
        }
    }

    public static BootstrapTabHeaderMetrics ResolveMetrics(
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
        return new BootstrapTabHeaderMetrics(
            DpiScaler.Scale(metrics.ControlHeight, dpi),
            DpiScaler.Scale(metrics.SpacingMD, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.ControlHeightLarge + metrics.SpacingLG, dpi),
            DpiScaler.Scale(metrics.BorderWidth, dpi),
            DpiScaler.Scale(metrics.FocusBorderWidth, dpi),
            DpiScaler.Scale(Math.Max(metrics.FocusBorderWidth, metrics.BorderWidth), dpi),
            DpiScaler.Scale(logicalRadius, dpi));
    }

    public static int CalculateUniformItemWidth(
        int tabCount,
        int availableWidth,
        IReadOnlyList<int> preferredContentWidths,
        BootstrapTabHeaderMetrics metrics,
        bool fill)
    {
        if (tabCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tabCount), tabCount, "Tab count cannot be negative.");
        }

        if (preferredContentWidths is null)
        {
            throw new ArgumentNullException(nameof(preferredContentWidths));
        }

        if (preferredContentWidths.Count != tabCount)
        {
            throw new ArgumentException("Preferred-content width count must match tab count.", nameof(preferredContentWidths));
        }

        if (tabCount == 0)
        {
            return metrics.MinimumWidth;
        }

        if (fill)
        {
            return Math.Max(metrics.MinimumWidth, Math.Max(0, availableWidth) / tabCount);
        }

        var widest = 0;
        for (var index = 0; index < preferredContentWidths.Count; index++)
        {
            widest = Math.Max(widest, Math.Max(0, preferredContentWidths[index]));
        }

        return Math.Max(metrics.MinimumWidth, widest + (metrics.HorizontalPadding * 2));
    }

    public static BootstrapTabHeaderPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        BootstrapTabStyle style,
        bool selected,
        bool enabled,
        bool hovered)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        ValidateVariant(variant);
        ValidateStyle(style);

        if (!enabled)
        {
            return new BootstrapTabHeaderPalette(
                colors.Surface,
                colors.Border,
                colors.Disabled,
                colors.Disabled,
                colors.Focus);
        }

        var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
        if (!selected)
        {
            return new BootstrapTabHeaderPalette(
                hovered ? colors.Hover : colors.Surface,
                colors.Border,
                hovered ? colors.Text : colors.MutedText,
                Color.Transparent,
                colors.Focus);
        }

        switch (style)
        {
            case BootstrapTabStyle.Tabs:
                return new BootstrapTabHeaderPalette(
                    colors.Surface,
                    semantic,
                    semantic,
                    semantic,
                    colors.Focus);
            case BootstrapTabStyle.Pills:
                return new BootstrapTabHeaderPalette(
                    semantic,
                    semantic,
                    ColorUtil.GetContrastingTextColor(semantic, colors.Light, colors.Dark),
                    semantic,
                    colors.Focus);
            case BootstrapTabStyle.Underline:
                return new BootstrapTabHeaderPalette(
                    colors.Surface,
                    colors.Surface,
                    semantic,
                    semantic,
                    colors.Focus);
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, "Unsupported Bootstrap tab style.");
        }
    }

    public static BootstrapTabHeaderLayout CalculateLayout(
        Rectangle headerBounds,
        BootstrapTabStyle style,
        BootstrapTabHeaderMetrics metrics,
        int preferredTextWidth,
        Size imageSize,
        bool hasImage)
    {
        ValidateStyle(style);

        var surface = new Rectangle(
            headerBounds.X,
            headerBounds.Y,
            Math.Max(0, headerBounds.Width),
            Math.Max(0, headerBounds.Height));

        var radius = ResolveCornerRadius(style, metrics.Radius);
        if (surface.Width <= 0 || surface.Height <= 0)
        {
            return new BootstrapTabHeaderLayout(
                surface,
                new Rectangle(surface.X, surface.Y, 0, 0),
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                radius);
        }

        var content = InsetHorizontally(surface, metrics.HorizontalPadding);
        var underline = Rectangle.Empty;
        if (style == BootstrapTabStyle.Underline && metrics.UnderlineHeight > 0)
        {
            var underlineHeight = Math.Min(metrics.UnderlineHeight, surface.Height);
            underline = new Rectangle(surface.Left, surface.Bottom - underlineHeight, surface.Width, underlineHeight);
            content.Height = Math.Max(0, Math.Min(content.Height, underline.Top - content.Top));
        }

        var focusInset = Math.Max(1, metrics.FocusBorderWidth);
        var focus = Inset(surface, focusInset);

        var image = Rectangle.Empty;
        var text = content;
        if (content.Width > 0 && content.Height > 0)
        {
            var normalizedTextWidth = Math.Max(0, preferredTextWidth);
            var imageWidth = hasImage ? Math.Min(Math.Max(0, imageSize.Width), content.Width) : 0;
            var imageHeight = hasImage ? Math.Min(Math.Max(0, imageSize.Height), content.Height) : 0;
            var spacing = imageWidth > 0 && normalizedTextWidth > 0
                ? Math.Min(metrics.ContentSpacing, Math.Max(0, content.Width - imageWidth))
                : 0;
            var desiredTextWidth = Math.Min(normalizedTextWidth, Math.Max(0, content.Width - imageWidth - spacing));
            var groupWidth = Math.Min(content.Width, imageWidth + spacing + desiredTextWidth);
            var groupLeft = content.Left + Math.Max(0, (content.Width - groupWidth) / 2);

            if (imageWidth > 0 && imageHeight > 0)
            {
                image = new Rectangle(
                    groupLeft,
                    content.Top + Math.Max(0, (content.Height - imageHeight) / 2),
                    imageWidth,
                    imageHeight);
            }

            var textLeft = image.Width > 0 ? image.Right + spacing : groupLeft;
            var remaining = Math.Max(0, content.Right - textLeft);
            var textWidth = image.Width > 0
                ? Math.Min(desiredTextWidth, remaining)
                : Math.Min(Math.Max(desiredTextWidth, 0), remaining);

            if (!hasImage && normalizedTextWidth == 0)
            {
                textWidth = content.Width;
                textLeft = content.Left;
            }

            text = new Rectangle(textLeft, content.Top, textWidth, content.Height);
        }

        return new BootstrapTabHeaderLayout(surface, content, image, text, underline, focus, radius);
    }

    private static CornerRadius ResolveCornerRadius(BootstrapTabStyle style, int radius)
    {
        switch (style)
        {
            case BootstrapTabStyle.Tabs:
                return new CornerRadius(radius, radius, 0f, 0f);
            case BootstrapTabStyle.Pills:
                return new CornerRadius(radius);
            case BootstrapTabStyle.Underline:
                return CornerRadius.Empty;
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, "Unsupported Bootstrap tab style.");
        }
    }

    private static Rectangle InsetHorizontally(Rectangle bounds, int horizontal)
    {
        var amount = Math.Max(0, horizontal);
        var left = Math.Min(bounds.Right, bounds.Left + amount);
        var right = Math.Max(left, bounds.Right - amount);
        return new Rectangle(left, bounds.Top, Math.Max(0, right - left), bounds.Height);
    }

    private static Rectangle Inset(Rectangle bounds, int amount)
    {
        var inset = Math.Max(0, amount);
        var left = Math.Min(bounds.Right, bounds.Left + inset);
        var top = Math.Min(bounds.Bottom, bounds.Top + inset);
        var right = Math.Max(left, bounds.Right - inset);
        var bottom = Math.Max(top, bounds.Bottom - inset);
        return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}
