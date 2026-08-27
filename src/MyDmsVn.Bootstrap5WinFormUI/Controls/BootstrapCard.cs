using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired themed surface with Header, Body, and Footer composition regions.
/// </summary>
[DefaultProperty(nameof(Body))]
public class BootstrapCard : ContainerControl
{
    // A rectangular child whose corner starts this fraction of the radius inward is inside
    // the rounded corner's 45-degree tangent point: 1 - (1 / sqrt(2)).
    private const float RoundedCornerSafeInsetFactor = 0.29289322f;

    private readonly Panel _header = new Panel();
    private readonly Panel _body = new Panel();
    private readonly Panel _footer = new Panel();
    private bool _showBorder = true;
    private bool _showShadow;
    private int _borderRadius = -1;
    private bool _themeSubscribed;
    private bool _settingThemePadding;
    private bool _useThemePadding = true;

    /// <summary>
    /// Initializes a designer-safe card using the current application theme.
    /// </summary>
    public BootstrapCard()
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
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Bootstrap-inspired card surface.";

        _header.Dock = DockStyle.Top;
        _header.Visible = false;
        _header.Height = 48;
        _header.Margin = Padding.Empty;

        _footer.Dock = DockStyle.Bottom;
        _footer.Visible = false;
        _footer.Height = 48;
        _footer.Margin = Padding.Empty;

        _body.Dock = DockStyle.Fill;
        _body.Margin = Padding.Empty;

        Controls.Add(_body);
        Controls.Add(_footer);
        Controls.Add(_header);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemePadding();
        ApplyTheme();
        Size = new Size(320, 200);
    }

    /// <summary>
    /// Gets the designer-serializable header content container. It is hidden by default.
    /// </summary>
    [Category("Layout")]
    [Description("Provides the optional top content region of the card.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Panel Header => _header;

    /// <summary>
    /// Gets the designer-serializable main content container.
    /// </summary>
    [Category("Layout")]
    [Description("Provides the main fill content region of the card.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Panel Body => _body;

    /// <summary>
    /// Gets the designer-serializable footer content container. It is hidden by default.
    /// </summary>
    [Category("Layout")]
    [Description("Provides the optional bottom content region of the card.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Panel Footer => _footer;

    /// <inheritdoc />
    public override Rectangle DisplayRectangle
    {
        get
        {
            var displayRectangle = base.DisplayRectangle;
            var decorationInsets = GetDecorationInsets();
            var leftExtra = Math.Max(0, decorationInsets.Left - Padding.Left);
            var topExtra = Math.Max(0, decorationInsets.Top - Padding.Top);
            var rightExtra = Math.Max(0, decorationInsets.Right - Padding.Right);
            var bottomExtra = Math.Max(0, decorationInsets.Bottom - Padding.Bottom);
            var width = Math.Max(0, displayRectangle.Width - leftExtra - rightExtra);
            var height = Math.Max(0, displayRectangle.Height - topExtra - bottomExtra);
            return new Rectangle(
                displayRectangle.Left + leftExtra,
                displayRectangle.Top + topExtra,
                width,
                height);
        }
    }

    /// <summary>
    /// Gets or sets whether the card paints a themed border.
    /// </summary>
    [Category("Appearance")]
    [Description("Paints the card's themed border.")]
    [DefaultValue(true)]
    public bool ShowBorder
    {
        get => _showBorder;
        set
        {
            if (_showBorder == value)
            {
                return;
            }

            _showBorder = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the card paints a lightweight drop shadow behind its surface.
    /// </summary>
    [Category("Appearance")]
    [Description("Paints a lightweight rounded drop shadow without a cached bitmap.")]
    [DefaultValue(false)]
    public bool ShowShadow
    {
        get => _showShadow;
        set
        {
            if (_showShadow == value)
            {
                return;
            }

            _showShadow = value;
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
            PerformLayout();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        if (!_settingThemePadding)
        {
            _useThemePadding = false;
        }

        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        if (_useThemePadding)
        {
            ApplyThemePadding();
        }

        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var borderWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.BorderWidth, dpi));
        var shadowOffset = _showShadow ? Math.Max(1f, DpiScaler.Scale(3f, dpi)) : 0f;
        var inset = borderWidth / 2f;
        var surfaceBounds = new RectangleF(
            inset,
            inset,
            Math.Max(0f, ClientSize.Width - borderWidth - shadowOffset),
            Math.Max(0f, ClientSize.Height - borderWidth - shadowOffset));
        if (surfaceBounds.Width <= 0f || surfaceBounds.Height <= 0f)
        {
            return;
        }

        var logicalRadius = _borderRadius >= 0 ? _borderRadius : theme.Metrics.Radius;
        var radius = DpiScaler.Scale((float)logicalRadius, dpi);
        var graphics = e.Graphics;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            if (_showShadow)
            {
                var shadowBounds = new RectangleF(
                    surfaceBounds.X + shadowOffset,
                    surfaceBounds.Y + shadowOffset,
                    surfaceBounds.Width,
                    surfaceBounds.Height);
                using var shadowPath = RoundedPath.Create(shadowBounds, new CornerRadius(radius));
                var alpha = theme.Mode == BootstrapThemeMode.Dark ? 80 : 42;
                using var shadowBrush = new SolidBrush(Color.FromArgb(alpha, Color.Black));
                graphics.FillPath(shadowBrush, shadowPath);
            }

            using var surfacePath = RoundedPath.Create(surfaceBounds, new CornerRadius(radius));
            using var surfaceBrush = new SolidBrush(theme.Colors.Surface);
            graphics.FillPath(surfaceBrush, surfacePath);

            if (_showBorder)
            {
                using var borderPen = new Pen(theme.Colors.Border, borderWidth);
                graphics.DrawPath(borderPen, surfacePath);
            }
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothing;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _themeSubscribed)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _themeSubscribed = false;
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (_useThemePadding)
        {
            ApplyThemePadding();
        }

        ApplyTheme();
        PerformLayout();
        Invalidate();
    }

    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        ForeColor = colors.Text;
        _header.BackColor = colors.Surface;
        _header.ForeColor = colors.Text;
        _body.BackColor = colors.Surface;
        _body.ForeColor = colors.Text;
        _footer.BackColor = colors.Surface;
        _footer.ForeColor = colors.Text;
    }

    private void ApplyThemePadding()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var inset = DpiScaler.Scale(theme.Metrics.SpacingMD, dpi);
        _settingThemePadding = true;
        try
        {
            Padding = new Padding(inset);
        }
        finally
        {
            _settingThemePadding = false;
        }
    }

    private Padding GetDecorationInsets()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var borderWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.BorderWidth, dpi));
        var logicalRadius = _borderRadius >= 0 ? _borderRadius : theme.Metrics.Radius;
        var radius = Math.Max(0f, DpiScaler.Scale((float)logicalRadius, dpi));
        var paintedBorderWidth = _showBorder ? borderWidth : 0f;
        var innerRadius = Math.Max(0f, radius - paintedBorderWidth);
        var roundedInset = paintedBorderWidth + (innerRadius * RoundedCornerSafeInsetFactor);
        var edgeInset = (int)Math.Ceiling(Math.Max(borderWidth / 2f, roundedInset));
        var shadowInset = _showShadow
            ? (int)Math.Ceiling(Math.Max(1f, DpiScaler.Scale(3f, dpi)))
            : 0;

        return new Padding(
            edgeInset,
            edgeInset,
            edgeInset + shadowInset,
            edgeInset + shadowInset);
    }
}
