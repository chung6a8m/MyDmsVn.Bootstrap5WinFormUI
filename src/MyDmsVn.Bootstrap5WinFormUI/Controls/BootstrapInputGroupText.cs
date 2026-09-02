using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Displays non-interactive text and an optional icon inside a connected input group.</summary>
[DefaultProperty(nameof(Text))]
public class BootstrapInputGroupText : Control, IBootstrapConnectedControl
{
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();
    private IconDescriptor? _icon;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;
    private int _borderRadius = -1;
    private CornerRadius? _connectedCornerRadius;
    private BootstrapConnectedControlSize? _connectedSizeOverride;
    private Font? _themeFont;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _themeSubscribed;

    /// <summary>Initializes a designer-safe, non-focusable addon surface.</summary>
    public BootstrapInputGroupText()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.StaticText;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        Size = GetPreferredSize(Size.Empty);
    }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
    public override string Text
    {
        get => base.Text;
        set => base.Text = value ?? string.Empty;
    }

    /// <summary>Gets or sets the optional source-neutral addon icon.</summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public IconDescriptor? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnContentChanged();
        }
    }

    /// <summary>Gets or sets the renderer used for <see cref="Icon"/>.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer
    {
        get => _iconRenderer;
        set
        {
            _iconRenderer = value ?? throw new ArgumentNullException(nameof(value));
            Invalidate();
        }
    }

    /// <summary>Gets or sets alignment of the addon text.</summary>
    [Category("Appearance")]
    [DefaultValue(ContentAlignment.MiddleCenter)]
    public ContentAlignment TextAlign
    {
        get => _textAlign;
        set
        {
            _textAlign = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the standalone logical corner radius, or -1 for the current theme radius.</summary>
    [Category("Appearance")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or non-negative.");
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
        var padding = DpiScaler.Scale(GetLogicalPadding(theme.Metrics), dpi);
        var spacing = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        var iconSize = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var textSize = string.IsNullOrEmpty(Text)
            ? Size.Empty
            : TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        var contentWidth = textSize.Width;
        if (_icon is not null)
        {
            contentWidth += iconSize + (textSize.Width > 0 ? spacing : 0);
        }

        var height = DpiScaler.Scale(GetLogicalHeight(theme.Metrics, GetEffectiveSize()), dpi);
        return new Size(Math.Max(1, contentWidth + (padding * 2)), Math.Max(1, height));
    }

    CornerRadius? IBootstrapConnectedControl.ConnectedCornerRadius
    {
        get => _connectedCornerRadius;
        set
        {
            _connectedCornerRadius = value;
            Invalidate();
        }
    }

    BootstrapConnectedControlSize? IBootstrapConnectedControl.ConnectedSizeOverride
    {
        get => _connectedSizeOverride;
        set
        {
            _connectedSizeOverride = value;
            OnContentChanged();
        }
    }

    int IBootstrapConnectedControl.GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize size, int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi));
        }

        return DpiScaler.Scale(GetLogicalHeight(BootstrapThemeManager.CurrentTheme.Metrics, size), dpi);
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        OnContentChanged();
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

        OnContentChanged();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var borderWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.BorderWidth, dpi));
        var inset = borderWidth / 2f;
        var bounds = new RectangleF(inset, inset, Math.Max(0f, Width - borderWidth), Math.Max(0f, Height - borderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var logicalRadius = _borderRadius >= 0 ? _borderRadius : GetLogicalRadius(theme.Metrics, GetEffectiveSize());
        var radius = _connectedCornerRadius.HasValue
            ? Scale(_connectedCornerRadius.Value, dpi)
            : new CornerRadius(DpiScaler.Scale((float)logicalRadius, dpi));
        var graphics = e.Graphics;
        var smoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(bounds, radius);
            using var background = new SolidBrush(theme.Colors.SurfaceSecondary);
            using var border = new Pen(theme.Colors.Border, borderWidth);
            graphics.FillPath(background, path);
            graphics.DrawPath(border, path);
        }
        finally
        {
            graphics.SmoothingMode = smoothing;
        }

        PaintContent(graphics, theme, dpi);
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

    private void PaintContent(Graphics graphics, BootstrapTheme theme, int dpi)
    {
        var padding = DpiScaler.Scale(GetLogicalPadding(theme.Metrics), dpi);
        var spacing = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        var iconSize = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var content = Rectangle.FromLTRB(padding, 0, Math.Max(padding, Width - padding), Height);
        var color = Enabled ? theme.Colors.Text : theme.Colors.MutedText;
        if (_icon is not null)
        {
            var iconBounds = new Rectangle(content.Left, Math.Max(0, (Height - iconSize) / 2), iconSize, iconSize);
            _iconRenderer.TryRender(graphics, _icon, iconBounds, color);
            content.X += iconSize + spacing;
            content.Width = Math.Max(0, content.Width - iconSize - spacing);
        }

        TextRenderer.DrawText(graphics, Text, Font, content, color, ResolveTextFlags(_textAlign));
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (_useThemeFont)
        {
            ApplyThemeFont();
        }
        OnContentChanged();
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = next;
        _settingThemeFont = true;
        try { Font = next; }
        finally { _settingThemeFont = false; }
        previous?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private void OnContentChanged()
    {
        if (AutoSize)
        {
            Size = GetPreferredSize(Size.Empty);
        }
        Invalidate();
    }

    private BootstrapConnectedControlSize GetEffectiveSize() => _connectedSizeOverride ?? BootstrapConnectedControlSize.Default;

    private static int GetLogicalHeight(BootstrapThemeMetrics metrics, BootstrapConnectedControlSize size) =>
        size == BootstrapConnectedControlSize.Small ? metrics.ControlHeightSmall :
        (size == BootstrapConnectedControlSize.Large ? metrics.ControlHeightLarge : metrics.ControlHeight);

    private static int GetLogicalRadius(BootstrapThemeMetrics metrics, BootstrapConnectedControlSize size) =>
        size == BootstrapConnectedControlSize.Small ? metrics.RadiusSmall :
        (size == BootstrapConnectedControlSize.Large ? metrics.RadiusLarge : metrics.Radius);

    private static int GetLogicalPadding(BootstrapThemeMetrics metrics) => metrics.SpacingSM;

    private static CornerRadius Scale(CornerRadius value, int dpi) => new CornerRadius(
        DpiScaler.Scale(value.TopLeft, dpi), DpiScaler.Scale(value.TopRight, dpi),
        DpiScaler.Scale(value.BottomRight, dpi), DpiScaler.Scale(value.BottomLeft, dpi));

    private static TextFormatFlags ResolveTextFlags(ContentAlignment alignment)
    {
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        flags |= alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.BottomLeft
            ? TextFormatFlags.Left
            : (alignment == ContentAlignment.TopRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.BottomRight ? TextFormatFlags.Right : TextFormatFlags.HorizontalCenter);
        flags |= alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight
            ? TextFormatFlags.Top
            : (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.BottomRight ? TextFormatFlags.Bottom : TextFormatFlags.VerticalCenter);
        return flags;
    }
}
