using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired presentation for the native WinForms <see cref="ComboBox"/> while preserving native item, binding, selection, editing, keyboard, auto-complete, and drop-down behavior.
/// </summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
public class BootstrapComboBox : ComboBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

    private BootstrapValidationState _validationState = BootstrapValidationState.None;
    private int _borderRadius = -1;
    private IconDescriptor? _leadingIcon;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe native-backed Bootstrap combo box.
    /// </summary>
    public BootstrapComboBox()
    {
        base.DrawMode = DrawMode.OwnerDrawFixed;
        FlatStyle = FlatStyle.Flat;
        IntegralHeight = true;

        FontChanged += OnComboBoxFontChanged;
        DpiChangedAfterParent += OnComboBoxDpiChangedAfterParent;
        EnabledChanged += OnComboBoxPresentationChanged;
        GotFocus += OnComboBoxPresentationChanged;
        LostFocus += OnComboBoxPresentationChanged;
        DropDown += OnComboBoxPresentationChanged;
        DropDownClosed += OnComboBoxPresentationChanged;

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;

        ApplyThemeFont();
        ApplyThemePresentation();
        ApplyOwnerDrawMetrics();
    }

    /// <summary>
    /// Gets or sets the validation presentation state without changing native selection or data-binding state.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects neutral, valid, or invalid border presentation for the combo box.")]
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
    /// Gets or sets the logical uniform shell radius, or -1 to use the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets a uniform logical combo-box shell radius, or -1 to use the current theme radius.")]
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

    /// <summary>
    /// Gets or sets an optional decorative icon shown in the owner-drawn closed selection area when WinForms exposes that drawing context.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies an optional source-neutral leading icon for the closed combo-box selection area.")]
    [DefaultValue(null)]
    public IconDescriptor? LeadingIcon
    {
        get => _leadingIcon;
        set
        {
            if (ReferenceEquals(_leadingIcon, value))
            {
                return;
            }

            _leadingIcon = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the source-neutral renderer used for <see cref="LeadingIcon"/>.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer
    {
        get => _iconRenderer;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (ReferenceEquals(_iconRenderer, value))
            {
                return;
            }

            _iconRenderer = value;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        DrawBootstrapItem(e);
        base.OnDrawItem(e);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (DrawMode != DrawMode.OwnerDrawFixed)
        {
            base.DrawMode = DrawMode.OwnerDrawFixed;
        }

        ApplyThemePresentation();
        ApplyOwnerDrawMetrics();
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if ((m.Msg == WmPaint || m.Msg == WmNcPaint) && IsHandleCreated && !IsDisposed)
        {
            DrawShellBorder();
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

            FontChanged -= OnComboBoxFontChanged;
            DpiChangedAfterParent -= OnComboBoxDpiChangedAfterParent;
            EnabledChanged -= OnComboBoxPresentationChanged;
            GotFocus -= OnComboBoxPresentationChanged;
            LostFocus -= OnComboBoxPresentationChanged;
            DropDown -= OnComboBoxPresentationChanged;
            DropDownClosed -= OnComboBoxPresentationChanged;

            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void DrawBootstrapItem(DrawItemEventArgs e)
    {
        if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = ResolveMetrics(theme);
        var palette = BootstrapComboBoxRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            Focused || ContainsFocus,
            Enabled);
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var comboBoxEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
        var background = selected ? palette.SelectedBackground : palette.Background;
        var foreground = selected ? palette.SelectedForeground : palette.Foreground;

        using (var backgroundBrush = new SolidBrush(background))
        {
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
        }

        var showLeadingIcon = comboBoxEdit && _leadingIcon is not null;
        var layout = BootstrapComboBoxRenderLogic.CalculateItemLayout(
            e.Bounds,
            metrics,
            showLeadingIcon,
            trailingReserve: 0);

        if (showLeadingIcon && layout.IconBounds.Width > 0 && layout.IconBounds.Height > 0)
        {
            _iconRenderer.TryRender(e.Graphics, _leadingIcon!, layout.IconBounds, foreground);
        }

        var text = ResolveItemText(e.Index);
        if (layout.TextBounds.Width > 0 && layout.TextBounds.Height > 0 && text.Length > 0)
        {
            TextRenderer.DrawText(
                e.Graphics,
                text,
                Font,
                layout.TextBounds,
                foreground,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }

        if (!comboBoxEdit && (e.State & DrawItemState.Focus) == DrawItemState.Focus && ShowFocusCues)
        {
            e.DrawFocusRectangle();
        }
    }

    private string ResolveItemText(int index)
    {
        if (index >= 0 && index < Items.Count)
        {
            return GetItemText(Items[index]) ?? string.Empty;
        }

        return Text ?? string.Empty;
    }

    private void DrawShellBorder()
    {
        var bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = ResolveMetrics(theme);
        var containsFocus = Focused || ContainsFocus;
        var palette = BootstrapComboBoxRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            containsFocus,
            Enabled);
        var borderWidth = containsFocus ? metrics.FocusBorderWidth : metrics.BorderWidth;
        if (borderWidth <= 0f)
        {
            return;
        }

        var inset = borderWidth / 2f;
        var borderBounds = new RectangleF(
            bounds.Left + inset,
            bounds.Top + inset,
            Math.Max(0f, bounds.Width - borderWidth),
            Math.Max(0f, bounds.Height - borderWidth));
        if (borderBounds.Width <= 0f || borderBounds.Height <= 0f)
        {
            return;
        }

        using var graphics = Graphics.FromHwnd(Handle);
        var oldSmoothingMode = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(borderBounds, new CornerRadius(metrics.Radius));
            using var pen = new Pen(palette.Border, borderWidth);
            graphics.DrawPath(pen, path);
        }
        finally
        {
            graphics.SmoothingMode = oldSmoothingMode;
        }
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

        ApplyThemePresentation();
        ApplyOwnerDrawMetrics();
        Invalidate();
    }

    private void OnComboBoxFontChanged(object? sender, EventArgs e)
    {
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        ApplyOwnerDrawMetrics();
        Invalidate();
    }

    private void OnComboBoxDpiChangedAfterParent(object? sender, EventArgs e)
    {
        ApplyOwnerDrawMetrics();
        Invalidate();
    }

    private void OnComboBoxPresentationChanged(object? sender, EventArgs e)
    {
        ApplyThemePresentation();
        Invalidate();
    }

    private void ApplyThemePresentation()
    {
        if (IsDisposed)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var palette = BootstrapComboBoxRenderLogic.ResolvePalette(
            theme.Colors,
            _validationState,
            Focused || ContainsFocus,
            Enabled);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
    }

    private void ApplyOwnerDrawMetrics()
    {
        if (IsDisposed)
        {
            return;
        }

        var metrics = ResolveMetrics(BootstrapThemeManager.CurrentTheme);
        var nextHeight = Math.Max(1, metrics.ItemHeight);
        if (ItemHeight != nextHeight)
        {
            ItemHeight = nextHeight;
        }
    }

    private BootstrapComboBoxMetrics ResolveMetrics(BootstrapTheme theme)
    {
        return BootstrapComboBoxRenderLogic.ResolveMetrics(
            theme.Metrics,
            Math.Max(1, Font.Height),
            GetCurrentDpi(),
            _borderRadius);
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
