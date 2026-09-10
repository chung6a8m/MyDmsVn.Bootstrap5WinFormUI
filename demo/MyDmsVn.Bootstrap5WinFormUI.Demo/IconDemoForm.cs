using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class IconDemoForm : DemoFormBase
{
    private readonly Label _instructions = new Label();
    private readonly IconPreviewSurface _preview = new IconPreviewSurface();

    public IconDemoForm()
    {
        Text = "Icon Infrastructure";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 560);
        MinimumSize = new Size(680, 460);

        _instructions.Dock = DockStyle.Top;
        _instructions.Height = 72;
        _instructions.Padding = new Padding(12, 8, 12, 8);
        _instructions.TextAlign = ContentAlignment.MiddleLeft;
        _instructions.Text =
            "Source-neutral rendering: Segoe MDL2 Assets and framework vector glyphs use the same IconDescriptor/IIconRenderer path. " +
            "SVG is adapter-backed through ISvgIconRenderer, so the core library has no SVG package dependency.";

        _preview.Dock = DockStyle.Fill;
        _preview.AccessibleName = "Icon source preview";

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

    private sealed class IconPreviewSurface : Control
    {
        private static readonly PreviewItem[] Items =
        {
            new PreviewItem("MDL2 Home", "Segoe MDL2", IconDescriptor.SegoeMdl2('\uE80F')),
            new PreviewItem("MDL2 Settings", "Segoe MDL2", IconDescriptor.SegoeMdl2('\uE713')),
            new PreviewItem("Chevron Down", "Framework vector", IconDescriptor.Framework(FrameworkIconGlyph.ChevronDown)),
            new PreviewItem("Check", "Framework vector", IconDescriptor.Framework(FrameworkIconGlyph.Check)),
            new PreviewItem("Close", "Framework vector", IconDescriptor.Framework(FrameworkIconGlyph.Close)),
            new PreviewItem("Plus", "Framework vector", IconDescriptor.Framework(FrameworkIconGlyph.Plus))
        };

        private readonly IIconRenderer _renderer = BootstrapIconRenderer.CreateDefault();
        private BootstrapTheme _theme = BootstrapThemeManager.CurrentTheme;

        public IconPreviewSurface()
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

            var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
            var columnCount = ClientSize.Width >= DpiScaler.Scale(760, dpi) ? 3 : 2;
            var spacing = DpiScaler.Scale(12, dpi);
            var outer = DpiScaler.Scale(16, dpi);
            var availableWidth = Math.Max(1, ClientSize.Width - (outer * 2) - ((columnCount - 1) * spacing));
            var cardWidth = Math.Max(DpiScaler.Scale(140, dpi), availableWidth / columnCount);
            var cardHeight = DpiScaler.Scale(132, dpi);

            for (var index = 0; index < Items.Length; index++)
            {
                var column = index % columnCount;
                var row = index / columnCount;
                var bounds = new Rectangle(
                    outer + (column * (cardWidth + spacing)),
                    outer + (row * (cardHeight + spacing)),
                    cardWidth,
                    cardHeight);

                DrawItem(e.Graphics, Items[index], bounds, dpi);
            }
        }

        private void DrawItem(Graphics graphics, PreviewItem item, Rectangle bounds, int dpi)
        {
            using var path = RoundedPath.Create(
                bounds,
                new CornerRadius(DpiScaler.Scale(_theme.Metrics.Radius, dpi)));
            using var surfaceBrush = new SolidBrush(_theme.Colors.Surface);
            using var borderPen = new Pen(
                _theme.Colors.Border,
                Math.Max(1, DpiScaler.Scale(_theme.Metrics.BorderWidth, dpi)));
            graphics.FillPath(surfaceBrush, path);
            graphics.DrawPath(borderPen, path);

            using var titleFont = new Font(
                _theme.Typography.Body.FontFamilyName,
                _theme.Typography.Body.SizeInPoints,
                FontStyle.Bold);
            using var sourceFont = new Font(
                _theme.Typography.BodySmall.FontFamilyName,
                _theme.Typography.BodySmall.SizeInPoints,
                _theme.Typography.BodySmall.Style);
            var layout = IconPreviewItemLayout.Calculate(graphics, bounds, dpi, titleFont, sourceFont);
            var rendered = _renderer.TryRender(
                graphics,
                item.Descriptor,
                layout.IconBounds,
                _theme.Colors.Primary);

            TextRenderer.DrawText(
                graphics,
                item.Title,
                titleFont,
                layout.TitleBounds,
                _theme.Colors.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

            var sourceText = rendered ? item.Source : item.Source + " (unavailable)";
            TextRenderer.DrawText(
                graphics,
                sourceText,
                sourceFont,
                layout.SourceBounds,
                _theme.Colors.MutedText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }
    }

    private sealed class PreviewItem
    {
        public PreviewItem(string title, string source, IconDescriptor descriptor)
        {
            Title = title;
            Source = source;
            Descriptor = descriptor;
        }

        public string Title { get; }

        public string Source { get; }

        public IconDescriptor Descriptor { get; }
    }
}
