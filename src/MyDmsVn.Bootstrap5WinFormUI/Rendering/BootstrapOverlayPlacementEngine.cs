using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

internal readonly struct BootstrapOverlayPlacementRequest
{
    public BootstrapOverlayPlacementRequest(
        Rectangle anchorBounds,
        Size floatingSize,
        Rectangle boundaryBounds,
        BootstrapOverlayPlacement preferredPlacement,
        BootstrapOverlayCollisionBehavior collisionBehavior,
        int offset,
        int boundaryPadding,
        bool rightToLeft)
    {
        AnchorBounds = anchorBounds;
        FloatingSize = floatingSize;
        BoundaryBounds = boundaryBounds;
        PreferredPlacement = preferredPlacement;
        CollisionBehavior = collisionBehavior;
        Offset = offset;
        BoundaryPadding = boundaryPadding;
        RightToLeft = rightToLeft;
    }

    public Rectangle AnchorBounds { get; }
    public Size FloatingSize { get; }
    public Rectangle BoundaryBounds { get; }
    public BootstrapOverlayPlacement PreferredPlacement { get; }
    public BootstrapOverlayCollisionBehavior CollisionBehavior { get; }
    public int Offset { get; }
    public int BoundaryPadding { get; }
    public bool RightToLeft { get; }
}

internal readonly struct BootstrapOverlayOverflow
{
    public BootstrapOverlayOverflow(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }
    public int Total => Saturate((long)Left + Top + Right + Bottom);

    private static int Saturate(long value)
    {
        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }
}

internal readonly struct BootstrapOverlayPlacementResult
{
    public BootstrapOverlayPlacementResult(
        Rectangle bounds,
        BootstrapOverlayPlacement placement,
        BootstrapOverlayOverflow overflow,
        bool flipped,
        bool shifted)
    {
        Bounds = bounds;
        Placement = placement;
        Overflow = overflow;
        Flipped = flipped;
        Shifted = shifted;
    }

    public Rectangle Bounds { get; }
    public BootstrapOverlayPlacement Placement { get; }
    public BootstrapOverlayOverflow Overflow { get; }
    public bool Flipped { get; }
    public bool Shifted { get; }
}

internal static class BootstrapOverlayPlacementEngine
{
    private static readonly BootstrapOverlayPlacement[] AutoCandidates =
    {
        BootstrapOverlayPlacement.Bottom,
        BootstrapOverlayPlacement.Top,
        BootstrapOverlayPlacement.Right,
        BootstrapOverlayPlacement.Left
    };

    public static BootstrapOverlayPlacementResult Compute(BootstrapOverlayPlacementRequest request)
    {
        Validate(request);
        var boundary = CreateEffectiveBoundary(request.BoundaryBounds, request.BoundaryPadding);

        if (request.PreferredPlacement == BootstrapOverlayPlacement.Auto)
        {
            return ComputeAuto(request, boundary);
        }

        var placement = request.PreferredPlacement;
        var bounds = CalculateBaseBounds(request.AnchorBounds, request.FloatingSize, placement, request.Offset, request.RightToLeft);
        var flipped = false;
        if (IncludesFlip(request.CollisionBehavior))
        {
            var preferredOverflow = CalculateOverflow(bounds, boundary);
            if (GetMainAxisOverflow(preferredOverflow, placement) > 0)
            {
                var opposite = GetOpposite(placement);
                var oppositeBounds = CalculateBaseBounds(request.AnchorBounds, request.FloatingSize, opposite, request.Offset, request.RightToLeft);
                var oppositeOverflow = CalculateOverflow(oppositeBounds, boundary);
                if (GetMainAxisOverflow(oppositeOverflow, opposite) == 0 || oppositeOverflow.Total < preferredOverflow.Total)
                {
                    placement = opposite;
                    bounds = oppositeBounds;
                    flipped = true;
                }
            }
        }

        var shifted = false;
        if (IncludesShift(request.CollisionBehavior))
        {
            var shiftedBounds = Shift(bounds, placement, boundary);
            shifted = shiftedBounds.Location != bounds.Location;
            bounds = shiftedBounds;
        }

        return new BootstrapOverlayPlacementResult(bounds, placement, CalculateOverflow(bounds, boundary), flipped, shifted);
    }

    private static BootstrapOverlayPlacementResult ComputeAuto(BootstrapOverlayPlacementRequest request, LongRectangle boundary)
    {
        var bestPlacement = AutoCandidates[0];
        var bestBounds = CalculateBaseBounds(request.AnchorBounds, request.FloatingSize, bestPlacement, request.Offset, request.RightToLeft);
        var bestOverflow = CalculateOverflow(bestBounds, boundary);
        var bestIntersectionArea = CalculateIntersectionArea(bestBounds, boundary);

        for (var index = 1; index < AutoCandidates.Length; index++)
        {
            var placement = AutoCandidates[index];
            var bounds = CalculateBaseBounds(request.AnchorBounds, request.FloatingSize, placement, request.Offset, request.RightToLeft);
            var overflow = CalculateOverflow(bounds, boundary);
            var intersectionArea = CalculateIntersectionArea(bounds, boundary);
            if (overflow.Total < bestOverflow.Total ||
                (overflow.Total == bestOverflow.Total && intersectionArea > bestIntersectionArea))
            {
                bestPlacement = placement;
                bestBounds = bounds;
                bestOverflow = overflow;
                bestIntersectionArea = intersectionArea;
            }
        }

        var shifted = false;
        if (IncludesShift(request.CollisionBehavior))
        {
            var shiftedBounds = Shift(bestBounds, bestPlacement, boundary);
            shifted = shiftedBounds.Location != bestBounds.Location;
            bestBounds = shiftedBounds;
            bestOverflow = CalculateOverflow(bestBounds, boundary);
        }

        return new BootstrapOverlayPlacementResult(bestBounds, bestPlacement, bestOverflow, false, shifted);
    }

