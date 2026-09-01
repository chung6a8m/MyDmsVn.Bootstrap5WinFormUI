using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class BootstrapSelectProductRenderer : IBootstrapSelectRenderer
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly BootstrapSelectRenderer _defaultRenderer = new BootstrapSelectRenderer();

    public void DrawResult(Graphics graphics, BootstrapSelectResultRenderContext context)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (context.Item.Tag is not BootstrapSelectProduct product)
        {
            _defaultRenderer.DrawResult(graphics, context);
            return;
        }

        var colors = context.Theme.Colors;
        var background = (context.State & (BootstrapSelectRenderState.Highlighted | BootstrapSelectRenderState.Selected)) != 0
            ? colors.Active
            : (context.State & BootstrapSelectRenderState.Hot) != 0
                ? colors.Hover
                : colors.Surface;
        using (var backgroundBrush = new SolidBrush(background))
        {
            graphics.FillRectangle(backgroundBrush, context.Bounds);
        }

        using var secondaryFont = new Font(
            context.Font.FontFamily,
            Math.Max(6f, context.Font.Size * 0.82f),
            context.Font.Style,
            GraphicsUnit.Point);
        var layout = BootstrapSelectProductResultLayout.Calculate(
            graphics,
            context.Bounds,
            context.Dpi,
            context.Font,
            secondaryFont);
        var primaryColor = (context.State & BootstrapSelectRenderState.Disabled) != 0
            ? colors.MutedText
            : colors.Text;
        var details = string.Format(
            VietnameseCulture,
            "{0} · {1:N0} ₫ · Tồn {2:N0}",
            product.Unit,
            product.UnitPrice,
            product.StockQuantity);
        const TextFormatFlags drawFlags = TextFormatFlags.SingleLine
            | TextFormatFlags.EndEllipsis
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.NoPadding;
        TextRenderer.DrawText(graphics, product.Name, context.Font, layout.NameBounds, primaryColor, drawFlags);
        TextRenderer.DrawText(graphics, details, secondaryFont, layout.DetailsBounds, colors.MutedText, drawFlags);
    }

    public void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context)
    {
        _defaultRenderer.DrawGroupHeader(graphics, context);
    }

    public void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context)
    {
        _defaultRenderer.DrawSelection(graphics, context);
    }

    public void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context)
    {
        _defaultRenderer.DrawChip(graphics, context);
    }
}
