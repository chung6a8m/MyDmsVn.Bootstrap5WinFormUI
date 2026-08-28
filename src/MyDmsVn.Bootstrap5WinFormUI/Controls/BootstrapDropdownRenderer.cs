using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    public BootstrapDropdownMetrics(
        int itemHorizontalPadding,
        int itemVerticalPadding,
        int imageSize,
        int separatorInset,
        float borderWidth)
    {
        ItemHorizontalPadding = itemHorizontalPadding;
        ItemVerticalPadding = itemVerticalPadding;
        ImageSize = imageSize;
        SeparatorInset = separatorInset;
        BorderWidth = borderWidth;
    }

    public int ItemHorizontalPadding { get; }
    public int ItemVerticalPadding { get; }
    public int ImageSize { get; }
    public int SeparatorInset { get; }
    public float BorderWidth { get; }
}

internal sealed class BootstrapDropdownRenderer : ToolStripRenderer
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;

    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            _variant = value;
        }
    }

    internal static BootstrapDropdownPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool enabled,
        bool selected)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        var variantColor = BootstrapVariantColorResolver.Resolve(colors, variant);
        return new BootstrapDropdownPalette(
            selected ? ColorUtil.Blend(variantColor, colors.Surface, 0.12f) : colors.Surface,
            enabled ? colors.Text : colors.MutedText,
            colors.Border,
            enabled ? variantColor : colors.Disabled);
    }

    internal static BootstrapDropdownMetrics ResolveMetrics(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        return new BootstrapDropdownMetrics(
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.SpacingXS, dpi),
            DpiScaler.Scale(metrics.SpacingLG, dpi),
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale((float)metrics.BorderWidth, dpi));
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        using var brush = new SolidBrush(colors.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = ResolveMetrics(theme.Metrics, GetDpi(e.ToolStrip));
        if (metrics.BorderWidth <= 0f || e.ToolStrip.Width <= 0 || e.ToolStrip.Height <= 0)
        {
            return;
        }

        var inset = metrics.BorderWidth / 2f;
        var bounds = new RectangleF(
            inset,
            inset,
            Math.Max(0f, e.ToolStrip.Width - metrics.BorderWidth),
            Math.Max(0f, e.ToolStrip.Height - metrics.BorderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        using var pen = new Pen(theme.Colors.Border, metrics.BorderWidth);
        e.Graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(BootstrapThemeManager.CurrentTheme.Colors.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var palette = ResolvePalette(theme.Colors, _variant, e.Item.Enabled, e.Item.Selected);
        using var brush = new SolidBrush(palette.Background);
        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var palette = ResolvePalette(theme.Colors, _variant, e.Item.Enabled, e.Item.Selected);
        e.TextColor = palette.Foreground;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        if (e.Item is not ToolStripMenuItem { Checked: true })
        {
            base.OnRenderItemCheck(e);
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var palette = ResolvePalette(theme.Colors, _variant, e.Item.Enabled, e.Item.Selected);
        var rect = e.ImageRectangle;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var oldSmoothingMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            var penWidth = Math.Max(1f, ResolveMetrics(theme.Metrics, GetDpi(e.ToolStrip)).BorderWidth * 2f);
            using var pen = new Pen(palette.Accent, penWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            var x1 = rect.Left + (rect.Width * 0.22f);
            var y1 = rect.Top + (rect.Height * 0.52f);
            var x2 = rect.Left + (rect.Width * 0.43f);
            var y2 = rect.Top + (rect.Height * 0.72f);
            var x3 = rect.Left + (rect.Width * 0.80f);
            var y3 = rect.Top + (rect.Height * 0.30f);
            e.Graphics.DrawLines(pen, new[]
            {
                new PointF(x1, y1),
                new PointF(x2, y2),
                new PointF(x3, y3)
            });
        }
        finally
        {
            e.Graphics.SmoothingMode = oldSmoothingMode;
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = ResolveMetrics(theme.Metrics, GetDpi(e.ToolStrip));
        var y = e.Item.Height / 2f;
        var right = Math.Max(metrics.SeparatorInset, e.Item.Width - metrics.SeparatorInset);
        using var pen = new Pen(theme.Colors.Border, Math.Max(1f, metrics.BorderWidth));
        e.Graphics.DrawLine(pen, metrics.SeparatorInset, y, right, y);
    }

    private static int GetDpi(ToolStrip? toolStrip)
    {
        return toolStrip is not null && toolStrip.DeviceDpi > 0
            ? toolStrip.DeviceDpi
            : DpiScaler.DefaultDpi;
    }
}
