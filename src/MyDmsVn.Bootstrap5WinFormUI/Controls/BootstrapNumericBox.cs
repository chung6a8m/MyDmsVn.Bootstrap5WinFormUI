using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired numeric input while delegating numeric editing, formatting, range, and spin behavior to a native <see cref="NumericUpDown"/>.
/// </summary>
[DefaultProperty(nameof(Value))]
[DefaultEvent(nameof(ValueChanged))]
public class BootstrapNumericBox : UserControl
{
    private readonly NumericUpDown _editor = new NumericUpDown();
    private BootstrapValidationState _validationState = BootstrapValidationState.None;
    private int _borderRadius = -1;
    private Font? _themeFont;
    private bool _useThemeFont = true;
    private bool _settingThemeFont;
    private bool _themeSubscribed;

    /// <summary>
    /// Initializes a designer-safe native-backed numeric input.
    /// </summary>
    public BootstrapNumericBox()
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
        AccessibleRole = AccessibleRole.SpinButton;
        AccessibleDescription = "Bootstrap-inspired numeric input.";

        _editor.BorderStyle = BorderStyle.None;
        _editor.TabStop = false;
        _editor.Margin = Padding.Empty;
        _editor.ValueChanged += OnEditorValueChanged;
        _editor.KeyDown += OnEditorKeyDown;
        _editor.KeyPress += OnEditorKeyPress;
        _editor.KeyUp += OnEditorKeyUp;
        _editor.PreviewKeyDown += OnEditorPreviewKeyDown;
        _editor.GotFocus += OnEditorFocusChanged;
        _editor.LostFocus += OnEditorFocusChanged;

        Controls.Add(_editor);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyTheme();

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapNumericBoxRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
        Size = new Size(
            _editor.Width + (metrics.HorizontalPadding * 2),
            DpiScaler.Scale(theme.Metrics.ControlHeight, dpi));
        PerformLayout();
    }

    /// <summary>
    /// Occurs when the native numeric value changes.
    /// </summary>
    [Category("Action")]
    [Description("Occurs when the native numeric value changes.")]
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Gets or sets the current native numeric value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the current native numeric value.")]
    [DefaultValue(typeof(decimal), "0")]
    public decimal Value
    {
        get => _editor.Value;
        set => _editor.Value = value;
    }

    /// <summary>
    /// Gets or sets the minimum native numeric value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the minimum native numeric value.")]
    [DefaultValue(typeof(decimal), "0")]
    public decimal Minimum
    {
        get => _editor.Minimum;
        set => _editor.Minimum = value;
    }

    /// <summary>
    /// Gets or sets the maximum native numeric value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the maximum native numeric value.")]
    [DefaultValue(typeof(decimal), "100")]
    public decimal Maximum
    {
        get => _editor.Maximum;
        set => _editor.Maximum = value;
    }

    /// <summary>
    /// Gets or sets the amount by which native spin operations change the value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the native numeric increment.")]
    [DefaultValue(typeof(decimal), "1")]
    public decimal Increment
    {
        get => _editor.Increment;
        set => _editor.Increment = value;
    }

    /// <summary>
    /// Gets or sets the number of decimal places displayed by the native editor.
    /// </summary>
    [Category("Appearance")]
    [Description("Gets or sets the number of decimal places displayed by the native numeric editor.")]
    [DefaultValue(0)]
    public int DecimalPlaces
    {
        get => _editor.DecimalPlaces;
        set => _editor.DecimalPlaces = value;
    }

    /// <summary>
    /// Gets or sets whether the native editor displays a thousands separator when appropriate.
    /// </summary>
    [Category("Appearance")]
    [Description("Gets or sets whether the native numeric editor uses a thousands separator.")]
    [DefaultValue(false)]
    public bool ThousandsSeparator
    {
        get => _editor.ThousandsSeparator;
        set => _editor.ThousandsSeparator = value;
    }

    /// <summary>
    /// Gets or sets whether typed editing is read-only while native spin behavior remains available.
    /// </summary>
    [Category("Behavior")]
    [Description("Prevents typed numeric editing while retaining native spin behavior.")]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => _editor.ReadOnly;
        set
        {
            if (_editor.ReadOnly == value)
            {
                return;
            }

            _editor.ReadOnly = value;
            ApplyTheme();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the validation state used by the themed numeric shell.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects neutral, valid, or invalid numeric-input validation presentation.")]
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
            ApplyTheme();
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
            PerformLayout();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        FocusEditor();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        FocusEditor();
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
        LayoutEditor();
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
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapNumericBoxRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
        var palette = BootstrapNumericBoxRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            ContainsFocus,
            Enabled,
            ReadOnly);
        var borderWidth = Math.Max(
            1f,
            ContainsFocus ? metrics.FocusBorderWidth : metrics.BorderWidth);
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
            using var surfaceBrush = new SolidBrush(palette.Background);
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

            _editor.ValueChanged -= OnEditorValueChanged;
            _editor.KeyDown -= OnEditorKeyDown;
            _editor.KeyPress -= OnEditorKeyPress;
            _editor.KeyUp -= OnEditorKeyUp;
            _editor.PreviewKeyDown -= OnEditorPreviewKeyDown;
            _editor.GotFocus -= OnEditorFocusChanged;
            _editor.LostFocus -= OnEditorFocusChanged;
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void OnEditorValueChanged(object? sender, EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        OnKeyDown(e);
    }

    private void OnEditorKeyPress(object? sender, KeyPressEventArgs e)
    {
        OnKeyPress(e);
    }

    private void OnEditorKeyUp(object? sender, KeyEventArgs e)
    {
        OnKeyUp(e);
    }

    private void OnEditorPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        OnPreviewKeyDown(e);
    }

    private void OnEditorFocusChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
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

        ApplyTheme();
        PerformLayout();
        Invalidate();
    }

    private void ApplyTheme()
    {
        var palette = BootstrapNumericBoxRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme.Colors,
            _validationState,
            ContainsFocus,
            Enabled,
            ReadOnly);

        _editor.BackColor = palette.Background;
        _editor.ForeColor = palette.Foreground;
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
        _editor.Font = Font;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private void FocusEditor()
    {
        if (Enabled && !_editor.Focused)
        {
            _editor.Focus();
        }
    }

    private void LayoutEditor()
    {
        var nativePreferredHeight = Math.Max(1, _editor.PreferredHeight);
        if (ClientSize.Height < nativePreferredHeight)
        {
            Height = nativePreferredHeight;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapNumericBoxRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
        _editor.Bounds = BootstrapNumericBoxRenderLogic.CalculateNativeBounds(
            ClientSize,
            nativePreferredHeight,
            metrics);
    }
}
