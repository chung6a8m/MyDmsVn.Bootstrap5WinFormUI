using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapDropdownPalette
{
    public BootstrapDropdownPalette(Color background, Color foreground, Color border, Color accent)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
        Accent = accent;
    }

    public Color Background { get; }
    public Color Foreground { get; }
    public Color Border { get; }
    public Color Accent { get; }
}

internal readonly struct BootstrapDropdownMetrics
{
    public BootstrapDropdownMetrics(int itemHorizontalPadding, int itemVerticalPadding, int imageSize, int separatorInset, float borderWidth, int arrowSize, int gripDotSize)
    {
        ItemHorizontalPadding = itemHorizontalPadding;
        ItemVerticalPadding = itemVerticalPadding;
        ImageSize = imageSize;
        SeparatorInset = separatorInset;
        BorderWidth = borderWidth;
        ArrowSize = arrowSize;
        GripDotSize = gripDotSize;
    }

    public int ItemHorizontalPadding { get; }
    public int ItemVerticalPadding { get; }
    public int ImageSize { get; }
    public int SeparatorInset { get; }
    public float BorderWidth { get; }
    public int ArrowSize { get; }
    public int GripDotSize { get; }
}

internal readonly struct BootstrapToolStripLine
{
    public BootstrapToolStripLine(PointF start, PointF end) { Start = start; End = end; }
    public PointF Start { get; }
    public PointF End { get; }
}

internal readonly struct BootstrapToolStripSplitGeometry
{
    public BootstrapToolStripSplitGeometry(PointF dividerStart, PointF dividerEnd, PointF[] arrowPoints)
    {
        DividerStart = dividerStart;
        DividerEnd = dividerEnd;
        ArrowPoints = arrowPoints;
    }

    public PointF DividerStart { get; }
    public PointF DividerEnd { get; }
    public PointF[] ArrowPoints { get; }
}

internal readonly struct BootstrapToolStripBorderLine
{
    public BootstrapToolStripBorderLine(ToolStripStatusLabelBorderSides side, BootstrapToolStripLine line)
    {
        Side = side;
        Line = line;
    }

    public ToolStripStatusLabelBorderSides Side { get; }
    public BootstrapToolStripLine Line { get; }
}

internal static class BootstrapToolStripRenderLogic
{
    public static BootstrapDropdownMetrics ResolveMetrics(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null) throw new ArgumentNullException(nameof(metrics));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");

