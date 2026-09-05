using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal static class BootstrapListViewLayoutLogic
{
    internal static Rectangle Deflate(Rectangle bounds, int horizontal, int vertical)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        horizontal = Math.Max(0, horizontal);
        vertical = Math.Max(0, vertical);
        var width = bounds.Width - (horizontal * 2);
        var height = bounds.Height - (vertical * 2);
        return width <= 0 || height <= 0
            ? Rectangle.Empty
            : new Rectangle(bounds.X + horizontal, bounds.Y + vertical, width, height);
    }

    internal static Rectangle GetFocusBounds(
        View view,
        Rectangle itemBounds,
        Rectangle labelBounds,
        bool fullRowSelect)
    {
        if (itemBounds.Width <= 0 || itemBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        if ((view == View.Details || view == View.List) && !fullRowSelect)
        {
            return labelBounds.Width > 0 && labelBounds.Height > 0 ? labelBounds : Rectangle.Empty;
        }

        return itemBounds;
    }

    internal static TextFormatFlags GetTextFlags(
        HorizontalAlignment alignment,
        bool rightToLeft,
        bool wordWrap)
    {
        if (alignment != HorizontalAlignment.Left &&
            alignment != HorizontalAlignment.Center &&
            alignment != HorizontalAlignment.Right)
        {
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }

        var flags = TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis;
        flags |= wordWrap ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine;

        if (alignment == HorizontalAlignment.Center)
        {
            flags |= TextFormatFlags.HorizontalCenter;
        }
        else if (alignment == HorizontalAlignment.Right)
        {
            flags |= TextFormatFlags.Right;
        }
        else
        {
            flags |= TextFormatFlags.Left;
        }

        if (rightToLeft)
        {
            flags |= TextFormatFlags.RightToLeft;
        }

        return flags;
    }

    internal static Rectangle GetTileTextBounds(
        Rectangle itemBounds,
        Rectangle imageBounds,
        int gap,
        bool rightToLeft)
    {
        if (itemBounds.Width <= 0 || itemBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        gap = Math.Max(0, gap);
        if (imageBounds.Width <= 0 || imageBounds.Height <= 0)
        {
            return itemBounds;
        }

        if (rightToLeft)
        {
            var right = Math.Min(itemBounds.Right, imageBounds.Left - gap);
            return right <= itemBounds.Left
                ? Rectangle.Empty
                : Rectangle.FromLTRB(itemBounds.Left, itemBounds.Top, right, itemBounds.Bottom);
        }

        var left = Math.Max(itemBounds.Left, imageBounds.Right + gap);
        return left >= itemBounds.Right
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, itemBounds.Top, itemBounds.Right, itemBounds.Bottom);
    }
}
