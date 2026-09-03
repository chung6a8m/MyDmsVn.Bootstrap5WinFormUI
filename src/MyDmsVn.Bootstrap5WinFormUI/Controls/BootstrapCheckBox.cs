using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-inspired CheckBox presentation while retaining native WinForms checked-state and input semantics.
/// </summary>
[DefaultProperty(nameof(Checked))]
[DefaultEvent(nameof(CheckedChanged))]
public class BootstrapCheckBox : CheckBox
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private BootstrapValidationState _validationState;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;
    private bool _hot;
    private bool _pressed;

    /// <summary>
    /// Initializes a designer-safe Bootstrap-themed CheckBox.
    /// </summary>
    public BootstrapCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        AutoSize = true;
        BackColor = Color.Transparent;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyPreferredSize();
    }

    /// <summary>Gets or sets the enabled semantic accent used for checked presentation.</summary>
    [Category("Appearance")]
    [Description("Selects the semantic accent used for checked presentation.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            if (_variant == value) return;
            _variant = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the validation accent applied to the indicator and label.</summary>
    [Category("Appearance")]
    [Description("Applies no validation accent, a success accent, or a danger accent.")]
    [DefaultValue(BootstrapValidationState.None)]
    public BootstrapValidationState ValidationState
    {
        get => _validationState;
        set
        {
            BootstrapTextBoxRenderLogic.ValidateState(value);
            if (_validationState == value) return;
            _validationState = value;
            Invalidate();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        if (UsesNativeFallback()) return base.GetPreferredSize(proposedSize);
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, BootstrapThemeManager.CurrentTheme.Metrics, dpi);
        var textSize = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(Text, Font, Size.Empty, GetTextFlags() | TextFormatFlags.NoPadding);
        return BootstrapCheckableRenderLogic.GetPreferredSize(textSize, Padding, metrics);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        if (UsesNativeFallback())
        {
            base.OnPaint(e);
            return;
        }

        PaintBootstrap(e.Graphics);
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); ApplyPreferredSize(); Invalidate(); }
    /// <inheritdoc />
    protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); ApplyPreferredSize(); Invalidate(); }
    /// <inheritdoc />
    protected override void OnAutoSizeChanged(EventArgs e) { base.OnAutoSizeChanged(e); ApplyPreferredSize(); }
    /// <inheritdoc />
    protected override void OnCheckStateChanged(EventArgs e) { base.OnCheckStateChanged(e); Invalidate(); }
    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); if (!Enabled) ClearTransientState(); Invalidate(); }
    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); ClearTransientState(); Invalidate(); }
    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs eventargs) { base.OnMouseEnter(eventargs); _hot = true; Invalidate(); }
    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs eventargs) { base.OnMouseLeave(eventargs); _hot = false; Invalidate(); }
    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs mevent) { base.OnMouseDown(mevent); if (mevent.Button == MouseButtons.Left) _pressed = true; Invalidate(); }
    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs mevent) { base.OnMouseUp(mevent); _pressed = false; Invalidate(); }
    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); _pressed = false; Invalidate(); }
    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (!Visible) ClearTransientState(); }
    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); ApplyPreferredSize(); Invalidate(); }
    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_settingThemeFont) { _useThemeFont = false; DisposeThemeFont(); }
        ApplyPreferredSize();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed) { BootstrapThemeManager.ThemeChanged -= OnThemeChanged; _themeSubscribed = false; }
            DisposeThemeFont();
        }
        base.Dispose(disposing);
    }

    private void PaintBootstrap(Graphics graphics)
    {
        base.OnPaintBackground(new PaintEventArgs(graphics, ClientRectangle));
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, theme.Metrics, dpi);
        var palette = BootstrapCheckableRenderLogic.ResolvePalette(theme.Colors, _variant, _validationState, CheckState, Enabled);
        var layout = BootstrapCheckableRenderLogic.GetLayout(ClientRectangle, Padding, metrics, CheckAlign, RightToLeft == RightToLeft.Yes);
        var indicator = layout.IndicatorBounds;
        if (indicator.Width > 0 && indicator.Height > 0)
        {
            var old = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            try
            {
                var rect = new RectangleF(indicator.X + metrics.BorderWidth / 2f, indicator.Y + metrics.BorderWidth / 2f, Math.Max(0, indicator.Width - metrics.BorderWidth), Math.Max(0, indicator.Height - metrics.BorderWidth));
                using var path = RoundedPath.Create(rect, new CornerRadius(Math.Min(metrics.Radius, Math.Min(rect.Width, rect.Height) / 2f)));
                var fill = _pressed ? ColorUtil.Blend(palette.Fill, theme.Colors.Active, 0.25f) : _hot ? ColorUtil.Blend(palette.Fill, theme.Colors.Hover, 0.18f) : palette.Fill;
                using var brush = new SolidBrush(fill);
                using var pen = new Pen(palette.Border, Math.Max(1, metrics.BorderWidth));
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
                if (CheckState == CheckState.Checked) DrawCheckMark(graphics, indicator, palette.Glyph, metrics.BorderWidth);
                else if (CheckState == CheckState.Indeterminate) DrawMixedMark(graphics, indicator, palette.Glyph);
                if (Focused && ShowFocusCues) DrawFocus(graphics, indicator, palette.Focus, metrics.FocusWidth, metrics.Radius);
            }
            finally { graphics.SmoothingMode = old; }
        }

        TextRenderer.DrawText(graphics, Text ?? string.Empty, Font, layout.TextBounds, palette.Text, GetTextFlags());
    }

    private static void DrawCheckMark(Graphics graphics, Rectangle bounds, Color color, int width)
    {
        using var pen = new Pen(color, Math.Max(2f, width * 1.6f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        graphics.DrawLines(pen, new[]
        {
            new PointF(bounds.Left + bounds.Width * 0.22f, bounds.Top + bounds.Height * 0.52f),
            new PointF(bounds.Left + bounds.Width * 0.43f, bounds.Top + bounds.Height * 0.72f),
            new PointF(bounds.Left + bounds.Width * 0.79f, bounds.Top + bounds.Height * 0.28f)
        });
    }

    private static void DrawMixedMark(Graphics graphics, Rectangle bounds, Color color)
    {
        var bar = new RectangleF(bounds.Left + bounds.Width * 0.23f, bounds.Top + bounds.Height * 0.43f, bounds.Width * 0.54f, Math.Max(2f, bounds.Height * 0.14f));
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, bar);
    }

    private static void DrawFocus(Graphics graphics, Rectangle bounds, Color color, int width, float radius)
    {
        var rect = new RectangleF(bounds.X + width / 2f, bounds.Y + width / 2f, Math.Max(0, bounds.Width - width), Math.Max(0, bounds.Height - width));
        using var path = RoundedPath.Create(rect, new CornerRadius(Math.Min(radius + width, Math.Min(rect.Width, rect.Height) / 2f)));
        using var pen = new Pen(color, Math.Max(1, width));
        graphics.DrawPath(pen, path);
    }

    private TextFormatFlags GetTextFlags()
    {
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping;
        if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
        else if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        if (AutoEllipsis) flags |= TextFormatFlags.EndEllipsis;
        if (RightToLeft == RightToLeft.Yes) flags |= TextFormatFlags.RightToLeft;
        switch (TextAlign)
        {
            case ContentAlignment.TopCenter:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.BottomCenter: flags |= TextFormatFlags.HorizontalCenter; break;
            case ContentAlignment.TopRight:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.BottomRight: flags |= TextFormatFlags.Right; break;
            default: flags |= TextFormatFlags.Left; break;
        }
        return flags;
    }

    private bool UsesNativeFallback()
    {
        return BootstrapCheckableRenderLogic.ShouldUseNativeFallback(Appearance, Image is not null, ImageList is not null, ImageIndex, ImageKey);
    }

    private void ApplyPreferredSize()
    {
        if (!AutoSize || IsDisposed) return;
        var size = GetPreferredSize(Size.Empty);
        if (Size != size) Size = size;
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed) return;
        if (_useThemeFont) ApplyThemeFont();
        ApplyPreferredSize();
        Invalidate();
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _settingThemeFont = true;
        try { Font = next; }
        finally { _settingThemeFont = false; }
        if (previous is not null && ReferenceEquals(Font, previous)) { next.Dispose(); return; }
        _themeFont = next;
        previous?.Dispose();
    }

    private void DisposeThemeFont() { var font = _themeFont; _themeFont = null; font?.Dispose(); }
    private void ClearTransientState() { _hot = false; _pressed = false; }
}