    private static Rectangle CalculateBaseBounds(Rectangle anchor, Size floating, BootstrapOverlayPlacement placement, int offset, bool rightToLeft)
    {
        var anchorLeft = (long)anchor.X;
        var anchorTop = (long)anchor.Y;
        var anchorWidth = Math.Max(0, anchor.Width);
        var anchorHeight = Math.Max(0, anchor.Height);
        var anchorRight = anchorLeft + anchorWidth;
        var anchorBottom = anchorTop + anchorHeight;
        var width = Math.Max(0, floating.Width);
        var height = Math.Max(0, floating.Height);
        long x;
        long y;

        switch (placement)
        {
            case BootstrapOverlayPlacement.Top:
            case BootstrapOverlayPlacement.Bottom:
                x = anchorLeft + ((long)anchorWidth - width) / 2;
                break;
            case BootstrapOverlayPlacement.TopStart:
            case BootstrapOverlayPlacement.BottomStart:
                x = rightToLeft ? anchorRight - width : anchorLeft;
                break;
            case BootstrapOverlayPlacement.TopEnd:
            case BootstrapOverlayPlacement.BottomEnd:
                x = rightToLeft ? anchorLeft : anchorRight - width;
                break;
            case BootstrapOverlayPlacement.Left:
            case BootstrapOverlayPlacement.LeftStart:
            case BootstrapOverlayPlacement.LeftEnd:
                x = anchorLeft - offset - width;
                break;
            default:
                x = anchorRight + offset;
                break;
        }

        switch (placement)
        {
            case BootstrapOverlayPlacement.Left:
            case BootstrapOverlayPlacement.Right:
                y = anchorTop + ((long)anchorHeight - height) / 2;
                break;
            case BootstrapOverlayPlacement.LeftStart:
            case BootstrapOverlayPlacement.RightStart:
                y = anchorTop;
                break;
            case BootstrapOverlayPlacement.LeftEnd:
            case BootstrapOverlayPlacement.RightEnd:
                y = anchorBottom - height;
                break;
            case BootstrapOverlayPlacement.Top:
            case BootstrapOverlayPlacement.TopStart:
            case BootstrapOverlayPlacement.TopEnd:
                y = anchorTop - offset - height;
                break;
            default:
                y = anchorBottom + offset;
                break;
        }

        return new Rectangle(SaturateCoordinate(x), SaturateCoordinate(y), width, height);
    }

    private static LongRectangle CreateEffectiveBoundary(Rectangle boundary, int padding)
    {
        var left = (long)boundary.X;
        var top = (long)boundary.Y;
        var width = Math.Max(0, boundary.Width);
        var height = Math.Max(0, boundary.Height);
        return new LongRectangle(
            InsetAxisStart(left, width, padding),
            InsetAxisStart(top, height, padding),
            InsetAxisEnd(left, width, padding),
            InsetAxisEnd(top, height, padding));
    }

    private static long InsetAxisStart(long start, int length, int padding)
    {
        return (long)padding * 2 >= length ? start + length / 2L : start + padding;
    }

    private static long InsetAxisEnd(long start, int length, int padding)
    {
        return (long)padding * 2 >= length ? start + length / 2L : start + length - padding;
    }

    private static BootstrapOverlayOverflow CalculateOverflow(Rectangle bounds, LongRectangle boundary)
    {
        var left = (long)bounds.X;
        var top = (long)bounds.Y;
        var right = left + Math.Max(0, bounds.Width);
        var bottom = top + Math.Max(0, bounds.Height);
        return new BootstrapOverlayOverflow(
            SaturateDistance(Math.Max(0L, boundary.Left - left)),
            SaturateDistance(Math.Max(0L, boundary.Top - top)),
            SaturateDistance(Math.Max(0L, right - boundary.Right)),
            SaturateDistance(Math.Max(0L, bottom - boundary.Bottom)));
    }

    private static Rectangle Shift(Rectangle bounds, BootstrapOverlayPlacement placement, LongRectangle boundary)
    {
        var x = (long)bounds.X;
        var y = (long)bounds.Y;
        if (IsVerticalSide(placement))
        {
            x = ClampAxis(x, bounds.Width, boundary.Left, boundary.Right);
        }
        else
        {
            y = ClampAxis(y, bounds.Height, boundary.Top, boundary.Bottom);
        }

        return new Rectangle(SaturateCoordinate(x), SaturateCoordinate(y), bounds.Width, bounds.Height);
    }

