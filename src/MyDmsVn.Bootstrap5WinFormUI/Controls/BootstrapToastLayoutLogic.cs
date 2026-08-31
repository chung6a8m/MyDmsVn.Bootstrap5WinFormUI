using System;
using System.Collections.Generic;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapToastMetrics
{
    public BootstrapToastMetrics(
        int horizontalPadding,
        int verticalPadding,
        int contentSpacing,
        int titleBodySpacing,
        int iconSize,
        int closeButtonSize,
        int borderWidth,
        int radius,
        int slideDistance)
    {
        HorizontalPadding = horizontalPadding;
        VerticalPadding = verticalPadding;
        ContentSpacing = contentSpacing;
        TitleBodySpacing = titleBodySpacing;
        IconSize = iconSize;
        CloseButtonSize = closeButtonSize;
        BorderWidth = borderWidth;
        Radius = radius;
        SlideDistance = slideDistance;
    }

    public int HorizontalPadding { get; }
    public int VerticalPadding { get; }
    public int ContentSpacing { get; }
    public int TitleBodySpacing { get; }
    public int IconSize { get; }
    public int CloseButtonSize { get; }
    public int BorderWidth { get; }
    public int Radius { get; }
    public int SlideDistance { get; }
}

internal readonly struct BootstrapToastContentLayout
{
    public BootstrapToastContentLayout(
        Rectangle surfaceBounds,
        Rectangle contentBounds,
        Rectangle iconBounds,
        Rectangle titleBounds,
        Rectangle bodyBounds,
        Rectangle closeBounds,
        CornerRadius cornerRadius)
    {
        SurfaceBounds = surfaceBounds;
        ContentBounds = contentBounds;
        IconBounds = iconBounds;
        TitleBounds = titleBounds;
        BodyBounds = bodyBounds;
        CloseBounds = closeBounds;
        CornerRadius = cornerRadius;
    }

    public Rectangle SurfaceBounds { get; }
    public Rectangle ContentBounds { get; }
    public Rectangle IconBounds { get; }
    public Rectangle TitleBounds { get; }
    public Rectangle BodyBounds { get; }
    public Rectangle CloseBounds { get; }
    public CornerRadius CornerRadius { get; }
}

internal static class BootstrapToastLayoutLogic
{
    public static int CalculateRequiredStackHeight(
        IReadOnlyList<Size> toastSizes,
        int logicalSpacing,
        int dpi)
    {
        if (toastSizes is null)
        {
            throw new ArgumentNullException(nameof(toastSizes));
        }

        if (logicalSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalSpacing), logicalSpacing, "Toast spacing cannot be negative.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        long height = 0;
        var spacing = DpiScaler.Scale(logicalSpacing, dpi);
        for (var index = 0; index < toastSizes.Count; index++)
        {
            var size = toastSizes[index];
            if (size.Width < 0 || size.Height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toastSizes), "Toast sizes cannot contain negative dimensions.");
            }

            if (index > 0)
            {
                height += spacing;
            }

            height += size.Height;
            if (height >= int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        return (int)height;
    }

