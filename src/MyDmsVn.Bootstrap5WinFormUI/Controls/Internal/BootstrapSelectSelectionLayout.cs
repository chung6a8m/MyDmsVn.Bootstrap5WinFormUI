using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal readonly struct BootstrapSelectChipLayout
{
    internal BootstrapSelectChipLayout(BootstrapSelectItem item, Rectangle bounds, Rectangle removeBounds)
    {
        Item = item;
        Bounds = bounds;
        RemoveBounds = removeBounds;
    }

    internal BootstrapSelectItem Item { get; }
    internal Rectangle Bounds { get; }
    internal Rectangle RemoveBounds { get; }
}

internal sealed class BootstrapSelectSelectionLayoutResult
{
    internal BootstrapSelectSelectionLayoutResult(Rectangle contentBounds, Rectangle arrowBounds, Rectangle clearBounds, IList<BootstrapSelectChipLayout> chips, int preferredHeight, int rowCount, bool hasOverflow)
    {
        ContentBounds = contentBounds;
        ArrowBounds = arrowBounds;
        ClearBounds = clearBounds;
        Chips = new ReadOnlyCollection<BootstrapSelectChipLayout>(chips);
        PreferredHeight = preferredHeight;
        RowCount = rowCount;
        HasOverflow = hasOverflow;
    }

    internal Rectangle ContentBounds { get; }
    internal Rectangle ArrowBounds { get; }
    internal Rectangle ClearBounds { get; }
    internal IReadOnlyList<BootstrapSelectChipLayout> Chips { get; }
    internal int PreferredHeight { get; }
    internal int RowCount { get; }
    internal bool HasOverflow { get; }
}

internal static class BootstrapSelectSelectionLayout
{
    internal static BootstrapSelectSelectionLayoutResult Create(Size clientSize, BootstrapSelectMode mode, IReadOnlyList<BootstrapSelectItem> selectedItems, bool allowClear, bool rightToLeft, int dpi, int maximumRows)
    {
        if (selectedItems is null) throw new ArgumentNullException(nameof(selectedItems));
        if (!Enum.IsDefined(typeof(BootstrapSelectMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        if (maximumRows <= 0) throw new ArgumentOutOfRangeException(nameof(maximumRows));

        var padding = DpiScaler.Scale(6, dpi);
        var actionWidth = Math.Max(DpiScaler.Scale(20, dpi), 20);
        var gap = DpiScaler.Scale(4, dpi);
        var arrow = Rectangle.Empty;
        var clear = Rectangle.Empty;
        var usable = new Rectangle(padding, 0, Math.Max(0, clientSize.Width - (padding * 2)), clientSize.Height);
        var showClear = allowClear && selectedItems.Count > 0;

        if (rightToLeft)
        {
            arrow = new Rectangle(usable.Left, 0, Math.Min(actionWidth, usable.Width), clientSize.Height);
            usable.X += arrow.Width;
            usable.Width = Math.Max(0, usable.Width - arrow.Width);
            if (showClear)
            {
                clear = new Rectangle(usable.Left, 0, Math.Min(actionWidth, usable.Width), clientSize.Height);
                usable.X += clear.Width;
                usable.Width = Math.Max(0, usable.Width - clear.Width);
            }
        }
        else
        {
            arrow = new Rectangle(Math.Max(usable.Left, usable.Right - actionWidth), 0, Math.Min(actionWidth, usable.Width), clientSize.Height);
            usable.Width = Math.Max(0, usable.Width - arrow.Width);
            if (showClear)
            {
                clear = new Rectangle(Math.Max(usable.Left, usable.Right - actionWidth), 0, Math.Min(actionWidth, usable.Width), clientSize.Height);
                usable.Width = Math.Max(0, usable.Width - clear.Width);
            }
        }

        var content = usable;
        if (mode == BootstrapSelectMode.Single)
        {
            return new BootstrapSelectSelectionLayoutResult(content, arrow, clear, new List<BootstrapSelectChipLayout>(), DpiScaler.Scale(32, dpi), 1, false);
        }

        var chipHeight = DpiScaler.Scale(24, dpi);
        var verticalPadding = DpiScaler.Scale(4, dpi);
        var rowHeight = chipHeight + gap;
        var chips = new List<BootstrapSelectChipLayout>();
        var x = content.Left;
        var y = verticalPadding;
        var row = 1;
        var overflow = false;
        for (var i = 0; i < selectedItems.Count; i++)
        {
            var item = selectedItems[i];
            var estimatedLogicalWidth = Math.Max(48, 28 + (item.Text.Length * 7));
            var chipWidth = Math.Min(content.Width, DpiScaler.Scale(estimatedLogicalWidth, dpi));
            if (x > content.Left && x + chipWidth > content.Right)
            {
                row++;
                if (row > maximumRows)
                {
                    overflow = true;
                    break;
                }
                x = content.Left;
                y += rowHeight;
            }

            if (content.Width <= 0)
            {
                overflow = selectedItems.Count > 0;
                break;
            }

            chipWidth = Math.Max(1, Math.Min(chipWidth, content.Width));
            var bounds = new Rectangle(x, y, chipWidth, chipHeight);
            var removeWidth = Math.Min(DpiScaler.Scale(20, dpi), chipWidth);
            var remove = new Rectangle(bounds.Right - removeWidth, bounds.Top, removeWidth, bounds.Height);
            chips.Add(new BootstrapSelectChipLayout(item, bounds, remove));
            x += chipWidth + gap;
        }

        var effectiveRows = Math.Max(1, Math.Min(row, maximumRows));
        var preferredHeight = Math.Max(DpiScaler.Scale(32, dpi), (effectiveRows * chipHeight) + ((effectiveRows - 1) * gap) + (verticalPadding * 2));
        return new BootstrapSelectSelectionLayoutResult(content, arrow, clear, chips, preferredHeight, effectiveRows, overflow);
    }
}
