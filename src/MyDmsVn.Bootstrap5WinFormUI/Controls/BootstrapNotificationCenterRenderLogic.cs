using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapNotificationCenterMetrics
{
    public BootstrapNotificationCenterMetrics(
        int padding,
        int unreadMarkerSize,
        int contentSpacing,
        int titleBodySpacing,
        int timestampSpacing,
        int minimumRowHeight)
    {
        Padding = padding;
        UnreadMarkerSize = unreadMarkerSize;
        ContentSpacing = contentSpacing;
        TitleBodySpacing = titleBodySpacing;
        TimestampSpacing = timestampSpacing;
        MinimumRowHeight = minimumRowHeight;
    }

    public int Padding { get; }
    public int UnreadMarkerSize { get; }
    public int ContentSpacing { get; }
    public int TitleBodySpacing { get; }
    public int TimestampSpacing { get; }
    public int MinimumRowHeight { get; }
}

internal readonly struct BootstrapNotificationCenterPalette
{
    public BootstrapNotificationCenterPalette(Color surface, Color foreground, Color muted, Color marker, Color border)
    {
        Surface = surface;
        Foreground = foreground;
        Muted = muted;
        Marker = marker;
        Border = border;
    }

    public Color Surface { get; }
    public Color Foreground { get; }
    public Color Muted { get; }
    public Color Marker { get; }
    public Color Border { get; }
}

internal readonly struct BootstrapNotificationCenterRowLayout
{
    public BootstrapNotificationCenterRowLayout(
        Rectangle rowBounds,
        Rectangle unreadMarkerBounds,
        Rectangle titleBounds,
        Rectangle bodyBounds,
        Rectangle timestampBounds)
    {
        RowBounds = rowBounds;
        UnreadMarkerBounds = unreadMarkerBounds;
        TitleBounds = titleBounds;
        BodyBounds = bodyBounds;
        TimestampBounds = timestampBounds;
    }

    public Rectangle RowBounds { get; }
    public Rectangle UnreadMarkerBounds { get; }
    public Rectangle TitleBounds { get; }
    public Rectangle BodyBounds { get; }
    public Rectangle TimestampBounds { get; }
}

internal static class BootstrapNotificationCenterRenderLogic
{
    public static BootstrapNotificationCenterMetrics ResolveMetrics(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        return new BootstrapNotificationCenterMetrics(
            DpiScaler.Scale(metrics.SpacingMD, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingXS, dpi),
            DpiScaler.Scale(metrics.SpacingXS, dpi),
            DpiScaler.Scale(metrics.ControlHeightLarge + metrics.SpacingMD, dpi));
    }

    public static BootstrapNotificationCenterPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool selected,
        bool isRead)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        var marker = BootstrapVariantColorResolver.Resolve(colors, variant);
        return new BootstrapNotificationCenterPalette(
            selected ? colors.Active : colors.Surface,
            isRead ? colors.MutedText : colors.Text,
            colors.MutedText,
            marker,
            colors.Border);
    }

    public static int CalculateRowHeight(
        int availableWidth,
        BootstrapNotificationCenterMetrics metrics,
        Size titleSize,
        Size bodySize,
        Size timestampSize,
        bool hasTitle)
    {
        if (availableWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableWidth), availableWidth, "Available width must be greater than zero.");
        }

        ValidateSize(titleSize, nameof(titleSize));
        ValidateSize(bodySize, nameof(bodySize));
        ValidateSize(timestampSize, nameof(timestampSize));

        var contentHeight = timestampSize.Height;
        if (bodySize.Height > 0)
        {
            contentHeight += metrics.TimestampSpacing + bodySize.Height;
        }

        if (hasTitle && titleSize.Height > 0)
        {
            contentHeight += titleSize.Height;
            if (bodySize.Height > 0)
            {
                contentHeight += metrics.TitleBodySpacing;
            }
        }

        return Math.Max(metrics.MinimumRowHeight, contentHeight + (metrics.Padding * 2));
    }

    public static BootstrapNotificationCenterRowLayout CalculateRowLayout(
        Rectangle rowBounds,
        BootstrapNotificationCenterMetrics metrics,
        Size titleSize,
        Size bodySize,
        Size timestampSize,
        bool hasTitle)
    {
        ValidateSize(titleSize, nameof(titleSize));
        ValidateSize(bodySize, nameof(bodySize));
        ValidateSize(timestampSize, nameof(timestampSize));
        var normalized = new Rectangle(rowBounds.X, rowBounds.Y, Math.Max(0, rowBounds.Width), Math.Max(0, rowBounds.Height));
        if (normalized.Width == 0 || normalized.Height == 0)
        {
            return new BootstrapNotificationCenterRowLayout(normalized, Rectangle.Empty, Rectangle.Empty, Rectangle.Empty, Rectangle.Empty);
        }

        var left = Math.Min(normalized.Right, normalized.Left + metrics.Padding);
        var top = Math.Min(normalized.Bottom, normalized.Top + metrics.Padding);
        var right = Math.Max(left, normalized.Right - metrics.Padding);
        var bottom = Math.Max(top, normalized.Bottom - metrics.Padding);
        var markerSide = Math.Min(metrics.UnreadMarkerSize, Math.Min(Math.Max(0, right - left), Math.Max(0, bottom - top)));
        var marker = markerSide > 0
            ? new Rectangle(left, top, markerSide, markerSide)
            : Rectangle.Empty;
        var textLeft = marker.IsEmpty ? left : Math.Min(right, marker.Right + metrics.ContentSpacing);
        var textWidth = Math.Max(0, right - textLeft);
        var cursor = top;

        var title = Rectangle.Empty;
        if (hasTitle && titleSize.Height > 0 && textWidth > 0)
        {
            var height = Math.Min(titleSize.Height, Math.Max(0, bottom - cursor));
            title = new Rectangle(textLeft, cursor, textWidth, height);
            cursor = title.Bottom;
            if (bodySize.Height > 0)
            {
                cursor = Math.Min(bottom, cursor + metrics.TitleBodySpacing);
            }
        }

        var timestampHeight = Math.Min(timestampSize.Height, Math.Max(0, bottom - cursor));
        var timestampTop = Math.Max(cursor, bottom - timestampHeight);
        var timestamp = timestampHeight > 0 && textWidth > 0
            ? new Rectangle(textLeft, timestampTop, textWidth, timestampHeight)
            : Rectangle.Empty;
        var bodyBottom = timestamp.IsEmpty ? bottom : Math.Max(cursor, timestamp.Top - metrics.TimestampSpacing);
        var bodyHeight = Math.Min(bodySize.Height, Math.Max(0, bodyBottom - cursor));
        var body = bodyHeight > 0 && textWidth > 0
            ? new Rectangle(textLeft, cursor, textWidth, bodyHeight)
            : Rectangle.Empty;

        return new BootstrapNotificationCenterRowLayout(normalized, marker, title, body, timestamp);
    }

    private static void ValidateSize(Size size, string parameterName)
    {
        if (size.Width < 0 || size.Height < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, size, "Measured sizes cannot contain negative dimensions.");
        }
    }
}