        return new BootstrapDropdownMetrics(
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingXS, dpi),
            DpiScaler.Scale(metrics.SpacingLG, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale((float)metrics.BorderWidth, dpi),
            Math.Max(2, DpiScaler.Scale(metrics.SpacingXS, dpi)),
            Math.Max(1, DpiScaler.Scale(2, dpi)));
    }

    public static BootstrapDropdownPalette ResolvePalette(BootstrapThemeColors colors, BootstrapVariant variant, BootstrapToolStripSurfaceKind surfaceKind, bool enabled, bool selected, bool pressed, bool @checked)
    {
        if (colors is null) throw new ArgumentNullException(nameof(colors));
        var variantColor = BootstrapVariantColorResolver.Resolve(colors, variant);
        var neutral = surfaceKind == BootstrapToolStripSurfaceKind.ToolBar || surfaceKind == BootstrapToolStripSurfaceKind.StatusBar
            ? colors.SurfaceSecondary
            : colors.Surface;
        var background = neutral;
        if (enabled)
        {
            if (pressed) background = ColorUtil.Blend(variantColor, neutral, 0.22f);
            else if (@checked) background = ColorUtil.Blend(variantColor, neutral, 0.16f);
            else if (selected) background = ColorUtil.Blend(variantColor, neutral, 0.12f);
        }

        return new BootstrapDropdownPalette(background, enabled ? colors.Text : colors.MutedText, colors.Border, enabled ? variantColor : colors.Disabled);
    }

    public static BootstrapToolStripLine ResolveSeparatorLine(Rectangle bounds, int inset, bool vertical)
    {
        if (vertical)
        {
            var x = bounds.Left + (bounds.Width / 2f);
            return new BootstrapToolStripLine(new PointF(x, Math.Min(bounds.Bottom, bounds.Top + inset)), new PointF(x, Math.Max(bounds.Top, bounds.Bottom - inset)));
        }

        var y = bounds.Top + (bounds.Height / 2f);
        return new BootstrapToolStripLine(new PointF(Math.Min(bounds.Right, bounds.Left + inset), y), new PointF(Math.Max(bounds.Left, bounds.Right - inset), y));
    }

    public static PointF[] ResolveArrowPoints(Rectangle bounds, ArrowDirection direction, int arrowSize)
    {
        if (arrowSize <= 0) throw new ArgumentOutOfRangeException(nameof(arrowSize));
        var halfWidth = Math.Min(arrowSize, Math.Max(1f, (bounds.Width - 1) / 2f));
        var halfHeight = Math.Min(arrowSize, Math.Max(1f, (bounds.Height - 1) / 2f));
        var centerX = bounds.Left + (bounds.Width / 2f);
        var centerY = bounds.Top + (bounds.Height / 2f);
        switch (direction)
        {
            case ArrowDirection.Left:
                return new[] { new PointF(centerX + halfWidth, centerY - halfHeight), new PointF(centerX - halfWidth, centerY), new PointF(centerX + halfWidth, centerY + halfHeight) };
            case ArrowDirection.Up:
                return new[] { new PointF(centerX - halfWidth, centerY + halfHeight), new PointF(centerX, centerY - halfHeight), new PointF(centerX + halfWidth, centerY + halfHeight) };
            case ArrowDirection.Down:
                return new[] { new PointF(centerX - halfWidth, centerY - halfHeight), new PointF(centerX, centerY + halfHeight), new PointF(centerX + halfWidth, centerY - halfHeight) };
            default:
                return new[] { new PointF(centerX - halfWidth, centerY - halfHeight), new PointF(centerX + halfWidth, centerY), new PointF(centerX - halfWidth, centerY + halfHeight) };
        }
    }

    public static BootstrapToolStripSplitGeometry ResolveSplitButtonGeometry(Rectangle buttonBounds, Rectangle dropDownBounds, bool rightToLeft, int arrowSize)
    {
        var dividerX = rightToLeft ? dropDownBounds.Right - 1 : dropDownBounds.Left;
        return new BootstrapToolStripSplitGeometry(
            new PointF(dividerX, dropDownBounds.Top + 2),
            new PointF(dividerX, Math.Max(dropDownBounds.Top + 2, dropDownBounds.Bottom - 3)),
            ResolveArrowPoints(dropDownBounds, ArrowDirection.Down, arrowSize));
    }

    public static IReadOnlyList<BootstrapToolStripBorderLine> ResolveStatusBorderLines(Rectangle bounds, ToolStripStatusLabelBorderSides sides)
    {
        var result = new List<BootstrapToolStripBorderLine>(4);
        AddBorder(result, sides, ToolStripStatusLabelBorderSides.Left, new PointF(bounds.Left, bounds.Top), new PointF(bounds.Left, bounds.Bottom - 1));
        AddBorder(result, sides, ToolStripStatusLabelBorderSides.Top, new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right - 1, bounds.Top));
        AddBorder(result, sides, ToolStripStatusLabelBorderSides.Right, new PointF(bounds.Right - 1, bounds.Top), new PointF(bounds.Right - 1, bounds.Bottom - 1));
        AddBorder(result, sides, ToolStripStatusLabelBorderSides.Bottom, new PointF(bounds.Left, bounds.Bottom - 1), new PointF(bounds.Right - 1, bounds.Bottom - 1));
        return result;
    }

    public static IReadOnlyList<Rectangle> ResolveSizingGripDots(Size surfaceSize, int dotSize, bool rightToLeft)
    {
        if (dotSize <= 0) throw new ArgumentOutOfRangeException(nameof(dotSize));
        var result = new List<Rectangle>(6);
        for (var row = 0; row < 3; row++)
        for (var column = 0; column <= row; column++)
        {
            var edge = (row + 1) * dotSize * 2;
            var x = rightToLeft ? edge : surfaceSize.Width - edge;
            var y = surfaceSize.Height - ((column + 1) * dotSize * 2);
            result.Add(new Rectangle(x, y, dotSize, dotSize));
        }

        return result;
    }

    private static void AddBorder(List<BootstrapToolStripBorderLine> result, ToolStripStatusLabelBorderSides actual, ToolStripStatusLabelBorderSides requested, PointF start, PointF end)
    {
        if ((actual & requested) != 0) result.Add(new BootstrapToolStripBorderLine(requested, new BootstrapToolStripLine(start, end)));
    }
}
