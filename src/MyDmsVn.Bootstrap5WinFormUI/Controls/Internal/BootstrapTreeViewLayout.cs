using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal readonly struct BootstrapTreeViewLayoutInput
{
    internal BootstrapTreeViewLayoutInput(
        Rectangle clientBounds,
        Rectangle drawBounds,
        Rectangle nativeLabelBounds,
        int nodeLevel,
        int dpi,
        bool rightToLeft,
        bool effectiveFullRowSelection,
        bool hasExpander,
        bool hasStateImage,
        int nativeStateImageSlotWidth,
        bool hasNodeImage,
        Size nodeImageSize,
        bool useNativeStateImageSize = false)
    {
        if (nodeLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeLevel));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi));
        }

        if (nativeStateImageSlotWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nativeStateImageSlotWidth));
        }

        if (nodeImageSize.Width < 0 || nodeImageSize.Height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeImageSize));
        }

        ClientBounds = Normalize(clientBounds);
        DrawBounds = Normalize(drawBounds);
        NativeLabelBounds = Normalize(nativeLabelBounds);
        NodeLevel = nodeLevel;
        Dpi = dpi;
        RightToLeft = rightToLeft;
        EffectiveFullRowSelection = effectiveFullRowSelection;
        HasExpander = hasExpander;
        HasStateImage = hasStateImage;
        NativeStateImageSlotWidth = nativeStateImageSlotWidth;
        HasNodeImage = hasNodeImage;
        NodeImageSize = nodeImageSize;
        UseNativeStateImageSize = useNativeStateImageSize;
    }

    internal Rectangle ClientBounds { get; }

    internal Rectangle DrawBounds { get; }

    internal Rectangle NativeLabelBounds { get; }

    internal int NodeLevel { get; }

    internal int Dpi { get; }

    internal bool RightToLeft { get; }

    internal bool EffectiveFullRowSelection { get; }

    internal bool HasExpander { get; }

    internal bool HasStateImage { get; }

    internal int NativeStateImageSlotWidth { get; }

    internal bool HasNodeImage { get; }

    internal Size NodeImageSize { get; }

    internal bool UseNativeStateImageSize { get; }

    private static Rectangle Normalize(Rectangle rectangle)
    {
        return new Rectangle(
            rectangle.X,
            rectangle.Y,
            Math.Max(0, rectangle.Width),
            Math.Max(0, rectangle.Height));
    }
}

internal readonly struct BootstrapTreeViewLineSegment
{
    internal BootstrapTreeViewLineSegment(Point start, Point end)
    {
        Start = start;
        End = end;
    }

    internal Point Start { get; }

    internal Point End { get; }

    internal bool IsEmpty => Start == End;
}

internal readonly struct BootstrapTreeViewExpanderGlyph
{
    internal BootstrapTreeViewExpanderGlyph(Point first, Point tip, Point second)
    {
        First = first;
        Tip = tip;
        Second = second;
    }

    internal Point First { get; }

    internal Point Tip { get; }

    internal Point Second { get; }
}

internal readonly struct BootstrapTreeViewNodeLayout
{
    internal BootstrapTreeViewNodeLayout(
        Rectangle rowBounds,
        Rectangle selectionBounds,
        Rectangle expanderSlotBounds,
        int expanderAnchorX,
        Rectangle expanderBounds,
        Rectangle stateImageBounds,
        Rectangle nodeImageBounds,
        Rectangle textBounds,
        Rectangle focusBounds)
    {
        RowBounds = rowBounds;
        SelectionBounds = selectionBounds;
        ExpanderSlotBounds = expanderSlotBounds;
        ExpanderAnchorX = expanderAnchorX;
        ExpanderBounds = expanderBounds;
        StateImageBounds = stateImageBounds;
        NodeImageBounds = nodeImageBounds;
        TextBounds = textBounds;
        FocusBounds = focusBounds;
    }

    internal Rectangle RowBounds { get; }

    internal Rectangle SelectionBounds { get; }

    internal Rectangle ExpanderSlotBounds { get; }

    internal int ExpanderAnchorX { get; }

    internal Rectangle ExpanderBounds { get; }

    internal Rectangle StateImageBounds { get; }

    internal Rectangle NodeImageBounds { get; }

    internal Rectangle TextBounds { get; }

    internal Rectangle FocusBounds { get; }
}

internal static class BootstrapTreeViewLayout
{
    private const int LogicalExpanderSize = 9;
    private const int LogicalExpanderSlotWidth = 19;
    private const int LogicalImageTextGap = 3;
    private const int LogicalStateImageSize = 13;

