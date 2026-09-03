using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-inspired switch presentation while retaining native WinForms CheckBox state and input semantics.
/// </summary>
[DefaultProperty(nameof(Checked))]
[DefaultEvent(nameof(CheckedChanged))]
public class BootstrapSwitch : CheckBox
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private BootstrapValidationState _validationState;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;
    private bool _hot;
    private bool _pressed;

    /// <summary>Initializes a designer-safe Bootstrap-themed switch.</summary>
    public BootstrapSwitch()
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

    /// <summary>Gets or sets the validation accent applied to the track and label.</summary>
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
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.Switch, BootstrapThemeManager.CurrentTheme.Metrics, dpi);
        var textSize = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(Text, Font, Size.Empty, GetTextFlags());
        return BootstrapCheckableRenderLogic.GetPreferredSize(textSize, Padding, metrics, CheckAlign);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        if (UsesNativeFallback()) { base.OnPaint(e); return; }
        base.OnPaintBackground(e);
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.Switch, theme.Metrics, dpi);
        var palette = BootstrapCheckableRenderLogic.ResolvePalette(theme.Colors, _variant, _validationState, CheckState, Enabled);
        var rtl = RightToLeft == RightToLeft.Yes;
        var layout = BootstrapCheckableRenderLogic.GetLayout(ClientRectangle, Padding, metrics, CheckAlign, rtl);
        var track = layout.IndicatorBounds;
        if (track.Width > 0 && track.Height > 0)
        {
            var old = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            try
            {
                var rect = new RectangleF(track.X + metrics.BorderWidth / 2f, track.Y + metrics.BorderWidth / 2f, Math.Max(0, track.Width - metrics.BorderWidth), Math.Max(0, track.Height - metrics.BorderWidth));
                using var path = RoundedPath.Create(rect, new CornerRadius(Math.Min(metrics.Radius, rect.Height / 2f)));
                var fill = _pressed ? ColorUtil.Blend(palette.Fill, theme.Colors.Active, 0.25f) : _hot ? ColorUtil.Blend(palette.Fill, theme.Colors.Hover, 0.18f) : palette.Fill;
                using var trackBrush = new SolidBrush(fill);
                using var borderPen = new Pen(palette.Border, Math.Max(1, metrics.BorderWidth));
                e.Graphics.FillPath(trackBrush, path);
                e.Graphics.DrawPath(borderPen, path);

                var inset = Math.Max(1, DpiScaler.Scale(theme.Metrics.BorderWidth + 1, dpi));
                var thumb = BootstrapCheckableRenderLogic.GetSwitchThumbBounds(track, inset, CheckState, rtl);
                if (thumb.Width > 0 && thumb.Height > 0)
                {
                    var thumbColor = CheckState == CheckState.Unchecked ? theme.Colors.MutedText : palette.Glyph;
                    using var thumbBrush = new SolidBrush(thumbColor);
                    e.Graphics.FillEllipse(thumbBrush, thumb);
                    if (CheckState == CheckState.Indeterminate)
                    {
                        using var cuePen = new Pen(fill, Math.Max(1, metrics.BorderWidth));
                        e.Graphics.DrawLine(cuePen, thumb.Left + thumb.Width / 2, thumb.Top + thumb.Height / 3, thumb.Left + thumb.Width / 2, thumb.Bottom - thumb.Height / 3);
                    }
                }

                if (Focused && ShowFocusCues)
                {
                    var focusRect = new RectangleF(track.X + metrics.FocusWidth / 2f, track.Y + metrics.FocusWidth / 2f, Math.Max(0, track.Width - metrics.FocusWidth), Math.Max(0, track.Height - metrics.FocusWidth));
                    using var focusPath = RoundedPath.Create(focusRect, new CornerRadius(Math.Min(metrics.Radius, focusRect.Height / 2f)));
                    using var focusPen = new Pen(palette.Focus, Math.Max(1, metrics.FocusWidth));
                    e.Graphics.DrawPath(focusPen, focusPath);
                }
            }
            finally { e.Graphics.SmoothingMode = old; }
        }
        TextRenderer.DrawText(e.Graphics, Text ?? string.Empty, Font, layout.TextBounds, palette.Text, GetTextFlags());
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

    private TextFormatFlags GetTextFlags()
    {
        return BootstrapCheckableRenderLogic.GetTextFormatFlags(TextAlign, UseMnemonic, ShowKeyboardCues, AutoEllipsis, RightToLeft == RightToLeft.Yes);
    }

    private bool UsesNativeFallback() => BootstrapCheckableRenderLogic.ShouldUseNativeFallback(Appearance, FlatStyle, Image is not null, ImageList is not null, ImageIndex, ImageKey);
    private void ApplyPreferredSize() { if (AutoSize && !IsDisposed) { var size = GetPreferredSize(Size.Empty); if (Size != size) Size = size; } }
    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) { if (!IsDisposed) { if (_useThemeFont) ApplyThemeFont(); ApplyPreferredSize(); Invalidate(); } }
    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _settingThemeFont = true;
        try { Font = next; } finally { _settingThemeFont = false; }
        if (previous is not null && ReferenceEquals(Font, previous)) { next.Dispose(); return; }
        _themeFont = next;
        previous?.Dispose();
    }
    private void DisposeThemeFont() { var font = _themeFont; _themeFont = null; font?.Dispose(); }
    private void ClearTransientState() { _hot = false; _pressed = false; }
}
