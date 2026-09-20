using System;
using System.Collections.Generic;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapBreadcrumbLayoutMetrics
{
    public BootstrapBreadcrumbLayoutMetrics(int dividerGap, int rowGap)
    {
        if (dividerGap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dividerGap));
        }

        if (rowGap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowGap));
        }

        DividerGap = dividerGap;
        RowGap = rowGap;
    }

    public int DividerGap { get; }

    public int RowGap { get; }
}

internal readonly struct BootstrapBreadcrumbSegmentSize
{
    public BootstrapBreadcrumbSegmentSize(
        Size itemSize,
        Size dividerSize,
        bool hasDivider)
    {
        ValidateSize(itemSize, nameof(itemSize));
        ValidateSize(dividerSize, nameof(dividerSize));

        ItemSize = itemSize;
        DividerSize = dividerSize;
        HasDivider = hasDivider;
    }

    public Size ItemSize { get; }

    public Size DividerSize { get; }

    public bool HasDivider { get; }

    private static void ValidateSize(Size size, string parameterName)
    {
        if (size.Width < 0 || size.Height < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal readonly struct BootstrapBreadcrumbSegmentLayout
{
    public BootstrapBreadcrumbSegmentLayout(
        Rectangle itemBounds,
        Rectangle dividerBounds)
    {
        ItemBounds = itemBounds;
        DividerBounds = dividerBounds;
    }

    public Rectangle ItemBounds { get; }

    public Rectangle DividerBounds { get; }
}

internal static class BootstrapBreadcrumbLayoutLogic
{
    internal static Size Measure(
        IReadOnlyList<BootstrapBreadcrumbSegmentSize> segments,
        int maximumWidth,
        BootstrapBreadcrumbLayoutMetrics metrics,
        bool wrapContents)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        if (segments.Count == 0)
        {
            return Size.Empty;
        }

        var rows = PackRows(segments, maximumWidth, metrics, wrapContents);
        var width = 0;
        var height = 0;
        for (var index = 0; index < rows.Count; index++)
        {
            width = Math.Max(width, rows[index].Width);
            height = AddSaturating(height, rows[index].Height);
            if (index > 0)
            {
                height = AddSaturating(height, metrics.RowGap);
            }
        }

        return new Size(width, height);
    }

    internal static IReadOnlyList<BootstrapBreadcrumbSegmentLayout> Arrange(
        IReadOnlyList<BootstrapBreadcrumbSegmentSize> segments,
        Rectangle contentBounds,
        BootstrapBreadcrumbLayoutMetrics metrics,
        bool wrapContents,
        bool rightToLeft)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        if (segments.Count == 0)
        {
            return Array.Empty<BootstrapBreadcrumbSegmentLayout>();
        }

        var normalizedBounds = new Rectangle(
            contentBounds.X,
            contentBounds.Y,
            Math.Max(0, contentBounds.Width),
            Math.Max(0, contentBounds.Height));
        var rows = PackRows(segments, normalizedBounds.Width, metrics, wrapContents);
        var layouts = new BootstrapBreadcrumbSegmentLayout[segments.Count];
        var rowTop = normalizedBounds.Top;

        foreach (var row in rows)
        {
            var cursor = normalizedBounds.Left;
            for (var index = row.StartIndex; index < row.StartIndex + row.Count; index++)
            {
                var segment = segments[index];
                var itemBounds = new Rectangle(
                    cursor,
                    rowTop + ((row.Height - segment.ItemSize.Height) / 2),
                    segment.ItemSize.Width,
                    segment.ItemSize.Height);
                var dividerBounds = Rectangle.Empty;

                if (segment.HasDivider)
                {
                    dividerBounds = new Rectangle(
                        cursor + metrics.DividerGap,
                        rowTop + ((row.Height - segment.DividerSize.Height) / 2),
                        segment.DividerSize.Width,
                        segment.DividerSize.Height);
                    itemBounds.X = AddSaturating(
                        dividerBounds.Right,
                        metrics.DividerGap);
                }

                if (rightToLeft)
                {
                    itemBounds = Mirror(itemBounds, normalizedBounds);
                    if (segment.HasDivider)
                    {
                        dividerBounds = Mirror(dividerBounds, normalizedBounds);
                    }
                }

                layouts[index] = new BootstrapBreadcrumbSegmentLayout(itemBounds, dividerBounds);
                cursor = AddSaturating(cursor, GetSegmentWidth(segment, metrics));
            }

            rowTop = AddSaturating(rowTop, row.Height);
            rowTop = AddSaturating(rowTop, metrics.RowGap);
        }

        return layouts;
    }

    private static List<Row> PackRows(
        IReadOnlyList<BootstrapBreadcrumbSegmentSize> segments,
        int maximumWidth,
        BootstrapBreadcrumbLayoutMetrics metrics,
        bool wrapContents)
    {
        var rows = new List<Row>();
        var bounded = wrapContents && maximumWidth > 0;
        var row = new Row(0);

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            var segmentWidth = GetSegmentWidth(segment, metrics);
            var segmentHeight = Math.Max(segment.ItemSize.Height, segment.DividerSize.Height);
            var candidateWidth = AddSaturating(row.Width, segmentWidth);

            if (bounded && row.Count > 0 && candidateWidth > maximumWidth)
            {
                rows.Add(row);
                row = new Row(index);
                candidateWidth = segmentWidth;
            }

            row.Width = candidateWidth;
            row.Height = Math.Max(row.Height, segmentHeight);
            row.Count++;
        }

        if (row.Count > 0)
        {
            rows.Add(row);
        }

        return rows;
    }

    private static int GetSegmentWidth(
        BootstrapBreadcrumbSegmentSize segment,
        BootstrapBreadcrumbLayoutMetrics metrics)
    {
        if (!segment.HasDivider)
        {
            return segment.ItemSize.Width;
        }

        var width = AddSaturating(segment.ItemSize.Width, segment.DividerSize.Width);
        width = AddSaturating(width, metrics.DividerGap);
        return AddSaturating(width, metrics.DividerGap);
    }

    private static Rectangle Mirror(Rectangle rectangle, Rectangle bounds)
    {
        var relativeLeft = rectangle.Left - bounds.Left;
        return new Rectangle(
            bounds.Left + bounds.Width - relativeLeft - rectangle.Width,
            rectangle.Top,
            rectangle.Width,
            rectangle.Height);
    }

    private static int AddSaturating(int left, int right)
    {
        var result = (long)left + right;
        if (result > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (result < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)result;
    }

    private sealed class Row
    {
        internal Row(int startIndex)
        {
            StartIndex = startIndex;
        }

        internal int StartIndex { get; }

        internal int Count { get; set; }

        internal int Width { get; set; }

        internal int Height { get; set; }
    }
}