    internal static BootstrapTreeViewNodeLayout Calculate(BootstrapTreeViewLayoutInput input)
    {
        var rowBand = new Rectangle(
            input.ClientBounds.Left,
            input.DrawBounds.Top,
            input.ClientBounds.Width,
            input.DrawBounds.Height);
        var rowBounds = Intersect(rowBand, input.ClientBounds);
        if (rowBounds.IsEmpty)
        {
            return new BootstrapTreeViewNodeLayout(
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                0,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty);
        }

        var textBounds = Intersect(input.NativeLabelBounds, rowBounds);
        var imageGap = DpiScaler.Scale(LogicalImageTextGap, input.Dpi);
        var expanderSize = DpiScaler.Scale(LogicalExpanderSize, input.Dpi);
        var expanderSlotWidth = DpiScaler.Scale(LogicalExpanderSlotWidth, input.Dpi);
        var stateImageSize = input.UseNativeStateImageSize
            ? input.NativeStateImageSlotWidth
            : DpiScaler.Scale(LogicalStateImageSize, input.Dpi);

        // TreeNode.Bounds is already reported in the native mirrored coordinate system.
        // Both LTR and RTL native structure slots therefore precede the label's Left edge;
        // mirroring them a second time around the label would move them away from HitTest regions.
        var cursor = input.NativeLabelBounds.Left;
        var nodeImageBounds = input.HasNodeImage
            ? PlaceBackward(ref cursor, input.NodeImageSize, imageGap, rowBounds)
            : Rectangle.Empty;
        var stateImageBounds = input.HasStateImage
            ? PlaceBackwardInSlot(ref cursor, input.NativeStateImageSlotWidth, stateImageSize, rowBounds)
            : Rectangle.Empty;

        // Keep the un-clipped native-anchored slot long enough to center both the glyph and the
        // connector anchor. Clip only the drawable rectangles; horizontal scrolling must not
        // recenter framework geometry away from the native HitTest column.
        var rawExpanderSlot = TakeBackwardSlot(ref cursor, expanderSlotWidth, rowBounds);
        var expanderAnchorX = rawExpanderSlot.Left + (rawExpanderSlot.Width / 2);
        var expanderSlotBounds = Intersect(rawExpanderSlot, rowBounds);
        var expanderBounds = input.HasExpander
            ? Intersect(CenterInSlot(rawExpanderSlot, expanderSize), rowBounds)
            : Rectangle.Empty;

        var selectionBounds = input.EffectiveFullRowSelection ? rowBounds : textBounds;
        return new BootstrapTreeViewNodeLayout(
            rowBounds,
            selectionBounds,
            expanderSlotBounds,
            expanderAnchorX,
            expanderBounds,
            stateImageBounds,
            nodeImageBounds,
            textBounds,
            selectionBounds);
    }

    internal static Rectangle CalculateContainedImageBounds(Rectangle slotBounds, Size imageSize)
    {
        if (slotBounds.Width <= 0 || slotBounds.Height <= 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var widthScale = (double)slotBounds.Width / imageSize.Width;
        var heightScale = (double)slotBounds.Height / imageSize.Height;
        var scale = Math.Min(1d, Math.Min(widthScale, heightScale));
        var width = Math.Max(1, Math.Min(slotBounds.Width, (int)Math.Round(imageSize.Width * scale)));
        var height = Math.Max(1, Math.Min(slotBounds.Height, (int)Math.Round(imageSize.Height * scale)));
        return new Rectangle(
            slotBounds.Left + ((slotBounds.Width - width) / 2),
            slotBounds.Top + ((slotBounds.Height - height) / 2),
            width,
            height);
    }

    internal static BootstrapTreeViewExpanderGlyph CalculateExpanderGlyph(
        Rectangle bounds,
        bool expanded,
        bool rightToLeft)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new BootstrapTreeViewExpanderGlyph(Point.Empty, Point.Empty, Point.Empty);
        }

        var horizontalInset = Math.Max(1, bounds.Width / 4);
        var verticalInset = Math.Max(1, bounds.Height / 4);
        var left = bounds.Left + horizontalInset;
        var right = Math.Max(left, bounds.Right - horizontalInset - 1);
        var top = bounds.Top + verticalInset;
        var bottom = Math.Max(top, bounds.Bottom - verticalInset - 1);
        var centerX = bounds.Left + (bounds.Width / 2);
        var centerY = bounds.Top + (bounds.Height / 2);

        if (expanded)
        {
            return new BootstrapTreeViewExpanderGlyph(
                new Point(left, top),
                new Point(centerX, bottom),
                new Point(right, top));
        }

        if (rightToLeft)
        {
            return new BootstrapTreeViewExpanderGlyph(
                new Point(right, top),
                new Point(left, centerY),
                new Point(right, bottom));
        }

