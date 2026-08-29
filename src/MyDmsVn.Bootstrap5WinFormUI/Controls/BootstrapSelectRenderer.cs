using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides the framework default BootstrapSelect renderer.</summary>
public sealed class BootstrapSelectRenderer : IBootstrapSelectRenderer
{
    private readonly IIconRenderer _iconRenderer = BootstrapIconRenderer.CreateDefault();

    /// <inheritdoc />
    public void DrawResult(Graphics graphics, BootstrapSelectResultRenderContext context)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (context is null) throw new ArgumentNullException(nameof(context));

        var colors = context.Theme.Colors;
        var background = ResolveBackground(colors.Surface, colors.Hover, colors.Active, context.State);
        using (var brush = new SolidBrush(background)) graphics.FillRectangle(brush, context.Bounds);

        var textColor = (context.State & BootstrapSelectRenderState.Disabled) != 0 ? colors.MutedText : colors.Text;
        var textBounds = Rectangle.Inflate(context.Bounds, -DpiScaler.Scale(8, context.Dpi), 0);
        if (context.Item.Icon is not null)
        {
            var iconSize = Math.Max(12, DpiScaler.Scale(16, context.Dpi));
            var iconBounds = new Rectangle(textBounds.Left, textBounds.Top + Math.Max(0, (textBounds.Height - iconSize) / 2), iconSize, iconSize);
            _iconRenderer.TryRender(graphics, context.Item.Icon, iconBounds, textColor);
            textBounds.X += iconSize + DpiScaler.Scale(6, context.Dpi);
            textBounds.Width = Math.Max(0, textBounds.Width - iconSize - DpiScaler.Scale(6, context.Dpi));
        }

        TextRenderer.DrawText(graphics, context.Item.Text, context.Font, textBounds, textColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    /// <inheritdoc />
    public void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (context is null) throw new ArgumentNullException(nameof(context));
        using (var brush = new SolidBrush(context.Theme.Colors.SurfaceSecondary)) graphics.FillRectangle(brush, context.Bounds);
        var bounds = Rectangle.Inflate(context.Bounds, -DpiScaler.Scale(8, context.Dpi), 0);
        using var font = new Font(context.Font, FontStyle.Bold);
        TextRenderer.DrawText(graphics, context.Group, font, bounds, context.Theme.Colors.MutedText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    /// <inheritdoc />
    public void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (context is null) throw new ArgumentNullException(nameof(context));
        var color = context.IsPlaceholder ? context.Theme.Colors.MutedText : context.Theme.Colors.Text;
        TextRenderer.DrawText(graphics, context.Text, context.Font, context.Bounds, color,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    /// <inheritdoc />
    public void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (context is null) throw new ArgumentNullException(nameof(context));
        var colors = context.Theme.Colors;
        using (var brush = new SolidBrush((context.State & BootstrapSelectRenderState.Hot) != 0 ? colors.Active : colors.SurfaceSecondary))
        using (var path = RoundedPath.Create(context.Bounds, new CornerRadius(DpiScaler.Scale(4, context.Dpi))))
        {
            graphics.FillPath(brush, path);
        }

        var textBounds = context.Bounds;
        textBounds.X += DpiScaler.Scale(8, context.Dpi);
        textBounds.Width = Math.Max(0, context.RemoveBounds.Left - textBounds.Left - DpiScaler.Scale(4, context.Dpi));
        TextRenderer.DrawText(graphics, context.Item.Text, context.Font, textBounds, colors.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(graphics, "×", context.Font, context.RemoveBounds, colors.MutedText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    private static Color ResolveBackground(Color normal, Color hot, Color selected, BootstrapSelectRenderState state)
    {
        if ((state & BootstrapSelectRenderState.Highlighted) != 0 || (state & BootstrapSelectRenderState.Selected) != 0) return selected;
        if ((state & BootstrapSelectRenderState.Hot) != 0) return hot;
        return normal;
    }
}
