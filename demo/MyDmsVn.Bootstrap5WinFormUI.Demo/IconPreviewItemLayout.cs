using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal readonly struct IconPreviewItemLayout
{
    private IconPreviewItemLayout(Rectangle iconBounds, Rectangle titleBounds, Rectangle sourceBounds)
    {
        IconBounds = iconBounds;
        TitleBounds = titleBounds;
        SourceBounds = sourceBounds;
    }

    public Rectangle IconBounds { get; }

    public Rectangle TitleBounds { get; }

    public Rectangle SourceBounds { get; }

    public static IconPreviewItemLayout Calculate(
        Graphics graphics,
        Rectangle bounds,
        int dpi,
        Font titleFont,
        Font sourceFont)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (titleFont is null) throw new ArgumentNullException(nameof(titleFont));
        if (sourceFont is null) throw new ArgumentNullException(nameof(sourceFont));

        var inset = DpiScaler.Scale(8, dpi);
        var iconTop = bounds.Top + DpiScaler.Scale(16, dpi);
        var textGap = DpiScaler.Scale(8, dpi);
        var maximumIconSize = DpiScaler.Scale(48, dpi);
        var textWidth = Math.Max(1, bounds.Width - (inset * 2));
        const TextFormatFlags measureFlags = TextFormatFlags.SingleLine
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;
        var proposedSize = new Size(textWidth, Math.Max(1, bounds.Height));
        var titleHeight = TextRenderer.MeasureText(graphics, "Ag", titleFont, proposedSize, measureFlags).Height;
        var sourceHeight = TextRenderer.MeasureText(graphics, "Ag", sourceFont, proposedSize, measureFlags).Height;
        var availableIconHeight = bounds.Bottom - inset - sourceHeight - titleHeight - textGap - iconTop;
        var iconSize = Math.Max(1, Math.Min(maximumIconSize, Math.Min(textWidth, availableIconHeight)));
        var iconBounds = new Rectangle(
            bounds.Left + ((bounds.Width - iconSize) / 2),
            iconTop,
            iconSize,
            iconSize);
        var titleBounds = new Rectangle(
            bounds.Left + inset,
            iconBounds.Bottom + textGap,
            textWidth,
            titleHeight);
        var sourceBounds = new Rectangle(
            bounds.Left + inset,
            titleBounds.Bottom,
            textWidth,
            sourceHeight);

        return new IconPreviewItemLayout(iconBounds, titleBounds, sourceBounds);
    }
}
