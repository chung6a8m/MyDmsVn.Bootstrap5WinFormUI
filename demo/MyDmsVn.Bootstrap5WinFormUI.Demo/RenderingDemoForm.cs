using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class RenderingDemoForm : Form
{
    private readonly RenderingPreviewSurface _preview = new RenderingPreviewSurface();
    private readonly Label _instructions = new Label();

    public RenderingDemoForm()
    {
        Text = "Rendering and DPI Foundation";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 640);
        MinimumSize = new Size(760, 560);

        _instructions.Dock = DockStyle.Top;
        _instructions.Height = 58;
        _instructions.Padding = new Padding(12, 8, 12, 8);
        _instructions.TextAlign = ContentAlignment.MiddleLeft;
        _instructions.Text =
            "Virtual DPI matrix: 100%, 125%, 150%, 175%, 200%. " +
            "Check radius quality, border thickness, contrast, and icon/text-style alignment. " +
            "This preview complements, not replaces, live Windows DPI checks.";

        _preview.Dock = DockStyle.Fill;
        _preview.AccessibleName = "Rendering and DPI preview";

        Controls.Add(_preview);
        Controls.Add(_instructions);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _instructions.BackColor = theme.Colors.SurfaceSecondary;
        _instructions.ForeColor = theme.Colors.Text;
        _preview.Theme = theme;
    }

    private sealed class RenderingPreviewSurface : Control
    {
        private static readonly int[] PreviewDpis = { 96, 120, 144, 168, 192 };
        private BootstrapTheme _theme = BootstrapThemeManager.CurrentTheme;

        public RenderingPreviewSurface()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            TabStop = false;
        }

        public BootstrapTheme Theme
        {
            get => _theme;
            set
            {
                _theme = value ?? throw new ArgumentNullException(nameof(value));
                BackColor = value.Colors.Body;
                ForeColor = value.Colors.Text;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(_theme.Colors.Body);

            var availableWidth = Math.Max(240, ClientSize.Width - 32);
            var y = 16;

            foreach (var dpi in PreviewDpis)
            {
                var logicalHeight = 64;
                var rowHeight = DpiScaler.Scale(logicalHeight, dpi);
                var radius = DpiScaler.Scale((float)_theme.Metrics.Radius, dpi);
                var borderWidth = Math.Max(1f, DpiScaler.Scale((float)_theme.Metrics.BorderWidth, dpi));
                var rowBounds = new RectangleF(16f, y, availableWidth, rowHeight);

                DrawPreviewRow(e.Graphics, rowBounds, dpi, radius, borderWidth);
                y += rowHeight + 12;
            }
        }

        private void DrawPreviewRow(
            Graphics graphics,
            RectangleF bounds,
            int dpi,
            float radius,
            float borderWidth)
        {
            using var path = RoundedPath.Create(bounds, new CornerRadius(radius, radius * 0.5f, radius, radius * 0.5f));
            using var surfaceBrush = new SolidBrush(_theme.Colors.Surface);
            using var borderPen = new Pen(_theme.Colors.Border, borderWidth);

            graphics.FillPath(surfaceBrush, path);
            graphics.DrawPath(borderPen, path);

            var scaleLabelWidth = DpiScaler.Scale(108, dpi);
            var swatchSize = DpiScaler.Scale(new Size(28, 28), dpi);
            var textSize = DpiScaler.Scale(new Size(180, 24), dpi);
            var inner = Rectangle.Round(bounds);
            var layout = ContentLayoutHelper.ArrangeHorizontal(
                inner,
                DpiScaler.Scale(new Padding(12), dpi),
                swatchSize,
                textSize,
                DpiScaler.Scale(8, dpi),
                ContentAlignment.MiddleLeft);

            var labelBounds = new Rectangle(
                layout.ContentBounds.Left,
                layout.ContentBounds.Top,
                Math.Min(scaleLabelWidth, Math.Max(0, inner.Right - layout.ContentBounds.Left)),
                layout.ContentBounds.Height);

            var primary = _theme.Colors.Primary;
            using var swatchBrush = new SolidBrush(primary);
            using var swatchPath = RoundedPath.Create(
                layout.LeadingBounds,
                new CornerRadius(DpiScaler.Scale(4f, dpi)));
            graphics.FillPath(swatchBrush, swatchPath);

            var textColor = ColorUtil.GetContrastingTextColor(primary, Color.White, Color.Black);
            var contrast = ColorUtil.GetContrastRatio(primary, textColor);
            var percent = (int)Math.Round(dpi * 100d / DpiScaler.DefaultDpi, MidpointRounding.AwayFromZero);
            var text = $"{percent}% / {dpi} DPI   radius {radius:0.#}   border {borderWidth:0.#}   contrast {contrast:0.00}:1";

            using var font = new Font(
                _theme.Typography.Body.FontFamilyName,
                _theme.Typography.Body.SizeInPoints,
                _theme.Typography.Body.Style);

            var textBounds = Rectangle.FromLTRB(
                layout.LeadingBounds.Right + DpiScaler.Scale(8, dpi),
                inner.Top,
                inner.Right - DpiScaler.Scale(12, dpi),
                inner.Bottom);

            TextRenderer.DrawText(
                graphics,
                text,
                font,
                textBounds,
                _theme.Colors.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

            if (!labelBounds.IsEmpty)
            {
                using var markerPen = new Pen(ColorUtil.Blend(_theme.Colors.Focus, _theme.Colors.Surface, 0.35f));
                graphics.DrawLine(
                    markerPen,
                    labelBounds.Left,
                    bounds.Bottom - borderWidth,
                    Math.Min(labelBounds.Right, inner.Right),
                    bounds.Bottom - borderWidth);
            }
        }
    }
}
