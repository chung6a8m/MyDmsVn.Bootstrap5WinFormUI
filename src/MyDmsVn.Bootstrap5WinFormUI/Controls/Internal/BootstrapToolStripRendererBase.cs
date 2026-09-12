using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal abstract class BootstrapToolStripRendererBase : ToolStripRenderer
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;

    internal BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            _variant = value;
        }
    }

    internal static BootstrapToolStripSurfaceKind GetSurfaceKind(ToolStrip? toolStrip)
    {
        if (toolStrip is StatusStrip) return BootstrapToolStripSurfaceKind.StatusBar;
        if (toolStrip is MenuStrip) return BootstrapToolStripSurfaceKind.MenuBar;
        if (toolStrip is ToolStripDropDown) return BootstrapToolStripSurfaceKind.DropDown;
        return BootstrapToolStripSurfaceKind.ToolBar;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        PaintBounds(e.Graphics, e.AffectedBounds, ResolvePalette(e.ToolStrip, true, false, false, false).Background);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        var metrics = ResolveMetrics(e.ToolStrip);
        if (metrics.BorderWidth <= 0f || e.ToolStrip.Width <= 0 || e.ToolStrip.Height <= 0) return;
        using var pen = new Pen(BootstrapThemeManager.CurrentTheme.Colors.Border, metrics.BorderWidth);
        if (GetSurfaceKind(e.ToolStrip) == BootstrapToolStripSurfaceKind.StatusBar)
        {
            e.Graphics.DrawLine(pen, 0, metrics.BorderWidth / 2f, e.ToolStrip.Width, metrics.BorderWidth / 2f);
            return;
        }

        var inset = metrics.BorderWidth / 2f;
        e.Graphics.DrawRectangle(pen, inset, inset, Math.Max(0f, e.ToolStrip.Width - metrics.BorderWidth), Math.Max(0f, e.ToolStrip.Height - metrics.BorderWidth));
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        PaintBounds(e.Graphics, e.AffectedBounds, ResolveImageMarginColor(e.ToolStrip));
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) => PaintItemBackground(e);
    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e) => PaintItemBackground(e);
    protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e) => PaintItemBackground(e);

    protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is not ToolStripSplitButton splitButton)
        {
            base.OnRenderSplitButtonBackground(e);
            return;
        }

        PaintBounds(e.Graphics, splitButton.ButtonBounds, ResolvePalette(e.ToolStrip, splitButton.Enabled, splitButton.ButtonSelected, splitButton.ButtonPressed, false).Background);
        PaintBounds(e.Graphics, splitButton.DropDownButtonBounds, ResolvePalette(e.ToolStrip, splitButton.Enabled, splitButton.DropDownButtonSelected, splitButton.DropDownButtonPressed, false).Background);
        var metrics = ResolveMetrics(e.ToolStrip);
        var geometry = BootstrapToolStripRenderLogic.ResolveSplitButtonGeometry(splitButton.ButtonBounds, splitButton.DropDownButtonBounds, e.ToolStrip?.RightToLeft == RightToLeft.Yes, metrics.ArrowSize);
        var palette = ResolvePalette(e.ToolStrip, splitButton.Enabled, splitButton.Selected, splitButton.Pressed, false);
        using var dividerPen = new Pen(palette.Border, Math.Max(1f, metrics.BorderWidth));
        e.Graphics.DrawLine(dividerPen, geometry.DividerStart, geometry.DividerEnd);
        DrawArrow(e.Graphics, geometry.ArrowPoints, palette.Foreground);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = ResolvePalette(e.ToolStrip, e.Item.Enabled, e.Item.Selected, e.Item.Pressed, IsChecked(e.Item)).Foreground;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        if (e.Item is not ToolStripMenuItem { Checked: true } || e.ImageRectangle.Width <= 0 || e.ImageRectangle.Height <= 0)
        {
            base.OnRenderItemCheck(e);
            return;
        }

        var palette = ResolvePalette(e.ToolStrip, e.Item.Enabled, e.Item.Selected, e.Item.Pressed, true);
        var rect = e.ImageRectangle;
        var oldSmoothingMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var pen = new Pen(palette.Accent, Math.Max(1f, ResolveMetrics(e.ToolStrip).BorderWidth * 2f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            e.Graphics.DrawLines(pen, new[]
            {
                new PointF(rect.Left + (rect.Width * 0.22f), rect.Top + (rect.Height * 0.52f)),
                new PointF(rect.Left + (rect.Width * 0.43f), rect.Top + (rect.Height * 0.72f)),
                new PointF(rect.Left + (rect.Width * 0.80f), rect.Top + (rect.Height * 0.30f))
            });
        }
        finally { e.Graphics.SmoothingMode = oldSmoothingMode; }
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        if (e.ArrowRectangle.Width <= 0 || e.ArrowRectangle.Height <= 0) return;
        var owner = e.Item?.Owner;
        var palette = ResolvePalette(owner, e.Item?.Enabled ?? true, e.Item?.Selected ?? false, e.Item?.Pressed ?? false, e.Item is not null && IsChecked(e.Item));
        DrawArrow(e.Graphics, ResolveArrowGeometry(e.ArrowRectangle, e.Direction, owner), palette.Foreground);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var metrics = ResolveMetrics(e.ToolStrip);
        var line = BootstrapToolStripRenderLogic.ResolveSeparatorLine(new Rectangle(Point.Empty, e.Item.Size), metrics.SeparatorInset, e.Vertical);
        using var pen = new Pen(BootstrapThemeManager.CurrentTheme.Colors.Border, Math.Max(1f, metrics.BorderWidth));
        e.Graphics.DrawLine(pen, line.Start, line.End);
    }

    protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
    {
        var metrics = ResolveMetrics(e.ToolStrip);
        using var brush = new SolidBrush(BootstrapThemeManager.CurrentTheme.Colors.MutedText);
        var horizontalStrip = e.ToolStrip.LayoutStyle != ToolStripLayoutStyle.VerticalStackWithOverflow;
        for (var index = 0; index < 3; index++)
        {
            var x = horizontalStrip ? e.GripBounds.Left + (e.GripBounds.Width / 2) : e.GripBounds.Left + index * metrics.GripDotSize * 2;
            var y = horizontalStrip ? e.GripBounds.Top + index * metrics.GripDotSize * 2 : e.GripBounds.Top + (e.GripBounds.Height / 2);
            e.Graphics.FillEllipse(brush, x, y, metrics.GripDotSize, metrics.GripDotSize);
        }
    }

    protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
    {
        PaintItemBackground(e);
        var metrics = ResolveMetrics(e.ToolStrip);
        var center = new Point(e.Item.Width / 2, e.Item.Height / 2);
        using var pen = new Pen(ResolvePalette(e.ToolStrip, e.Item.Enabled, e.Item.Selected, e.Item.Pressed, false).Foreground, Math.Max(1f, metrics.BorderWidth));
        e.Graphics.DrawLines(pen, new[] { new Point(center.X - metrics.ArrowSize, center.Y - metrics.ArrowSize / 2), new Point(center.X, center.Y + metrics.ArrowSize / 2), new Point(center.X + metrics.ArrowSize, center.Y - metrics.ArrowSize / 2) });
    }

    protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip is not StatusStrip { SizingGrip: true }) return;
        var metrics = ResolveMetrics(e.ToolStrip);
        var rtl = e.ToolStrip.RightToLeft == RightToLeft.Yes;
        using var brush = new SolidBrush(BootstrapThemeManager.CurrentTheme.Colors.MutedText);
        foreach (var dot in BootstrapToolStripRenderLogic.ResolveSizingGripDots(e.ToolStrip.Size, metrics.GripDotSize, rtl))
        {
            e.Graphics.FillRectangle(brush, dot);
        }
    }

    protected override void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e)
    {
        PaintItemBackground(e);
        if (e.Item is not ToolStripStatusLabel label || label.BorderSides == ToolStripStatusLabelBorderSides.None) return;
        var bounds = new Rectangle(Point.Empty, label.Size);
        ControlPaint.DrawBorder3D(e.Graphics, bounds, label.BorderStyle, ResolveBorderSides(label.BorderSides));
    }

    private void PaintItemBackground(ToolStripItemRenderEventArgs e) => PaintBounds(e.Graphics, new Rectangle(Point.Empty, e.Item.Size), ResolvePalette(e.ToolStrip, e.Item.Enabled, e.Item.Selected, e.Item.Pressed, IsChecked(e.Item)).Background);

    protected virtual Color ResolveImageMarginColor(ToolStrip? owner) => BootstrapThemeManager.CurrentTheme.Colors.SurfaceSecondary;

    protected virtual BootstrapDropdownPalette ResolvePalette(ToolStrip? owner, bool enabled, bool selected, bool pressed, bool @checked) =>
        BootstrapToolStripRenderLogic.ResolvePalette(BootstrapThemeManager.CurrentTheme.Colors, Variant, GetSurfaceKind(owner), enabled, selected, pressed, @checked);

    protected virtual PointF[] ResolveArrowGeometry(Rectangle bounds, ArrowDirection direction, ToolStrip? owner) =>
        BootstrapToolStripRenderLogic.ResolveArrowPoints(bounds, direction, ResolveMetrics(owner).ArrowSize);

    private static BootstrapDropdownMetrics ResolveMetrics(ToolStrip? toolStrip) => BootstrapToolStripRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, toolStrip is not null && toolStrip.DeviceDpi > 0 ? toolStrip.DeviceDpi : DpiScaler.DefaultDpi);
    private static bool IsChecked(ToolStripItem item) => item is ToolStripButton { Checked: true } || item is ToolStripMenuItem { Checked: true };

    private static Border3DSide ResolveBorderSides(ToolStripStatusLabelBorderSides sides)
    {
        var result = (Border3DSide)0;
        if ((sides & ToolStripStatusLabelBorderSides.Left) != 0) result |= Border3DSide.Left;
        if ((sides & ToolStripStatusLabelBorderSides.Top) != 0) result |= Border3DSide.Top;
        if ((sides & ToolStripStatusLabelBorderSides.Right) != 0) result |= Border3DSide.Right;
        if ((sides & ToolStripStatusLabelBorderSides.Bottom) != 0) result |= Border3DSide.Bottom;
        return result;
    }

    private static void PaintBounds(Graphics graphics, Rectangle bounds, Color color)
    {
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, bounds);
    }

    private static void DrawArrow(Graphics graphics, PointF[] points, Color color)
    {
        var old = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try { using var brush = new SolidBrush(color); graphics.FillPolygon(brush, points); }
        finally { graphics.SmoothingMode = old; }
    }

}
