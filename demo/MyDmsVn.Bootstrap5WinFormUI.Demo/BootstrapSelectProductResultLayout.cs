using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal readonly struct BootstrapSelectProductResultLayout
{
    private BootstrapSelectProductResultLayout(Rectangle nameBounds, Rectangle detailsBounds)
    {
        NameBounds = nameBounds;
        DetailsBounds = detailsBounds;
    }

    public Rectangle NameBounds { get; }

    public Rectangle DetailsBounds { get; }

    public static BootstrapSelectProductResultLayout Calculate(
        Graphics graphics,
        Rectangle bounds,
        int dpi,
        Font primaryFont,
        Font secondaryFont)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (primaryFont is null) throw new ArgumentNullException(nameof(primaryFont));
        if (secondaryFont is null) throw new ArgumentNullException(nameof(secondaryFont));

        var horizontalInset = DpiScaler.Scale(8, dpi);
        var verticalInset = DpiScaler.Scale(4, dpi);
        var gap = Math.Max(1, DpiScaler.Scale(1, dpi));
        var inner = Rectangle.Inflate(bounds, -horizontalInset, -verticalInset);
        if (inner.Width <= 0 || inner.Height <= gap + 1)
        {
            return new BootstrapSelectProductResultLayout(Rectangle.Empty, Rectangle.Empty);
        }

        const TextFormatFlags measureFlags = TextFormatFlags.SingleLine
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;
        var proposedSize = new Size(inner.Width, inner.Height);
        var primaryHeight = TextRenderer.MeasureText(graphics, "Ag", primaryFont, proposedSize, measureFlags).Height;
        var secondaryHeight = TextRenderer.MeasureText(graphics, "Ag", secondaryFont, proposedSize, measureFlags).Height;
        var availableHeight = inner.Height - gap;
        secondaryHeight = Math.Max(1, Math.Min(secondaryHeight, availableHeight - 1));
        primaryHeight = Math.Max(1, Math.Min(primaryHeight, availableHeight - secondaryHeight));

        var contentHeight = primaryHeight + gap + secondaryHeight;
        var top = inner.Top + Math.Max(0, (inner.Height - contentHeight) / 2);
        var nameBounds = new Rectangle(inner.Left, top, inner.Width, primaryHeight);
        var detailsBounds = new Rectangle(inner.Left, nameBounds.Bottom + gap, inner.Width, secondaryHeight);
        return new BootstrapSelectProductResultLayout(nameBounds, detailsBounds);
    }
}
