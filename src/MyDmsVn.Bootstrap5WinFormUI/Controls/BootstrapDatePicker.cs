using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired date input while delegating value, range, formatting,
/// checkbox, keyboard, and calendar behavior to one native <see cref="DateTimePicker"/>.
/// </summary>
[DefaultProperty(nameof(Value))]
[DefaultEvent(nameof(ValueChanged))]
public class BootstrapDatePicker : UserControl
{
    private readonly DateTimePicker _picker = new DateTimePicker();
    private BootstrapValidationState _validationState = BootstrapValidationState.None;
    private int _borderRadius = -1;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe native-backed Bootstrap date picker.
    /// </summary>
    public BootstrapDatePicker()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            true);

        BackColor = Color.Transparent;
        TabStop = true;
        AccessibleRole = AccessibleRole.DropList;
        AccessibleDescription = "Bootstrap-inspired date picker.";

        _picker.TabStop = false;
        _picker.Margin = Padding.Empty;
        _picker.ShowUpDown = false;
        _picker.ValueChanged += OnPickerValueChanged;
        _picker.GotFocus += OnPickerFocusChanged;
        _picker.LostFocus += OnPickerFocusChanged;
        _picker.KeyDown += OnPickerKeyDown;
        _picker.KeyPress += OnPickerKeyPress;
        _picker.KeyUp += OnPickerKeyUp;
        _picker.PreviewKeyDown += OnPickerPreviewKeyDown;
        Controls.Add(_picker);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyTheme();

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = EffectiveDpi;
        var metrics = BootstrapDatePickerRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
        var nativePreferredHeight = GetNativePreferredHeight();
        Size = new Size(
            DpiScaler.Scale(240, dpi),
            Math.Max(
                DpiScaler.Scale(theme.Metrics.ControlHeight, dpi),
                nativePreferredHeight + (metrics.ShellPadding * 2)));
        PerformLayout();
    }

    /// <summary>
    /// Occurs when the native picker reports an effective value change.
    /// </summary>
    [Category("Action")]
    [Description("Occurs when the native date/time value changes.")]
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Gets or sets the current native date/time value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the current native date/time value.")]
    public DateTime Value
    {
        get => _picker.Value;
        set => _picker.Value = value;
    }

    /// <summary>
    /// Gets or sets the minimum native date/time value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the minimum native date/time value.")]
    public DateTime MinDate
    {
        get => _picker.MinDate;
        set => _picker.MinDate = value;
    }

    /// <summary>
    /// Gets or sets the maximum native date/time value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the maximum native date/time value.")]
    public DateTime MaxDate
    {
        get => _picker.MaxDate;
        set => _picker.MaxDate = value;
    }

    /// <summary>
    /// Gets or sets the native date/time display format.
    /// </summary>
    [Category("Appearance")]
    [Description("Gets or sets the native date/time display format.")]
    [DefaultValue(DateTimePickerFormat.Long)]
    public DateTimePickerFormat Format
    {
        get => _picker.Format;
        set => _picker.Format = value;
    }

    /// <summary>
    /// Gets or sets the native custom format string used when <see cref="Format"/> is <see cref="DateTimePickerFormat.Custom"/>.
    /// </summary>
    [Category("Appearance")]
    [Description("Gets or sets the native custom date/time format string.")]
    [DefaultValue(null)]
    public string? CustomFormat
    {
        get => _picker.CustomFormat;
        set => _picker.CustomFormat = value;
    }

    /// <summary>
    /// Gets or sets whether the native picker displays its optional checkbox.
    /// </summary>
    [Category("Appearance")]
    [Description("Gets or sets whether the native date picker displays a checkbox.")]
    [DefaultValue(false)]
    public bool ShowCheckBox
    {
        get => _picker.ShowCheckBox;
        set => _picker.ShowCheckBox = value;
    }

    /// <summary>
    /// Gets or sets the native checkbox state when <see cref="ShowCheckBox"/> is enabled.
    /// </summary>
    [Category("Behavior")]
    [Description("Gets or sets the native date picker checkbox state.")]
    [DefaultValue(true)]
    public bool Checked
    {
        get => _picker.Checked;
        set => _picker.Checked = value;
    }

    /// <summary>
    /// Gets or sets the validation state used by the themed outer shell.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects neutral, valid, or invalid date-input validation presentation.")]
    [DefaultValue(BootstrapValidationState.None)]
    public BootstrapValidationState ValidationState
    {
        get => _validationState;
        set
        {
            BootstrapTextBoxRenderLogic.ValidateState(value);
            if (_validationState == value)
            {
                return;
            }

            _validationState = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets a uniform logical corner radius. Use -1 to select the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets a uniform logical corner radius, or -1 to use the current theme radius.")]
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
    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        FocusPicker();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        FocusPicker();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        ApplyTheme();
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        ApplyChildFont();
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        LayoutPicker();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapDatePickerRenderLogic.ResolveMetrics(theme.Metrics, EffectiveDpi, _borderRadius);
        var palette = BootstrapDatePickerRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            ContainsFocus,
            Enabled);
        var borderWidth = Math.Max(1f, ContainsFocus ? metrics.FocusBorderWidth : metrics.BorderWidth);
        var inset = borderWidth / 2f;
        var bounds = new RectangleF(
            inset,
            inset,
            Math.Max(0f, ClientSize.Width - borderWidth),
            Math.Max(0f, ClientSize.Height - borderWidth));

        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var graphics = e.Graphics;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(bounds, new CornerRadius(metrics.Radius));
            using var surfaceBrush = new SolidBrush(palette.Surface);
            using var borderPen = new Pen(palette.Border, borderWidth);
            graphics.FillPath(surfaceBrush, path);
            graphics.DrawPath(borderPen, path);
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothing;
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

            _picker.ValueChanged -= OnPickerValueChanged;
            _picker.GotFocus -= OnPickerFocusChanged;
            _picker.LostFocus -= OnPickerFocusChanged;
            _picker.KeyDown -= OnPickerKeyDown;
            _picker.KeyPress -= OnPickerKeyPress;
            _picker.KeyUp -= OnPickerKeyUp;
            _picker.PreviewKeyDown -= OnPickerPreviewKeyDown;
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private int EffectiveDpi => DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;

    private void OnPickerValueChanged(object? sender, EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    private void OnPickerFocusChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    private void OnPickerKeyDown(object? sender, KeyEventArgs e)
    {
        OnKeyDown(e);
    }

    private void OnPickerKeyPress(object? sender, KeyPressEventArgs e)
    {
        OnKeyPress(e);
    }

    private void OnPickerKeyUp(object? sender, KeyEventArgs e)
    {
        OnKeyUp(e);
    }

    private void OnPickerPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        OnPreviewKeyDown(e);
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

        ApplyTheme();
        PerformLayout();
        Invalidate();
    }

    private void ApplyTheme()
    {
        var palette = BootstrapDatePickerRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme.Colors,
            _validationState,
            ContainsFocus,
            Enabled);

        _picker.BackColor = palette.Surface;
        _picker.ForeColor = palette.Foreground;
        ForeColor = palette.Foreground;
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
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
        ApplyChildFont();
    }

    private void ApplyChildFont()
    {
        _picker.Font = Font;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private void FocusPicker()
    {
        if (Enabled && !_picker.Focused)
        {
            _picker.Focus();
        }
    }

    private void LayoutPicker()
    {
        if (IsDisposed)
        {
            return;
        }

        var metrics = BootstrapDatePickerRenderLogic.ResolveMetrics(
            BootstrapThemeManager.CurrentTheme.Metrics,
            EffectiveDpi,
            _borderRadius);
        _picker.Bounds = BootstrapDatePickerRenderLogic.CalculateNativeBounds(
            ClientSize,
            GetNativePreferredHeight(),
            metrics);
    }

    private int GetNativePreferredHeight()
    {
        var preferredHeight = _picker.PreferredSize.Height;
        if (preferredHeight <= 0)
        {
            preferredHeight = _picker.Height;
        }

        return Math.Max(1, preferredHeight);
    }
}
