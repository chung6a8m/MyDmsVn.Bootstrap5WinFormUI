using System;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal readonly struct BootstrapSelectResultLayout
{
    private BootstrapSelectResultLayout(int rowCount, int rowHeight, int viewportHeight, int scrollOffset, int totalHeight, int firstVisibleIndex, int lastVisibleIndex)
    {
        RowCount = rowCount;
        RowHeight = rowHeight;
        ViewportHeight = viewportHeight;
        ScrollOffset = scrollOffset;
        TotalHeight = totalHeight;
        FirstVisibleIndex = firstVisibleIndex;
        LastVisibleIndex = lastVisibleIndex;
    }

    internal int RowCount { get; }
    internal int RowHeight { get; }
    internal int ViewportHeight { get; }
    internal int ScrollOffset { get; }
    internal int TotalHeight { get; }
    internal int FirstVisibleIndex { get; }
    internal int LastVisibleIndex { get; }
    internal int VisibleCount => FirstVisibleIndex < 0 || LastVisibleIndex < FirstVisibleIndex ? 0 : LastVisibleIndex - FirstVisibleIndex + 1;

    internal static BootstrapSelectResultLayout Create(int rowCount, int rowHeight, int viewportHeight, int scrollOffset)
    {
        if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
        if (rowHeight <= 0) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        if (viewportHeight < 0) throw new ArgumentOutOfRangeException(nameof(viewportHeight));
        if (scrollOffset < 0) throw new ArgumentOutOfRangeException(nameof(scrollOffset));

        var totalLong = (long)rowCount * rowHeight;
        var totalHeight = totalLong >= int.MaxValue ? int.MaxValue : (int)totalLong;
        var maxOffset = Math.Max(0, totalHeight - viewportHeight);
        var effectiveOffset = Math.Min(scrollOffset, maxOffset);
        if (rowCount == 0 || viewportHeight == 0)
        {
            return new BootstrapSelectResultLayout(rowCount, rowHeight, viewportHeight, effectiveOffset, totalHeight, -1, -1);
        }

        var first = Math.Min(rowCount - 1, effectiveOffset / rowHeight);
        var lastPixel = Math.Max(0L, (long)effectiveOffset + viewportHeight - 1L);
        var last = (int)Math.Min(rowCount - 1L, lastPixel / rowHeight);
        return new BootstrapSelectResultLayout(rowCount, rowHeight, viewportHeight, effectiveOffset, totalHeight, first, last);
    }

    internal Rectangle GetRowBounds(int index, int width)
    {
        if (index < 0 || index >= RowCount) throw new ArgumentOutOfRangeException(nameof(index));
        var yLong = ((long)index * RowHeight) - ScrollOffset;
        var y = yLong < int.MinValue ? int.MinValue : yLong > int.MaxValue ? int.MaxValue : (int)yLong;
        return new Rectangle(0, y, Math.Max(0, width), RowHeight);
    }

    internal int HitTestIndex(int y)
    {
        if (y < 0 || y >= ViewportHeight || RowCount == 0) return -1;
        var absolute = (long)ScrollOffset + y;
        var index = absolute / RowHeight;
        return index >= 0 && index < RowCount ? (int)index : -1;
    }
}