        return new BootstrapTreeViewExpanderGlyph(
            new Point(left, top),
            new Point(right, centerY),
            new Point(left, bottom));
    }

    internal static BootstrapTreeViewLineSegment CalculateVerticalConnectorLine(
        Rectangle rowBounds,
        int x,
        bool continueAbove,
        bool continueBelow)
    {
        if (rowBounds.Width <= 0 || rowBounds.Height <= 0 || (!continueAbove && !continueBelow))
        {
            return new BootstrapTreeViewLineSegment(Point.Empty, Point.Empty);
        }

        var safeX = Clamp(x, rowBounds.Left, rowBounds.Right - 1);
        var centerY = rowBounds.Top + (rowBounds.Height / 2);
        var startY = continueAbove ? rowBounds.Top : centerY;
        var endY = continueBelow ? rowBounds.Bottom - 1 : centerY;
        return new BootstrapTreeViewLineSegment(new Point(safeX, startY), new Point(safeX, endY));
    }

    internal static BootstrapTreeViewLineSegment CalculateHorizontalConnectorLine(
        Rectangle rowBounds,
        int fromX,
        int toX)
    {
        if (rowBounds.Width <= 0 || rowBounds.Height <= 0)
        {
            return new BootstrapTreeViewLineSegment(Point.Empty, Point.Empty);
        }

        var safeFromX = Clamp(fromX, rowBounds.Left, rowBounds.Right - 1);
        var safeToX = Clamp(toX, rowBounds.Left, rowBounds.Right - 1);
        var centerY = rowBounds.Top + (rowBounds.Height / 2);
        return new BootstrapTreeViewLineSegment(
            new Point(safeFromX, centerY),
            new Point(safeToX, centerY));
    }

    internal static int CalculateAncestorConnectorX(
        int currentAnchorX,
        int currentLevel,
        int ancestorLevel,
        int indent,
        bool rightToLeft)
    {
        if (currentLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentLevel));
        }

        if (ancestorLevel < 0 || ancestorLevel >= currentLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(ancestorLevel));
        }

        if (indent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indent));
        }

        var delta = (long)(currentLevel - ancestorLevel) * indent;
        var value = rightToLeft ? currentAnchorX + delta : currentAnchorX - delta;
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)value;
    }

    private static Rectangle PlaceBackward(ref int cursor, Size size, int gap, Rectangle rowBounds)
    {
        var width = Math.Max(0, size.Width);
        var height = Math.Max(0, size.Height);
        var right = cursor - gap;
        var rectangle = CenterVertically(right - width, width, height, rowBounds);
        cursor = right - width;
        return Intersect(rectangle, rowBounds);
    }

    private static Rectangle PlaceBackwardInSlot(ref int cursor, int slotWidth, int desiredSize, Rectangle rowBounds)
    {
        var rawSlot = TakeBackwardSlot(ref cursor, slotWidth, rowBounds);
        return Intersect(CenterInSlot(rawSlot, desiredSize), rowBounds);
    }

    private static Rectangle TakeBackwardSlot(ref int cursor, int slotWidth, Rectangle rowBounds)
    {
        var width = Math.Max(0, slotWidth);
        var slotLeft = cursor - width;
        cursor = slotLeft;
        return new Rectangle(slotLeft, rowBounds.Top, width, rowBounds.Height);
    }

    private static Rectangle CenterInSlot(Rectangle slotBounds, int desiredSize)
    {
        if (slotBounds.Width <= 0 || slotBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var size = Math.Min(
            Math.Min(slotBounds.Width, slotBounds.Height),
            Math.Max(0, desiredSize));
        var x = slotBounds.Left + ((slotBounds.Width - size) / 2);
        var y = slotBounds.Top + ((slotBounds.Height - size) / 2);
        return new Rectangle(x, y, size, size);
    }

    private static Rectangle CenterVertically(int x, int width, int height, Rectangle rowBounds)
    {
        var safeWidth = Math.Max(0, width);
        var safeHeight = Math.Max(0, height);
        var y = rowBounds.Top + ((rowBounds.Height - safeHeight) / 2);
        return new Rectangle(x, y, safeWidth, safeHeight);
    }

    private static Rectangle Intersect(Rectangle first, Rectangle second)
    {
        if (first.Width <= 0 || first.Height <= 0 || second.Width <= 0 || second.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var left = Math.Max(first.Left, second.Left);
        var top = Math.Max(first.Top, second.Top);
        var right = Math.Min(first.Right, second.Right);
        var bottom = Math.Min(first.Bottom, second.Bottom);
        if (right <= left || bottom <= top)
        {
            return Rectangle.Empty;
        }

        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }
}
