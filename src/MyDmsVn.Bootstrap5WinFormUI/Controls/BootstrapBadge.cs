using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Displays a compact, non-interactive Bootstrap-inspired text indicator.
/// </summary>
[DefaultProperty(nameof(Text))]
public class BootstrapBadge : Control
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private Color _customColor = Color.Empty;
    private bool _pill;
    private int _borderRadius = -1;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe badge using the current application theme.
    /// </summary>
    public BootstrapBadge()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        SetStyle(ControlStyles.Selectable, false);

        AutoSize = true;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleDescription = "Bootstrap-inspired badge indicator.";

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyPreferredSize();
    }

    /// <summary>
    /// Gets or sets the semantic color variant used when <see cref="CustomColor"/> is empty.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired badge color variant.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            ValidateVariant(value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets an optional opaque background color that overrides <see cref="Variant"/>.
    /// Use <see cref="Color.Empty"/> to resolve the semantic variant from the current theme.
    /// </summary>
    [Category("Appearance")]
    [Description("Overrides the semantic badge color with Color.Empty or a fully opaque color.")]
    public Color CustomColor
    {
        get => _customColor;
        set
        {
            if (!value.IsEmpty && value.A != byte.MaxValue)
            {
                throw new ArgumentException("Custom color must be Color.Empty or fully opaque.", nameof(value));
            }

            if (_customColor == value)
            {
                return;
            }

            _customColor = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the badge uses a pill radius equal to half its rendered height.
    /// </summary>
    [Category("Appearance")]
    [Description("Uses a pill-shaped radius equal to half the rendered badge height.")]
    [DefaultValue(false)]
    public bool Pill
    {
        get => _pill;
        set
        {
            if (_pill == value)
            {
                return;
            }

            _pill = value;
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
    public override Size GetPreferredSize(Size proposedSize)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var textHeight = Font.Height;
        var textSize = string.IsNullOrEmpty(Text)
            ? new Size(0, textHeight)
            : TextRenderer.MeasureText(
                Text,
                Font,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

        return BootstrapBadgeRenderLogic.GetPreferredSize(textSize, theme.Metrics, dpi);
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ApplyPreferredSize();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnAutoSizeChanged(EventArgs e)
    {
        base.OnAutoSizeChanged(e);
        ApplyPreferredSize();
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

        ApplyPreferredSize();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyPreferredSize();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var palette = BootstrapBadgeRenderLogic.ResolvePalette(theme.Colors, _variant, _customColor, Enabled);
        var radius = BootstrapBadgeRenderLogic.GetRadius(ClientSize.Height, theme.Metrics, _pill, _borderRadius, dpi);
        var bounds = new RectangleF(0f, 0f, Math.Max(0f, ClientSize.Width - 1f), Math.Max(0f, ClientSize.Height - 1f));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var previousSmoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(bounds, new CornerRadius(radius));
            using var brush = new SolidBrush(palette.Background);
            e.Graphics.FillPath(brush, path);
        }
        finally
        {
            e.Graphics.SmoothingMode = previousSmoothing;
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text ?? string.Empty,
            Font,
            ClientRectangle,
            palette.Foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.SingleLine);
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

        ApplyPreferredSize();
        Invalidate();
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Label;
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

        // WinForms may treat a value-equal Font assignment as a no-op. In that case
        // the control still references the previous instance, so keep owning it and
        // dispose only the unused replacement.
        if (previous is not null && ReferenceEquals(Font, previous))
        {
            nextFont.Dispose();
            return;
        }

        _themeFont = nextFont;
        previous?.Dispose();
    }

    private void ApplyPreferredSize()
    {
        if (!AutoSize || IsDisposed)
        {
            return;
        }

        var preferredSize = GetPreferredSize(Size.Empty);
        if (Size != preferredSize)
        {
            Size = preferredSize;
        }
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private static void ValidateVariant(BootstrapVariant value)
    {
        if (value < BootstrapVariant.Primary || value > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported Bootstrap variant.");
        }
    }
}
