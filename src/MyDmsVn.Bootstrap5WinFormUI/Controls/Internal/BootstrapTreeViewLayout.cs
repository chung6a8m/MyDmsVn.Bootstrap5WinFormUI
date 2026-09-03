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
        Size nodeImageSize)
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

    private static Rectangle Normalize(Rectangle rectangle)
    {
        return new Rectangle(
            rectangle.X,
            rectangle.Y,
            Math.Max(0, rectangle.Width),
            Math.Max(0, rectangle.Height));
    }
}

internal readonly struct BootstrapTreeViewNodeLayout
{
    internal BootstrapTreeViewNodeLayout(
        Rectangle rowBounds,
        Rectangle selectionBounds,
        Rectangle expanderBounds,
        Rectangle stateImageBounds,
        Rectangle nodeImageBounds,
        Rectangle textBounds,
        Rectangle focusBounds)
    {
        RowBounds = rowBounds;
        SelectionBounds = selectionBounds;
        ExpanderBounds = expanderBounds;
        StateImageBounds = stateImageBounds;
        NodeImageBounds = nodeImageBounds;
        TextBounds = textBounds;
        FocusBounds = focusBounds;
    }

    internal Rectangle RowBounds { get; }

    internal Rectangle SelectionBounds { get; }

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
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty,
                Rectangle.Empty);
        }

        var textBounds = Intersect(input.NativeLabelBounds, rowBounds);
        var imageGap = DpiScaler.Scale(LogicalImageTextGap, input.Dpi);
        var expanderSize = DpiScaler.Scale(LogicalExpanderSize, input.Dpi);
        var expanderSlotWidth = DpiScaler.Scale(LogicalExpanderSlotWidth, input.Dpi);
        var stateImageSize = DpiScaler.Scale(LogicalStateImageSize, input.Dpi);

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
        var expanderBounds = input.HasExpander
            ? PlaceBackwardInSlot(ref cursor, expanderSlotWidth, expanderSize, rowBounds)
            : Rectangle.Empty;

        var selectionBounds = input.EffectiveFullRowSelection ? rowBounds : textBounds;
        return new BootstrapTreeViewNodeLayout(
            rowBounds,
            selectionBounds,
            expanderBounds,
            stateImageBounds,
            nodeImageBounds,
            textBounds,
            selectionBounds);
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
        var width = Math.Max(0, slotWidth);
        var slotLeft = cursor - width;
        var size = Math.Min(width, Math.Max(0, desiredSize));
        var rectangle = CenterVertically(slotLeft + ((width - size) / 2), size, size, rowBounds);
        cursor = slotLeft;
        return Intersect(rectangle, rowBounds);
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
}
