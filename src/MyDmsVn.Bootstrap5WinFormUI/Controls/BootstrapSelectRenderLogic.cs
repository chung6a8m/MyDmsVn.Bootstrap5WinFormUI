using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapSelectRenderMetrics
{
    public BootstrapSelectRenderMetrics(
        float borderWidth,
        RectangleF borderBounds,
        float radius)
    {
        BorderWidth = borderWidth;
        BorderBounds = borderBounds;
        Radius = radius;
    }

    public float BorderWidth { get; }

    public RectangleF BorderBounds { get; }

    public float Radius { get; }
}

internal static class BootstrapSelectRenderLogic
{
    public static BootstrapSelectRenderMetrics ResolveMetrics(
        Size clientSize,
        BootstrapThemeMetrics metrics,
        int dpi,
        int borderRadius,
        bool containsFocus)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi));
        }

        if (borderRadius < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(borderRadius));
        }

        var logicalBorderWidth = containsFocus
            ? metrics.FocusBorderWidth
            : metrics.BorderWidth;
        var borderWidth = Math.Max(
            1f,
            DpiScaler.Scale((float)logicalBorderWidth, dpi));
        var inset = borderWidth / 2f;
        var logicalRadius = borderRadius >= 0
            ? borderRadius
            : metrics.Radius;

        return new BootstrapSelectRenderMetrics(
            borderWidth,
            new RectangleF(
                inset,
                inset,
                Math.Max(0f, clientSize.Width - borderWidth),
                Math.Max(0f, clientSize.Height - borderWidth)),
            DpiScaler.Scale((float)logicalRadius, dpi));
    }
}
