using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

/// <summary>
/// Contains the rectangles produced when arranging two horizontal content items.
/// </summary>
public readonly struct HorizontalContentLayout
{
    /// <summary>
    /// Initializes a horizontal content layout result.
    /// </summary>
    public HorizontalContentLayout(Rectangle leadingBounds, Rectangle trailingBounds, Rectangle contentBounds)
    {
        LeadingBounds = leadingBounds;
        TrailingBounds = trailingBounds;
        ContentBounds = contentBounds;
    }

    /// <summary>
    /// Gets the bounds of the first item in reading order.
    /// </summary>
    public Rectangle LeadingBounds { get; }

    /// <summary>
    /// Gets the bounds of the second item in reading order.
    /// </summary>
    public Rectangle TrailingBounds { get; }

    /// <summary>
    /// Gets the union of the non-empty arranged items.
    /// </summary>
    public Rectangle ContentBounds { get; }
}

/// <summary>
/// Provides shared geometry for aligning text/icon-like content inside WinForms bounds.
/// </summary>
public static class ContentLayoutHelper
{
    /// <summary>
    /// Arranges two optional items horizontally as one aligned content group.
    /// </summary>
    public static HorizontalContentLayout ArrangeHorizontal(
        Rectangle bounds,
        Padding padding,
        Size leadingSize,
        Size trailingSize,
        int spacing,
        ContentAlignment alignment)
    {
        if (spacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Spacing cannot be negative.");
        }

        ValidateSize(leadingSize, nameof(leadingSize));
        ValidateSize(trailingSize, nameof(trailingSize));

        var innerBounds = Deflate(bounds, padding);
        var hasLeading = !leadingSize.IsEmpty;
        var hasTrailing = !trailingSize.IsEmpty;
        var effectiveSpacing = hasLeading && hasTrailing ? spacing : 0;
        var contentWidth = (hasLeading ? leadingSize.Width : 0)
            + effectiveSpacing
            + (hasTrailing ? trailingSize.Width : 0);

        var groupLeft = AlignHorizontally(innerBounds, contentWidth, alignment);
        var leadingBounds = Rectangle.Empty;
        var trailingBounds = Rectangle.Empty;
        var cursor = groupLeft;

        if (hasLeading)
        {
            leadingBounds = new Rectangle(
                cursor,
                AlignVertically(innerBounds, leadingSize.Height, alignment),
                leadingSize.Width,
                leadingSize.Height);
            cursor += leadingSize.Width + effectiveSpacing;
        }

        if (hasTrailing)
        {
            trailingBounds = new Rectangle(
                cursor,
                AlignVertically(innerBounds, trailingSize.Height, alignment),
                trailingSize.Width,
                trailingSize.Height);
        }

        return new HorizontalContentLayout(
            leadingBounds,
            trailingBounds,
            UnionNonEmpty(leadingBounds, trailingBounds));
    }

    /// <summary>
    /// Deflates a rectangle by WinForms padding while never returning negative dimensions.
    /// </summary>
    public static Rectangle Deflate(Rectangle bounds, Padding padding)
    {
        var width = Math.Max(0, bounds.Width - padding.Horizontal);
        var height = Math.Max(0, bounds.Height - padding.Vertical);
        return new Rectangle(bounds.Left + padding.Left, bounds.Top + padding.Top, width, height);
    }

    private static int AlignHorizontally(Rectangle bounds, int contentWidth, ContentAlignment alignment)
    {
        return alignment switch
        {
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight
                => bounds.Right - contentWidth,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter
                => bounds.Left + ((bounds.Width - contentWidth) / 2),
            _ => bounds.Left
        };
    }

    private static int AlignVertically(Rectangle bounds, int contentHeight, ContentAlignment alignment)
    {
        return alignment switch
        {
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight
                => bounds.Bottom - contentHeight,
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight
                => bounds.Top + ((bounds.Height - contentHeight) / 2),
            _ => bounds.Top
        };
    }

    private static Rectangle UnionNonEmpty(Rectangle first, Rectangle second)
    {
        if (first.IsEmpty)
        {
            return second;
        }

        if (second.IsEmpty)
        {
            return first;
        }

        return Rectangle.Union(first, second);
    }

    private static void ValidateSize(Size size, string parameterName)
    {
        if (size.Width < 0 || size.Height < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, size, "Content size cannot contain negative dimensions.");
        }
    }
}