    public static IReadOnlyList<Rectangle> CalculateStackBounds(
        Rectangle containerBounds,
        IReadOnlyList<Size> toastSizes,
        BootstrapToastPlacement placement,
        int logicalSpacing,
        int maximumVisibleToasts,
        int dpi)
    {
        if (toastSizes is null)
        {
            throw new ArgumentNullException(nameof(toastSizes));
        }

        ValidatePlacement(placement);
        if (logicalSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalSpacing), logicalSpacing, "Toast spacing cannot be negative.");
        }

        if (maximumVisibleToasts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisibleToasts), maximumVisibleToasts, "Maximum visible toasts must be greater than zero.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        for (var index = 0; index < toastSizes.Count; index++)
        {
            if (toastSizes[index].Width < 0 || toastSizes[index].Height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toastSizes), "Toast sizes cannot contain negative dimensions.");
            }
        }

        var count = Math.Min(toastSizes.Count, maximumVisibleToasts);
        if (count == 0)
        {
            return Array.Empty<Rectangle>();
        }

        var spacing = DpiScaler.Scale(logicalSpacing, dpi);
        var result = new Rectangle[count];
        var rightAligned = placement == BootstrapToastPlacement.TopRight || placement == BootstrapToastPlacement.BottomRight;
        var bottomAligned = placement == BootstrapToastPlacement.BottomLeft || placement == BootstrapToastPlacement.BottomRight;
        var cursor = bottomAligned ? containerBounds.Bottom : containerBounds.Top;

        for (var index = 0; index < count; index++)
        {
            var size = toastSizes[index];
            var x = rightAligned ? containerBounds.Right - size.Width : containerBounds.Left;
            var y = bottomAligned ? cursor - size.Height : cursor;
            result[index] = new Rectangle(x, y, size.Width, size.Height);
            cursor = bottomAligned ? y - spacing : y + size.Height + spacing;
        }

        return result;
    }

    public static BootstrapToastMetrics ResolveMetrics(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        return new BootstrapToastMetrics(
            DpiScaler.Scale(metrics.SpacingMD, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingXS, dpi),
            DpiScaler.Scale(metrics.SpacingLG, dpi),
            DpiScaler.Scale(metrics.ControlHeightSmall, dpi),
            DpiScaler.Scale(metrics.BorderWidth, dpi),
            DpiScaler.Scale(metrics.Radius, dpi),
            DpiScaler.Scale(metrics.SpacingLG, dpi));
    }

    public static int CalculatePreferredHeight(
        BootstrapToastMetrics metrics,
        Size titleSize,
        Size bodySize,
        bool hasTitle,
        bool hasIcon,
        bool dismissible)
    {
        ValidateMeasuredSize(titleSize, nameof(titleSize));
        ValidateMeasuredSize(bodySize, nameof(bodySize));

        var textHeight = bodySize.Height;
        if (hasTitle)
        {
            textHeight = titleSize.Height;
            if (bodySize.Height > 0)
            {
                textHeight += metrics.TitleBodySpacing + bodySize.Height;
            }
        }

        var contentHeight = textHeight;
        if (hasIcon)
        {
            contentHeight = Math.Max(contentHeight, metrics.IconSize);
        }

        if (dismissible)
        {
            contentHeight = Math.Max(contentHeight, metrics.CloseButtonSize);
        }

        return Math.Max(0, contentHeight) + (metrics.VerticalPadding * 2);
    }

    public static BootstrapToastContentLayout CalculateContentLayout(
        Rectangle clientBounds,
        BootstrapToastMetrics metrics,
        bool hasTitle,
        bool hasIcon,
        bool dismissible,
        Size titleSize,
        Size bodySize)
    {
        ValidateMeasuredSize(titleSize, nameof(titleSize));
        ValidateMeasuredSize(bodySize, nameof(bodySize));

        var surfaceBounds = new Rectangle(
            clientBounds.X,
            clientBounds.Y,
            Math.Max(0, clientBounds.Width),
            Math.Max(0, clientBounds.Height));
        var contentBounds = InsetClamped(surfaceBounds, metrics.HorizontalPadding, metrics.VerticalPadding);
        if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
        {
            return new BootstrapToastContentLayout(
                surfaceBounds,
                contentBounds,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                new CornerRadius(metrics.Radius));
        }

        var closeBounds = Rectangle.Empty;
        var textRight = contentBounds.Right;
        if (dismissible)
        {
            var closeSide = Math.Min(metrics.CloseButtonSize, Math.Min(contentBounds.Width, contentBounds.Height));
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
            var availableWidth = Math.Max(0, textRight - contentBounds.Left);
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

        var textWidth = Math.Max(0, textRight - textLeft);
        var titleBounds = Rectangle.Empty;
        var bodyBounds = Rectangle.Empty;
        var textTop = contentBounds.Top;
        var textBottom = contentBounds.Bottom;

        if (hasTitle && textWidth > 0)
        {
            var titleHeight = Math.Min(titleSize.Height, Math.Max(0, textBottom - textTop));
            titleBounds = new Rectangle(textLeft, textTop, textWidth, titleHeight);
            textTop = titleBounds.Bottom;
            if (bodySize.Height > 0 && textTop < textBottom)
            {
                textTop += Math.Min(metrics.TitleBodySpacing, textBottom - textTop);
            }
        }

        if (bodySize.Height > 0 && textWidth > 0 && textTop < textBottom)
        {
            var bodyHeight = Math.Min(bodySize.Height, textBottom - textTop);
            bodyBounds = new Rectangle(textLeft, textTop, textWidth, Math.Max(0, bodyHeight));
        }

        return new BootstrapToastContentLayout(
            surfaceBounds,
            contentBounds,
            iconBounds,
            titleBounds,
            bodyBounds,
            closeBounds,
            new CornerRadius(metrics.Radius));
    }

    public static void ValidatePlacement(BootstrapToastPlacement placement)
    {
        if (placement < BootstrapToastPlacement.TopLeft || placement > BootstrapToastPlacement.BottomRight)
        {
            throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unsupported toast placement.");
        }
    }

    private static void ValidateMeasuredSize(Size size, string parameterName)
    {
        if (size.Width < 0 || size.Height < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, size, "Measured sizes cannot contain negative dimensions.");
        }
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
        return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}