    private static long ClampAxis(long coordinate, int length, long boundaryStart, long boundaryEnd)
    {
        var boundaryLength = Math.Max(0L, boundaryEnd - boundaryStart);
        if (length >= boundaryLength)
        {
            return boundaryStart;
        }

        return Math.Max(boundaryStart, Math.Min(coordinate, boundaryEnd - length));
    }

    private static long CalculateIntersectionArea(Rectangle bounds, LongRectangle boundary)
    {
        var left = Math.Max((long)bounds.X, boundary.Left);
        var top = Math.Max((long)bounds.Y, boundary.Top);
        var right = Math.Min((long)bounds.X + Math.Max(0, bounds.Width), boundary.Right);
        var bottom = Math.Min((long)bounds.Y + Math.Max(0, bounds.Height), boundary.Bottom);
        return Math.Max(0L, right - left) * Math.Max(0L, bottom - top);
    }

    private static BootstrapOverlayPlacement GetOpposite(BootstrapOverlayPlacement placement)
    {
        switch (placement)
        {
            case BootstrapOverlayPlacement.Top: return BootstrapOverlayPlacement.Bottom;
            case BootstrapOverlayPlacement.TopStart: return BootstrapOverlayPlacement.BottomStart;
            case BootstrapOverlayPlacement.TopEnd: return BootstrapOverlayPlacement.BottomEnd;
            case BootstrapOverlayPlacement.Bottom: return BootstrapOverlayPlacement.Top;
            case BootstrapOverlayPlacement.BottomStart: return BootstrapOverlayPlacement.TopStart;
            case BootstrapOverlayPlacement.BottomEnd: return BootstrapOverlayPlacement.TopEnd;
            case BootstrapOverlayPlacement.Left: return BootstrapOverlayPlacement.Right;
            case BootstrapOverlayPlacement.LeftStart: return BootstrapOverlayPlacement.RightStart;
            case BootstrapOverlayPlacement.LeftEnd: return BootstrapOverlayPlacement.RightEnd;
            case BootstrapOverlayPlacement.Right: return BootstrapOverlayPlacement.Left;
            case BootstrapOverlayPlacement.RightStart: return BootstrapOverlayPlacement.LeftStart;
            case BootstrapOverlayPlacement.RightEnd: return BootstrapOverlayPlacement.LeftEnd;
            default: throw new ArgumentOutOfRangeException(nameof(placement));
        }
    }

    private static int GetMainAxisOverflow(BootstrapOverlayOverflow overflow, BootstrapOverlayPlacement placement)
    {
        switch (placement)
        {
            case BootstrapOverlayPlacement.Top:
            case BootstrapOverlayPlacement.TopStart:
            case BootstrapOverlayPlacement.TopEnd:
                return overflow.Top;
            case BootstrapOverlayPlacement.Bottom:
            case BootstrapOverlayPlacement.BottomStart:
            case BootstrapOverlayPlacement.BottomEnd:
                return overflow.Bottom;
            case BootstrapOverlayPlacement.Left:
            case BootstrapOverlayPlacement.LeftStart:
            case BootstrapOverlayPlacement.LeftEnd:
                return overflow.Left;
            default:
                return overflow.Right;
        }
    }

    private static bool IsVerticalSide(BootstrapOverlayPlacement placement)
    {
        return placement >= BootstrapOverlayPlacement.Top && placement <= BootstrapOverlayPlacement.BottomEnd;
    }

    private static bool IncludesFlip(BootstrapOverlayCollisionBehavior behavior)
    {
        return behavior == BootstrapOverlayCollisionBehavior.Flip || behavior == BootstrapOverlayCollisionBehavior.FlipAndShift;
    }

    private static bool IncludesShift(BootstrapOverlayCollisionBehavior behavior)
    {
        return behavior == BootstrapOverlayCollisionBehavior.Shift || behavior == BootstrapOverlayCollisionBehavior.FlipAndShift;
    }

    private static void Validate(BootstrapOverlayPlacementRequest request)
    {
        if (request.PreferredPlacement < BootstrapOverlayPlacement.Auto || request.PreferredPlacement > BootstrapOverlayPlacement.RightEnd)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PreferredPlacement));
        }

        if (request.CollisionBehavior < BootstrapOverlayCollisionBehavior.None || request.CollisionBehavior > BootstrapOverlayCollisionBehavior.FlipAndShift)
        {
            throw new ArgumentOutOfRangeException(nameof(request.CollisionBehavior));
        }

        if (request.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Offset));
        }

        if (request.BoundaryPadding < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.BoundaryPadding));
        }
    }

    private static int SaturateCoordinate(long value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }

    private static int SaturateDistance(long value)
    {
        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }

    private readonly struct LongRectangle
    {
        public LongRectangle(long left, long top, long right, long bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public long Left { get; }
        public long Top { get; }
        public long Right { get; }
        public long Bottom { get; }
    }
}
