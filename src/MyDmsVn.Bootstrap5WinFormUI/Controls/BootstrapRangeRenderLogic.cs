using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal readonly struct BootstrapRangeVisualState
{
    internal BootstrapRangeVisualState(bool enabled, bool focused, bool hot, bool pressed)
    {
        Enabled = enabled;
        Focused = focused;
        Hot = hot;
        Pressed = pressed;
    }

    internal bool Enabled { get; }

    internal bool Focused { get; }

    internal bool Hot { get; }

    internal bool Pressed { get; }
}

internal readonly struct BootstrapRangePalette
{
    internal BootstrapRangePalette(
        Color backgroundColor,
        Color railColor,
        Color thumbColor,
        Color focusColor,
        Color tickColor,
        bool drawFocusHalo)
    {
        BackgroundColor = backgroundColor;
        RailColor = railColor;
        ThumbColor = thumbColor;
        FocusColor = focusColor;
        TickColor = tickColor;
        DrawFocusHalo = drawFocusHalo;
    }

    internal Color BackgroundColor { get; }

    internal Color RailColor { get; }

    internal Color ThumbColor { get; }

    internal Color FocusColor { get; }

    internal Color TickColor { get; }

    internal bool DrawFocusHalo { get; }
}

internal readonly struct BootstrapRangeGeometry
{
    internal BootstrapRangeGeometry(
        Rectangle railBounds,
        Rectangle thumbBounds,
        Rectangle focusHaloBounds,
        float railRadius,
        float focusThickness,
        float tickThickness)
    {
        RailBounds = railBounds;
        ThumbBounds = thumbBounds;
        FocusHaloBounds = focusHaloBounds;
        RailRadius = railRadius;
        FocusThickness = focusThickness;
        TickThickness = tickThickness;
    }

    internal Rectangle RailBounds { get; }

    internal Rectangle ThumbBounds { get; }

    internal Rectangle FocusHaloBounds { get; }

    internal float RailRadius { get; }

    internal float FocusThickness { get; }

    internal float TickThickness { get; }
}

