using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired native WinForms tab control while preserving <see cref="TabPage"/> composition and selection behavior.
/// </summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
public class BootstrapTabControl : TabControl
{
    private BootstrapTabStyle _tabStyle = BootstrapTabStyle.Tabs;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _fill;
    private int _borderRadius = -1;
    private int _hoveredIndex = -1;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _updatingItemSize;
    private Size[] _sizedTabImageSizes = Array.Empty<Size>();
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe Bootstrap-inspired tab control using native WinForms tab pages and selection behavior.
    /// </summary>
    public BootstrapTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;

        DrawItem += OnDrawTabItem;
        ControlAdded += OnTabControlAdded;
        ControlRemoved += OnTabControlRemoved;
        FontChanged += OnTabFontChanged;
        SizeChanged += OnTabSizeChanged;
        DpiChangedAfterParent += OnTabDpiChangedAfterParent;
        MouseMove += OnTabMouseMove;
        MouseLeave += OnTabMouseLeave;
        GotFocus += OnTabFocusChanged;
        LostFocus += OnTabFocusChanged;
        EnabledChanged += OnTabEnabledChanged;
        SelectedIndexChanged += OnTabSelectedIndexChanged;

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyHeaderItemSize();
    }

    /// <summary>
    /// Gets or sets the Bootstrap-inspired visual treatment used for tab headers.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects Tabs, Pills, or Underline presentation for native tab headers.")]
    [DefaultValue(BootstrapTabStyle.Tabs)]
    public BootstrapTabStyle TabStyle
    {
        get => _tabStyle;
        set
        {
            BootstrapTabControlRenderLogic.ValidateStyle(value);
            if (_tabStyle == value)
            {
                return;
            }

            _tabStyle = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the semantic Bootstrap-inspired accent variant used by selected tab headers.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired accent color used by selected tab headers.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapTabControlRenderLogic.ValidateVariant(value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the fixed-width native tab headers share the available client width evenly.
    /// </summary>
    [Category("Layout")]
    [Description("Makes all fixed-width tab headers share the available client width evenly while retaining native overflow behavior.")]
    [DefaultValue(false)]
    public bool Fill
    {
        get => _fill;
        set
        {
            if (_fill == value)
            {
                return;
            }

            _fill = value;
            ApplyHeaderItemSize();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets a uniform logical corner radius for Tabs and Pills. Use -1 to select the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets a uniform logical tab-header corner radius, or -1 to use the current theme radius.")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or a non-negative value.");
            }

            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            DrawItem -= OnDrawTabItem;
            ControlAdded -= OnTabControlAdded;
            ControlRemoved -= OnTabControlRemoved;
            FontChanged -= OnTabFontChanged;
            SizeChanged -= OnTabSizeChanged;
            DpiChangedAfterParent -= OnTabDpiChangedAfterParent;
            MouseMove -= OnTabMouseMove;
            MouseLeave -= OnTabMouseLeave;
            GotFocus -= OnTabFocusChanged;
            LostFocus -= OnTabFocusChanged;
            EnabledChanged -= OnTabEnabledChanged;
            SelectedIndexChanged -= OnTabSelectedIndexChanged;

            foreach (TabPage page in TabPages)
            {
                DetachTabPage(page);
            }

            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void OnDrawTabItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= TabPages.Count)
        {
            return;
        }

        if (!_fill && NativeImageSizingChanged())
        {
            ApplyHeaderItemSize();
        }

        var page = TabPages[e.Index];
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, GetCurrentDpi(), _borderRadius);
        var image = ResolveTabImage(page);
        var textWidth = MeasureTabTextWidth(page.Text, metrics.Height);
        var imageSize = image?.Size ?? Size.Empty;
        var layout = BootstrapTabControlRenderLogic.CalculateLayout(
            e.Bounds,
            _tabStyle,
            metrics,
            textWidth,
            imageSize,
            image is not null);
        var enabled = Enabled && page.Enabled;
        var palette = BootstrapTabControlRenderLogic.ResolvePalette(
            theme.Colors,
            _variant,
            _tabStyle,
            e.Index == SelectedIndex,
            enabled,
            HotTrack && e.Index == _hoveredIndex);

        DrawHeaderSurface(e.Graphics, layout, metrics, palette, e.Index == SelectedIndex);

        if (image is not null && layout.ImageBounds.Width > 0 && layout.ImageBounds.Height > 0)
        {
            if (enabled)
            {
                e.Graphics.DrawImage(image, layout.ImageBounds);
            }
            else
            {
                ControlPaint.DrawImageDisabled(e.Graphics, image, layout.ImageBounds.X, layout.ImageBounds.Y, palette.Background);
            }
        }

        if (layout.TextBounds.Width > 0 && layout.TextBounds.Height > 0)
        {
            TextRenderer.DrawText(
                e.Graphics,
                page.Text ?? string.Empty,
                Font,
                layout.TextBounds,
                palette.Foreground,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis);
        }

        if (e.Index == SelectedIndex && Focused && ShowFocusCues && enabled)
        {
            DrawFocusIndicator(e.Graphics, layout.FocusBounds, metrics.FocusBorderWidth, palette.Focus);
        }
    }

    private void DrawHeaderSurface(
        Graphics graphics,
        BootstrapTabHeaderLayout layout,
        BootstrapTabHeaderMetrics metrics,
        BootstrapTabHeaderPalette palette,
        bool selected)
    {
        if (layout.SurfaceBounds.Width <= 0 || layout.SurfaceBounds.Height <= 0)
        {
            return;
        }

        using (var neutralBrush = new SolidBrush(BootstrapThemeManager.CurrentTheme.Colors.Surface))
        {
            graphics.FillRectangle(neutralBrush, layout.SurfaceBounds);
        }

        if (_tabStyle == BootstrapTabStyle.Underline)
        {
            using (var backgroundBrush = new SolidBrush(palette.Background))
            {
                graphics.FillRectangle(backgroundBrush, layout.SurfaceBounds);
            }

            if (selected && layout.UnderlineBounds.Width > 0 && layout.UnderlineBounds.Height > 0)
            {
                using var accentBrush = new SolidBrush(palette.Accent);
                graphics.FillRectangle(accentBrush, layout.UnderlineBounds);
            }

            return;
        }

        var borderWidth = Math.Max(0f, metrics.BorderWidth);
        var borderInset = borderWidth / 2f;
        var bounds = new RectangleF(
            layout.SurfaceBounds.X + borderInset,
            layout.SurfaceBounds.Y + borderInset,
            Math.Max(0f, layout.SurfaceBounds.Width - borderWidth),
            Math.Max(0f, layout.SurfaceBounds.Height - borderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var oldSmoothingMode = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(bounds, layout.CornerRadius);
            using var surfaceBrush = new SolidBrush(palette.Background);
            graphics.FillPath(surfaceBrush, path);

            var drawBorder = _tabStyle == BootstrapTabStyle.Tabs || selected;
            if (drawBorder && metrics.BorderWidth > 0)
            {
                using var borderPen = new Pen(palette.Border, borderWidth);
                graphics.DrawPath(borderPen, path);
            }
        }
        finally
        {
            graphics.SmoothingMode = oldSmoothingMode;
        }
    }

    private static void DrawFocusIndicator(Graphics graphics, Rectangle bounds, int width, Color color)
    {
        if (width <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var focusBounds = bounds;
        focusBounds.Width = Math.Max(0, focusBounds.Width - 1);
        focusBounds.Height = Math.Max(0, focusBounds.Height - 1);
        if (focusBounds.Width <= 0 || focusBounds.Height <= 0)
        {
            return;
        }

        using var pen = new Pen(color, width);
        graphics.DrawRectangle(pen, focusBounds);
    }

    private void OnTabControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is TabPage page)
        {
            AttachTabPage(page);
            ApplyHeaderItemSize();
            Invalidate();
        }
    }

    private void OnTabControlRemoved(object? sender, ControlEventArgs e)
    {
        if (e.Control is TabPage page)
        {
            DetachTabPage(page);
            ApplyHeaderItemSize();
            UpdateHoveredIndex(-1);
            Invalidate();
        }
    }

    private void AttachTabPage(TabPage page)
    {
        page.TextChanged -= OnTabPagePresentationChanged;
        page.EnabledChanged -= OnTabPagePresentationChanged;
        page.TextChanged += OnTabPagePresentationChanged;
        page.EnabledChanged += OnTabPagePresentationChanged;
    }

    private void DetachTabPage(TabPage page)
    {
        page.TextChanged -= OnTabPagePresentationChanged;
        page.EnabledChanged -= OnTabPagePresentationChanged;
    }

    private void OnTabPagePresentationChanged(object? sender, EventArgs e)
    {
        ApplyHeaderItemSize();
        Invalidate();
    }

    private void OnTabFontChanged(object? sender, EventArgs e)
    {
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        ApplyHeaderItemSize();
        Invalidate();
    }

    private void OnTabSizeChanged(object? sender, EventArgs e)
    {
        if (_fill)
        {
            ApplyHeaderItemSize();
        }

        Invalidate();
    }

    private void OnTabDpiChangedAfterParent(object? sender, EventArgs e)
    {
        ApplyHeaderItemSize();
        Invalidate();
    }

    private void OnTabMouseMove(object? sender, MouseEventArgs e)
    {
        UpdateHoveredIndex(HitTestTab(e.Location));
    }

    private void OnTabMouseLeave(object? sender, EventArgs e)
    {
        UpdateHoveredIndex(-1);
    }

    private void OnTabFocusChanged(object? sender, EventArgs e)
    {
        InvalidateSelectedHeader();
    }

    private void OnTabEnabledChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    private void OnTabSelectedIndexChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (_useThemeFont)
        {
            ApplyThemeFont();
        }

        ApplyHeaderItemSize();
        Invalidate();
    }

    private void ApplyHeaderItemSize()
    {
        if (_updatingItemSize || IsDisposed)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(theme.Metrics, GetCurrentDpi(), _borderRadius);
        var preferredWidths = new int[TabPages.Count];
        for (var index = 0; index < TabPages.Count; index++)
        {
            preferredWidths[index] = MeasurePreferredContentWidth(TabPages[index], metrics);
        }

        var width = BootstrapTabControlRenderLogic.CalculateUniformItemWidth(
            TabPages.Count,
            ClientSize.Width,
            preferredWidths,
            metrics,
            _fill);
        var next = new Size(Math.Max(1, width), Math.Max(1, metrics.Height));
        CaptureNativeImageSizing();
        if (ItemSize == next)
        {
            return;
        }

        _updatingItemSize = true;
        try
        {
            ItemSize = next;
        }
        finally
        {
            _updatingItemSize = false;
        }
    }

    private int MeasurePreferredContentWidth(TabPage page, BootstrapTabHeaderMetrics metrics)
    {
        var textWidth = MeasureTabTextWidth(page.Text, metrics.Height);
        var image = ResolveTabImage(page);
        if (image is null)
        {
            return textWidth;
        }

        var spacing = textWidth > 0 ? metrics.ContentSpacing : 0;
        return image.Width + spacing + textWidth;
    }

    private int MeasureTabTextWidth(string? text, int height)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return TextRenderer.MeasureText(
            text,
            Font,
            new Size(int.MaxValue, Math.Max(1, height)),
            TextFormatFlags.SingleLine).Width;
    }

    private Image? ResolveTabImage(TabPage page)
    {
        var images = ImageList?.Images;
        if (images is null || images.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(page.ImageKey) && images.ContainsKey(page.ImageKey))
        {
            return images[page.ImageKey];
        }

        var imageIndex = page.ImageIndex;
        return imageIndex >= 0 && imageIndex < images.Count ? images[imageIndex] : null;
    }

    private bool NativeImageSizingChanged()
    {
        if (_sizedTabImageSizes.Length != TabPages.Count)
        {
            return true;
        }

        for (var index = 0; index < TabPages.Count; index++)
        {
            var currentSize = ResolveTabImage(TabPages[index])?.Size ?? Size.Empty;
            if (currentSize != _sizedTabImageSizes[index])
            {
                return true;
            }
        }

        return false;
    }

    private void CaptureNativeImageSizing()
    {
        var sizes = new Size[TabPages.Count];
        for (var index = 0; index < TabPages.Count; index++)
        {
            sizes[index] = ResolveTabImage(TabPages[index])?.Size ?? Size.Empty;
        }

        _sizedTabImageSizes = sizes;
    }

    private int HitTestTab(Point point)
    {
        for (var index = 0; index < TabPages.Count; index++)
        {
            if (GetTabRect(index).Contains(point))
            {
                return index;
            }
        }

        return -1;
    }

    private void UpdateHoveredIndex(int value)
    {
        if (_hoveredIndex == value)
        {
            return;
        }

        var previous = _hoveredIndex;
        _hoveredIndex = value;
        InvalidateHeader(previous);
        InvalidateHeader(value);
    }

    private void InvalidateSelectedHeader()
    {
        InvalidateHeader(SelectedIndex);
    }

    private void InvalidateHeader(int index)
    {
        if (index < 0 || index >= TabPages.Count || !IsHandleCreated)
        {
            return;
        }

        Invalidate(GetTabRect(index));
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        if (ThemeFontMatches(token))
        {
            return;
        }

        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = nextFont;
        _settingThemeFont = true;
        try
        {
            Font = nextFont;
        }
        finally
        {
            _settingThemeFont = false;
        }

        previous?.Dispose();
    }

    private bool ThemeFontMatches(BootstrapFontToken token)
    {
        return _themeFont is not null &&
            string.Equals(_themeFont.Name, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(_themeFont.SizeInPoints - token.SizeInPoints) < 0.01f &&
            _themeFont.Style == token.Style;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private int GetCurrentDpi()
    {
        return DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
    }
}
