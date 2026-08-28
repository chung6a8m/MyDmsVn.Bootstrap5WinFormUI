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
/// Displays a Bootstrap-inspired inline feedback message with optional icon and dismissal affordance.
/// </summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Dismissed))]
public class BootstrapAlert : UserControl
{
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();
    private static readonly IconDescriptor CloseIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close);

    private readonly Button _dismissButton;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private IconDescriptor? _icon;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private bool _dismissible;
    private int _borderRadius = -1;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe alert using the current application theme.
    /// </summary>
    public BootstrapAlert()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Alert;
        AccessibleDescription = "Bootstrap-inspired inline alert message.";

        _dismissButton = new Button
        {
            AutoSize = false,
            Text = string.Empty,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Visible = false,
            TabStop = false,
            AccessibleRole = AccessibleRole.PushButton,
            AccessibleName = "Dismiss alert",
            AccessibleDescription = "Dismisses this alert."
        };
        _dismissButton.FlatAppearance.BorderSize = 0;
        _dismissButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
        _dismissButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
        _dismissButton.Click += OnDismissButtonClick;
        _dismissButton.Paint += OnDismissButtonPaint;
        Controls.Add(_dismissButton);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyTheme();
    }

    /// <summary>
    /// Gets or sets the semantic Bootstrap-inspired color variant.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired alert color variant.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapAlertRenderLogic.ValidateVariant(value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            ApplyTheme();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets an optional source-neutral icon displayed before the alert text.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies an optional source-neutral icon rendered before the alert text.")]
    [DefaultValue(null)]
    public IconDescriptor? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value))
            {
                return;
            }

            _icon = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the renderer used for the optional content icon and framework close glyph.
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
            _dismissButton.Invalidate();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether a native keyboard-accessible close affordance is shown.
    /// </summary>
    [Category("Behavior")]
    [Description("Shows a native keyboard-accessible close affordance for dismissing the alert.")]
    [DefaultValue(false)]
    public bool Dismissible
    {
        get => _dismissible;
        set
        {
            if (_dismissible == value)
            {
                return;
            }

            _dismissible = value;
            UpdateDismissButtonState();
            PerformLayout();
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

    /// <summary>
    /// Occurs after the alert is hidden through <see cref="Dismiss"/> or its close affordance.
    /// Direct changes to <see cref="Control.Visible"/> do not raise this event.
    /// </summary>
    [Category("Action")]
    [Description("Occurs after the alert is dismissed through its dismissal path.")]
    public event EventHandler? Dismissed;

    /// <summary>
    /// Hides the alert immediately and raises <see cref="Dismissed"/> once for the effective visible-to-hidden dismissal.
    /// </summary>
    public void Dismiss()
    {
        if (!Visible)
        {
            return;
        }

        Visible = false;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        if (IsDisposed)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
        var layout = BootstrapAlertRenderLogic.CalculateLayout(ClientRectangle, metrics, _icon is not null, _dismissible);
        _dismissButton.Bounds = layout.CloseBounds;
        UpdateDismissButtonState();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var palette = BootstrapAlertRenderLogic.ResolvePalette(theme.Colors, _variant, Enabled);
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
        var layout = BootstrapAlertRenderLogic.CalculateLayout(ClientRectangle, metrics, _icon is not null, _dismissible);
        if (layout.SurfaceBounds.Width <= 0 || layout.SurfaceBounds.Height <= 0)
        {
            return;
        }

        var previousSmoothingMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(layout.SurfaceBounds, layout.CornerRadius);
            using var surfaceBrush = new SolidBrush(palette.Surface);
            e.Graphics.FillPath(surfaceBrush, path);

            if (metrics.BorderWidth > 0)
            {
                using var borderPen = new Pen(palette.Border, metrics.BorderWidth);
                e.Graphics.DrawPath(borderPen, path);
            }

            if (_icon is not null && layout.IconBounds.Width > 0 && layout.IconBounds.Height > 0)
            {
                _iconRenderer.TryRender(e.Graphics, _icon, layout.IconBounds, palette.Foreground);
            }
        }
        finally
        {
            e.Graphics.SmoothingMode = previousSmoothingMode;
        }

        if (layout.TextBounds.Width > 0 && layout.TextBounds.Height > 0)
        {
            TextRenderer.DrawText(
                e.Graphics,
                Text ?? string.Empty,
                Font,
                layout.TextBounds,
                palette.Foreground,
                TextFormatFlags.NoPrefix |
                TextFormatFlags.WordBreak |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter);
        }
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        ApplyTheme();
        _dismissButton.Invalidate();
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

        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        PerformLayout();
        _dismissButton.Invalidate();
        Invalidate();
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

            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void OnDismissButtonClick(object? sender, EventArgs e)
    {
        Dismiss();
    }

    private void OnDismissButtonPaint(object? sender, PaintEventArgs e)
    {
        if (_dismissButton.ClientSize.Width <= 0 || _dismissButton.ClientSize.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var palette = BootstrapAlertRenderLogic.ResolvePalette(theme.Colors, _variant, Enabled);
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
        var inset = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        var glyphBounds = Rectangle.Inflate(_dismissButton.ClientRectangle, -inset, -inset);
        if (glyphBounds.Width > 0 && glyphBounds.Height > 0)
        {
            _iconRenderer.TryRender(e.Graphics, CloseIcon, glyphBounds, palette.Foreground);
        }

        if (_dismissButton.Focused && metrics.FocusBorderWidth > 0)
        {
            var focusBounds = Rectangle.Inflate(_dismissButton.ClientRectangle, -inset, -inset);
            focusBounds.Width = Math.Max(0, focusBounds.Width - 1);
            focusBounds.Height = Math.Max(0, focusBounds.Height - 1);
            if (focusBounds.Width > 0 && focusBounds.Height > 0)
            {
                using var focusPen = new Pen(palette.Focus, metrics.FocusBorderWidth);
                e.Graphics.DrawRectangle(focusPen, focusBounds);
            }
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

        ApplyTheme();
        PerformLayout();
        _dismissButton.Invalidate();
        Invalidate();
    }

    private void ApplyTheme()
    {
        if (IsDisposed)
        {
            return;
        }

        var palette = BootstrapAlertRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme.Colors,
            _variant,
            Enabled);
        _dismissButton.BackColor = palette.Surface;
        _dismissButton.ForeColor = palette.Foreground;
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;

        _settingThemeFont = true;
        try
        {
            Font = nextFont;
        }
        finally
        {
            _settingThemeFont = false;
        }

        if (previous is not null && ReferenceEquals(Font, previous))
        {
            nextFont.Dispose();
            return;
        }

        _themeFont = nextFont;
        previous?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private void UpdateDismissButtonState()
    {
        _dismissButton.Visible = _dismissible;
        _dismissButton.TabStop = _dismissible;
    }

    private int GetCurrentDpi()
    {
        return DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
    }
}