internal static class BootstrapRangeRenderLogic
{
    internal static BootstrapRangePalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        BootstrapRangeVisualState state)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        var accent = BootstrapVariantColorResolver.Resolve(colors, variant);
        if (!state.Enabled)
        {
            return new BootstrapRangePalette(
                colors.Surface,
                ColorUtil.Blend(colors.Disabled, colors.Surface, 0.55f),
                colors.Disabled,
                colors.Focus,
                colors.Disabled,
                drawFocusHalo: false);
        }

        var thumbColor = state.Pressed
            ? ColorUtil.Blend(colors.Dark, accent, 0.20f)
            : state.Hot
                ? ColorUtil.Blend(colors.Light, accent, 0.15f)
                : accent;
        return new BootstrapRangePalette(
            colors.Surface,
            colors.Border,
            thumbColor,
            colors.Focus,
            colors.MutedText,
            state.Focused);
    }

    internal static BootstrapRangeGeometry CalculateGeometry(
        Rectangle nativeChannelBounds,
        Rectangle nativeThumbBounds,
        Orientation orientation,
        BootstrapThemeMetrics metrics,
        int dpi,
        bool drawFocusHalo)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        var railThickness = Math.Max(1, DpiScaler.Scale(metrics.SpacingXS, dpi));
        var railBounds = CenterRail(nativeChannelBounds, orientation, railThickness);
        var thumbInset = Math.Max(1, DpiScaler.Scale(metrics.BorderWidth, dpi));
        var thumbBounds = CenterSquare(nativeThumbBounds, thumbInset);
        var focusThickness = Math.Max(1f, DpiScaler.Scale((float)metrics.FocusBorderWidth, dpi));
        var focusInflation = drawFocusHalo
            ? CalculateFocusInflation(metrics, dpi)
            : 0;
        var focusBounds = thumbBounds;
        if (focusInflation > 0 && !thumbBounds.IsEmpty)
        {
            focusBounds.Inflate(focusInflation, focusInflation);
        }

        return new BootstrapRangeGeometry(
            railBounds,
            thumbBounds,
            focusBounds,
            railBounds.Height > 0 && orientation == Orientation.Horizontal
                ? railBounds.Height / 2f
                : railBounds.Width / 2f,
            focusThickness,
            Math.Max(1f, DpiScaler.Scale((float)metrics.BorderWidth, dpi)));
    }

    internal static int CalculateFocusInflation(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        var focusGap = Math.Max(1, DpiScaler.Scale(metrics.BorderWidth, dpi));
        var focusThickness = Math.Max(1f, DpiScaler.Scale((float)metrics.FocusBorderWidth, dpi));
        return focusGap + (int)Math.Ceiling(focusThickness / 2f);
    }

    internal static IReadOnlyList<RectangleF> CalculateTicks(
        Rectangle clientBounds,
        Rectangle nativeChannelBounds,
        Orientation orientation,
        TickStyle tickStyle,
        IReadOnlyList<int> intermediatePositions,
        BootstrapThemeMetrics metrics,
        int dpi)
    {
        if (intermediatePositions is null)
        {
            throw new ArgumentNullException(nameof(intermediatePositions));
        }

        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (tickStyle == TickStyle.None || nativeChannelBounds.IsEmpty || clientBounds.IsEmpty)
        {
            return Array.Empty<RectangleF>();
        }

        var positions = BuildPhysicalTickPositions(nativeChannelBounds, orientation, intermediatePositions);
        var tickLength = Math.Max(1f, DpiScaler.Scale((float)metrics.SpacingXS, dpi));
        var tickThickness = Math.Max(1f, DpiScaler.Scale((float)metrics.BorderWidth, dpi));
        var gap = Math.Max(1f, DpiScaler.Scale((float)metrics.BorderWidth, dpi));
        var ticks = new List<RectangleF>(positions.Count * (tickStyle == TickStyle.Both ? 2 : 1));

        if (tickStyle == TickStyle.TopLeft || tickStyle == TickStyle.Both)
        {
            AddTickSide(ticks, positions, nativeChannelBounds, orientation, topOrLeft: true, tickLength, tickThickness, gap);
        }

        if (tickStyle == TickStyle.BottomRight || tickStyle == TickStyle.Both)
        {
            AddTickSide(ticks, positions, nativeChannelBounds, orientation, topOrLeft: false, tickLength, tickThickness, gap);
        }

        return ticks;
    }

    private static Rectangle CenterRail(Rectangle nativeBounds, Orientation orientation, int requestedThickness)
    {
        if (nativeBounds.Width <= 0 || nativeBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        if (orientation == Orientation.Horizontal)
        {
            var thickness = Math.Min(nativeBounds.Height, requestedThickness);
            return new Rectangle(
                nativeBounds.X,
                nativeBounds.Y + ((nativeBounds.Height - thickness) / 2),
                nativeBounds.Width,
                thickness);
        }

        var width = Math.Min(nativeBounds.Width, requestedThickness);
        return new Rectangle(
            nativeBounds.X + ((nativeBounds.Width - width) / 2),
            nativeBounds.Y,
            width,
            nativeBounds.Height);
    }

    private static Rectangle CenterSquare(Rectangle nativeBounds, int requestedInset)
    {
        if (nativeBounds.Width <= 0 || nativeBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var maximumInset = Math.Max(0, (Math.Min(nativeBounds.Width, nativeBounds.Height) - 1) / 2);
        var inset = Math.Min(maximumInset, requestedInset);
        var diameter = Math.Max(1, Math.Min(nativeBounds.Width, nativeBounds.Height) - (inset * 2));
        return new Rectangle(
            nativeBounds.X + ((nativeBounds.Width - diameter) / 2),
            nativeBounds.Y + ((nativeBounds.Height - diameter) / 2),
            diameter,
            diameter);
    }

    private static List<int> BuildPhysicalTickPositions(
        Rectangle channelBounds,
        Orientation orientation,
        IReadOnlyList<int> intermediatePositions)
    {
        var start = orientation == Orientation.Horizontal ? channelBounds.Left : channelBounds.Top;
        var end = orientation == Orientation.Horizontal ? channelBounds.Right - 1 : channelBounds.Bottom - 1;
        var positions = new List<int>(intermediatePositions.Count + 2) { start };
        for (var index = 0; index < intermediatePositions.Count; index++)
        {
            var position = intermediatePositions[index];
            if (position > start && position < end && !positions.Contains(position))
            {
                positions.Add(position);
            }
        }

        positions.Add(end);
        positions.Sort();
        return positions;
    }

    private static void AddTickSide(
        ICollection<RectangleF> target,
        IEnumerable<int> positions,
        Rectangle channelBounds,
        Orientation orientation,
        bool topOrLeft,
        float length,
        float thickness,
        float gap)
    {
        foreach (var position in positions)
        {
            if (orientation == Orientation.Horizontal)
            {
                var y = topOrLeft
                    ? channelBounds.Top - gap - length
                    : channelBounds.Bottom + gap;
                target.Add(new RectangleF(position - (thickness / 2f), y, thickness, length));
            }
            else
            {
                var x = topOrLeft
                    ? channelBounds.Left - gap - length
                    : channelBounds.Right + gap;
                target.Add(new RectangleF(x, position - (thickness / 2f), length, thickness));
            }
        }
    }
}
