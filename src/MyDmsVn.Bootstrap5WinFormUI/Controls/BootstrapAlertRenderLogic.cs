using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapAlertPalette
{
    public BootstrapAlertPalette(Color surface, Color border, Color foreground, Color focus)
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

internal readonly struct BootstrapAlertMetrics
{
    public BootstrapAlertMetrics(
        int horizontalPadding,
        int verticalPadding,
        int contentSpacing,
        int iconSize,
        int closeButtonSize,
        int borderWidth,
        int focusBorderWidth,
        int radius)
    {
        HorizontalPadding = horizontalPadding;
        VerticalPadding = verticalPadding;
        ContentSpacing = contentSpacing;
        IconSize = iconSize;
        CloseButtonSize = closeButtonSize;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int HorizontalPadding { get; }

    public int VerticalPadding { get; }

    public int ContentSpacing { get; }

    public int IconSize { get; }

    public int CloseButtonSize { get; }

    public int BorderWidth { get; }

    public int FocusBorderWidth { get; }

    public int Radius { get; }
}

internal readonly struct BootstrapAlertLayout
{
    public BootstrapAlertLayout(
        Rectangle surfaceBounds,
        Rectangle contentBounds,
        Rectangle iconBounds,
        Rectangle textBounds,
        Rectangle closeBounds,
        CornerRadius cornerRadius)
    {
        SurfaceBounds = surfaceBounds;
        ContentBounds = contentBounds;
        IconBounds = iconBounds;
        TextBounds = textBounds;
        CloseBounds = closeBounds;
        CornerRadius = cornerRadius;
    }

    public Rectangle SurfaceBounds { get; }

    public Rectangle ContentBounds { get; }

    public Rectangle IconBounds { get; }

    public Rectangle TextBounds { get; }

    public Rectangle CloseBounds { get; }

    public CornerRadius CornerRadius { get; }
}

internal static class BootstrapAlertRenderLogic
{
    private const float SurfaceSemanticAmount = 0.12f;
    private const float BorderSemanticAmount = 0.45f;
    private const float ForegroundSemanticAmount = 0.72f;
    private const double MinimumTextContrast = 4.5d;

    public static void ValidateVariant(BootstrapVariant variant)
    {
        if (variant < BootstrapVariant.Primary || variant > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported Bootstrap variant.");
        }
    }

    public static BootstrapAlertPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool enabled)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        ValidateVariant(variant);

        if (!enabled)
        {
            return new BootstrapAlertPalette(
                colors.SurfaceSecondary,
                colors.Border,
                colors.MutedText,
                colors.Disabled);
        }

        var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
        var surface = ColorUtil.Blend(semantic, colors.Surface, SurfaceSemanticAmount);
        var border = ColorUtil.Blend(semantic, colors.Border, BorderSemanticAmount);
        var foregroundCandidate = ColorUtil.Blend(semantic, colors.Text, ForegroundSemanticAmount);
        var foreground = ColorUtil.GetContrastRatio(foregroundCandidate, surface) >= MinimumTextContrast
            ? foregroundCandidate
            : colors.Text;

        return new BootstrapAlertPalette(surface, border, foreground, colors.Focus);
    }

    public static BootstrapAlertMetrics ResolveMetrics(
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
        return new BootstrapAlertMetrics(
            DpiScaler.Scale(metrics.SpacingMD, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingLG, dpi),
            DpiScaler.Scale(metrics.ControlHeightSmall, dpi),
            DpiScaler.Scale(metrics.BorderWidth, dpi),
            DpiScaler.Scale(metrics.FocusBorderWidth, dpi),
            DpiScaler.Scale(logicalRadius, dpi));
    }

    public static BootstrapAlertLayout CalculateLayout(
        Rectangle clientBounds,
        BootstrapAlertMetrics metrics,
        bool hasIcon,
        bool dismissible)
    {
        var surfaceBounds = new Rectangle(
            clientBounds.X,
            clientBounds.Y,
            Math.Max(0, clientBounds.Width),
            Math.Max(0, clientBounds.Height));

        var contentBounds = InsetClamped(
            surfaceBounds,
            metrics.HorizontalPadding,
            metrics.VerticalPadding);

        if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
        {
            return new BootstrapAlertLayout(
                surfaceBounds,
                contentBounds,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                new CornerRadius(metrics.Radius));
        }

        var closeBounds = Rectangle.Empty;
        var textRight = contentBounds.Right;
        if (dismissible)
        {
            var closeSide = Math.Min(metrics.CloseButtonSize, Math.Min(contentBounds.Height, contentBounds.Width));
            if (closeSide > 0)
            {
                closeBounds = new Rectangle(
                    contentBounds.Right - closeSide,
                    contentBounds.Top + ((contentBounds.Height - closeSide) / 2),
                    closeSide,
                    closeSide);
                textRight = Math.Max(contentBounds.Left, closeBounds.Left - metrics.ContentSpacing);
            }
        }

        var iconBounds = Rectangle.Empty;
        var textLeft = contentBounds.Left;
        if (hasIcon)
        {
            var iconRightLimit = closeBounds.IsEmpty ? contentBounds.Right : closeBounds.Left;
            var availableWidth = Math.Max(0, iconRightLimit - contentBounds.Left);
            var iconSide = Math.Min(metrics.IconSize, Math.Min(contentBounds.Height, availableWidth));
            if (iconSide > 0)
            {
                iconBounds = new Rectangle(
                    contentBounds.Left,
                    contentBounds.Top + ((contentBounds.Height - iconSide) / 2),
                    iconSide,
                    iconSide);
                textLeft = iconBounds.Right;
                if (textLeft < textRight)
                {
                    textLeft += Math.Min(metrics.ContentSpacing, textRight - textLeft);
                }
            }
        }

        if (textRight < textLeft)
        {
            textRight = textLeft;
        }

        var textBounds = new Rectangle(
            textLeft,
            contentBounds.Top,
            Math.Max(0, textRight - textLeft),
            contentBounds.Height);

        return new BootstrapAlertLayout(
            surfaceBounds,
            contentBounds,
            iconBounds,
            textBounds,
            closeBounds,
            new CornerRadius(metrics.Radius));
    }

    private static Rectangle InsetClamped(Rectangle bounds, int horizontal, int vertical)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new Rectangle(bounds.X, bounds.Y, 0, 0);
        }

        var left = Math.Min(bounds.Right, bounds.Left + Math.Max(0, horizontal));
        var top = Math.Min(bounds.Bottom, bounds.Top + Math.Max(0, vertical));
        var right = Math.Max(left, bounds.Right - Math.Max(0, horizontal));
        var bottom = Math.Max(top, bounds.Bottom - Math.Max(0, vertical));

        return new Rectangle(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }
}
