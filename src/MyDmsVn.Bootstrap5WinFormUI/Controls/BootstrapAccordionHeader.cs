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
/// Provides the full-width focusable command surface used by a <see cref="BootstrapAccordionItem"/>.
/// </summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Click))]
public class BootstrapAccordionHeader : Control
{
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private IconDescriptor? _icon;
    private bool _showChevron = true;
    private bool _expanded;
    private double _animationProgress;
    private bool _hovered;
    private bool _pressed;
    private bool _spacePressed;
    private bool _flush;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe, keyboard-focusable accordion header.
    /// </summary>
    public BootstrapAccordionHeader()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            true);
        SetStyle(ControlStyles.StandardClick, false);

        BackColor = Color.Transparent;
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleDescription = "Collapsed accordion section header.";

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        Size = GetPreferredSize(Size.Empty);
    }

    /// <summary>
    /// Gets or sets an optional source-neutral icon rendered before the header text.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the source-neutral icon rendered before the accordion header text.")]
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
            ApplyPreferredSize();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the renderer used for <see cref="Icon"/>.
    /// </summary>
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

    /// <summary>
    /// Gets or sets whether the framework vector chevron is displayed.
    /// </summary>
    [Category("Appearance")]
    [Description("Shows the vector chevron that follows the collapse animation progress.")]
    [DefaultValue(true)]
    public bool ShowChevron
    {
        get => _showChevron;
        set
        {
            if (_showChevron == value)
            {
                return;
            }

            _showChevron = value;
            ApplyPreferredSize();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets whether the associated accordion section is logically expanded.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Expanded => _expanded;

    /// <summary>
    /// Gets the current visual expansion amount from 0 (collapsed) through 1 (expanded).
    /// The vector chevron rotation is derived directly from this value.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double AnimationProgress => _animationProgress;

    /// <summary>
    /// Activates the header through the same <see cref="Control.Click"/> event used by mouse and keyboard input.
    /// </summary>
    public void PerformClick()
    {
        if (!Enabled || IsDisposed)
        {
            return;
        }

        OnClick(EventArgs.Empty);
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var horizontalPadding = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var contentGap = DpiScaler.Scale(theme.Metrics.SpacingSM, dpi);
        var iconExtent = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var textSize = TextRenderer.MeasureText(
            string.IsNullOrEmpty(Text) ? "Ag" : Text,
            Font,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

        var width = horizontalPadding * 2 + textSize.Width;
        if (_icon is not null)
        {
            width += iconExtent + contentGap;
        }

        if (_showChevron)
        {
            width += iconExtent + contentGap;
        }

        var logicalHeight = theme.Metrics.ControlHeightLarge + theme.Metrics.SpacingSM;
        var height = Math.Max(DpiScaler.Scale(logicalHeight, dpi), textSize.Height + DpiScaler.Scale(theme.Metrics.SpacingMD, dpi));
        return new Size(Math.Max(1, width), Math.Max(1, height));
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (Enabled)
        {
            _hovered = true;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        if (!_spacePressed)
        {
            _pressed = false;
        }

        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Enabled && e.Button == MouseButtons.Left)
        {
            Focus();
            Capture = true;
            _pressed = true;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        var shouldActivate = Enabled && e.Button == MouseButtons.Left && _pressed && ClientRectangle.Contains(e.Location);
        if (e.Button == MouseButtons.Left)
        {
            Capture = false;
            _pressed = false;
            Invalidate();
        }

        base.OnMouseUp(e);
        if (shouldActivate)
        {
            PerformClick();
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Enabled)
        {
            return;
        }

        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            PerformClick();
        }
        else if (e.KeyCode == Keys.Space)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            _spacePressed = true;
            _pressed = true;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && _spacePressed)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            _spacePressed = false;
            _pressed = false;
            Invalidate();
            PerformClick();
        }

        base.OnKeyUp(e);
    }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _spacePressed = false;
        _pressed = false;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled)
        {
            _hovered = false;
            _pressed = false;
            _spacePressed = false;
        }

        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ApplyPreferredSize();
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
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyPreferredSize();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var background = ResolveBackground(theme);
        var foreground = Enabled
            ? (_expanded ? theme.Colors.Primary : theme.Colors.Text)
            : theme.Colors.MutedText;

        var graphics = e.Graphics;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            PaintSurface(graphics, theme, background);
            PaintContent(graphics, theme, foreground);
            PaintFocus(graphics, theme);
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

            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    internal bool Flush
    {
        get => _flush;
        set
        {
            if (_flush == value)
            {
                return;
            }

            _flush = value;
            Invalidate();
        }
    }

    internal void SetExpansionState(bool expanded, double animationProgress)
    {
        animationProgress = Math.Max(0.0, Math.Min(1.0, animationProgress));
        var changed = _expanded != expanded || Math.Abs(_animationProgress - animationProgress) >= 0.000001;
        _expanded = expanded;
        _animationProgress = animationProgress;
        AccessibleDescription = _expanded
            ? "Expanded accordion section header."
            : "Collapsed accordion section header.";

        if (changed)
        {
            Invalidate();
        }
    }

    private Color ResolveBackground(BootstrapTheme theme)
    {
        if (!Enabled)
        {
            return theme.Colors.SurfaceSecondary;
        }

        if (_pressed)
        {
            return theme.Colors.Active;
        }

        if (_hovered)
        {
            return theme.Colors.Hover;
        }

        return _expanded ? theme.Colors.SurfaceSecondary : theme.Colors.Surface;
    }

    private void PaintSurface(Graphics graphics, BootstrapTheme theme, Color background)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        using var brush = new SolidBrush(background);
        if (_flush)
        {
            graphics.FillRectangle(brush, ClientRectangle);
            return;
        }

        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var radius = DpiScaler.Scale(theme.Metrics.Radius, dpi);
        using var path = RoundedPath.Create(ClientRectangle, new CornerRadius(radius));
        graphics.FillPath(brush, path);
    }

    private void PaintContent(Graphics graphics, BootstrapTheme theme, Color foreground)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var horizontalPadding = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var gap = DpiScaler.Scale(theme.Metrics.SpacingSM, dpi);
        var iconExtent = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var left = horizontalPadding;
        var right = Math.Max(left, ClientSize.Width - horizontalPadding);

        if (_icon is not null)
        {
            var iconBounds = new Rectangle(
                left,
                Math.Max(0, (ClientSize.Height - iconExtent) / 2),
                iconExtent,
                iconExtent);
            _iconRenderer.TryRender(graphics, _icon, iconBounds, foreground);
            left = iconBounds.Right + gap;
        }

        if (_showChevron)
        {
            var chevronBounds = new Rectangle(
                Math.Max(left, right - iconExtent),
                Math.Max(0, (ClientSize.Height - iconExtent) / 2),
                iconExtent,
                iconExtent);
            PaintChevron(graphics, chevronBounds, foreground, theme, dpi);
            right = Math.Max(left, chevronBounds.Left - gap);
        }

        var textBounds = Rectangle.FromLTRB(left, 0, right, ClientSize.Height);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            textBounds,
            foreground,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    private void PaintChevron(Graphics graphics, Rectangle bounds, Color color, BootstrapTheme theme, int dpi)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var strokeWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.BorderWidth + 0.5f, dpi));
        var span = Math.Max(3f, Math.Min(bounds.Width, bounds.Height) * 0.5f);
        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(bounds.Left + (bounds.Width / 2f), bounds.Top + (bounds.Height / 2f));
            graphics.RotateTransform((float)(180.0 * _animationProgress));
            using var pen = new Pen(color, strokeWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            graphics.DrawLines(
                pen,
                new[]
                {
                    new PointF(-span / 2f, -span / 4f),
                    new PointF(0f, span / 4f),
                    new PointF(span / 2f, -span / 4f)
                });
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private void PaintFocus(Graphics graphics, BootstrapTheme theme)
    {
        if (!Focused || !ShowFocusCues || !Enabled)
        {
            return;
        }

        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var inset = Math.Max(2, DpiScaler.Scale(theme.Metrics.SpacingXS, dpi));
        var focusWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.FocusBorderWidth, dpi));
        var bounds = new RectangleF(
            inset,
            inset,
            Math.Max(0, ClientSize.Width - (inset * 2)),
            Math.Max(0, ClientSize.Height - (inset * 2)));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        using var pen = new Pen(theme.Colors.Focus, focusWidth)
        {
            DashStyle = DashStyle.Dot
        };
        if (_flush)
        {
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            return;
        }

        var radius = DpiScaler.Scale(theme.Metrics.RadiusSmall, dpi);
        using var path = RoundedPath.Create(bounds, new CornerRadius(radius));
        graphics.DrawPath(pen, path);
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
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = next;
        _settingThemeFont = true;
        try
        {
            Font = next;
        }
        finally
        {
            _settingThemeFont = false;
        }

        previous?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private void ApplyPreferredSize()
    {
        if (!AutoSize || IsDisposed)
        {
            return;
        }

        Size = GetPreferredSize(Size.Empty);
    }
}
