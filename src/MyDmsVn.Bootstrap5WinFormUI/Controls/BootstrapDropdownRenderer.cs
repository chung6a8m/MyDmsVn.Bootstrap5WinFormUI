using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapDropdownRenderer : BootstrapToolStripRendererBase
{
    internal static BootstrapDropdownPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool enabled,
        bool selected)
    {
        return BootstrapToolStripRenderLogic.ResolvePalette(
            colors,
            variant,
            BootstrapToolStripSurfaceKind.DropDown,
            enabled,
            selected,
            pressed: false,
            @checked: false);
    }

    internal static BootstrapDropdownMetrics ResolveMetrics(BootstrapThemeMetrics metrics, int dpi)
    {
        return BootstrapToolStripRenderLogic.ResolveMetrics(metrics, dpi);
    }

    protected override Color ResolveImageMarginColor(ToolStrip? owner) => BootstrapThemeManager.CurrentTheme.Colors.Surface;

    protected override BootstrapDropdownPalette ResolvePalette(ToolStrip? owner, bool enabled, bool selected, bool pressed, bool @checked) =>
        ResolvePalette(BootstrapThemeManager.CurrentTheme.Colors, Variant, enabled, selected);

    protected override PointF[] ResolveArrowGeometry(Rectangle bounds, ArrowDirection direction, ToolStrip? owner)
    {
        var centerX = bounds.Left + (bounds.Width / 2f);
        var centerY = bounds.Top + (bounds.Height / 2f);
        var halfWidth = Math.Max(2f, bounds.Width * 0.22f);
        var halfHeight = Math.Max(2f, bounds.Height * 0.28f);
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
}
