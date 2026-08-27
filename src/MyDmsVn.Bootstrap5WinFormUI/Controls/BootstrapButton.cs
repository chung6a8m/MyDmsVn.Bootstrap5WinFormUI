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
/// Provides a Bootstrap-inspired native WinForms command button with semantic variants,
/// outline styling, icons, selection state, and spinner-backed loading presentation.
/// </summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Click))]
public class BootstrapButton : Button
{
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

    private readonly BootstrapSpinner _loadingSpinner;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private BootstrapButtonSize _buttonSize = BootstrapButtonSize.Default;
    private BootstrapIconPosition _iconPosition = BootstrapIconPosition.Left;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private IconDescriptor? _icon;
    private bool _outline;
    private bool _selected;
    private bool _loading;
    private string _loadingText = string.Empty;
    private int _borderRadius = -1;
    private CornerRadius? _groupCornerRadius;
    private bool _hovered;
    private bool _pressed;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe button using the current application theme.
    /// </summary>
    public BootstrapButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        // ButtonBase enables Opaque by default. Rounded owner painting leaves pixels outside
        // the rounded path untouched, so the background layer must run to clear reused buffers.
        SetStyle(ControlStyles.Opaque, false);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        UseVisualStyleBackColor = false;
        UseCompatibleTextRendering = false;
        BackColor = Color.Transparent;
        TabStop = true;
        TextAlign = ContentAlignment.MiddleCenter;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleDescription = "Bootstrap-inspired command button.";

        _loadingSpinner = new BootstrapSpinner
        {
            AutoSize = false,
            Type = BootstrapSpinnerType.Border,
            SpinnerSize = BootstrapSpinnerSize.Small,
            Spinning = false,
            Visible = false,
            TabStop = false
        };
        Controls.Add(_loadingSpinner);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        Size = GetPreferredSize(Size.Empty);
    }

    /// <summary>
    /// Gets or sets the semantic Bootstrap-inspired color variant.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired color variant.")]
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
    /// Gets or sets whether the button uses an outline presentation.
    /// </summary>
    [Category("Appearance")]
    [Description("Uses an outline presentation instead of a filled semantic background.")]
    [DefaultValue(false)]
    public bool Outline
    {
        get => _outline;
        set
        {
            if (_outline == value)
            {
                return;
            }

            _outline = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the standard button size.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the Small, Default, or Large button size.")]
    [DefaultValue(BootstrapButtonSize.Default)]
    public BootstrapButtonSize ButtonSize
    {
        get => _buttonSize;
        set
        {
            ValidateButtonSize(value);
            if (_buttonSize == value)
            {
                return;
            }

            _buttonSize = value;
            ApplyPreferredSize();
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets an optional source-neutral icon descriptor.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the source-neutral icon rendered next to the button text.")]
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
    /// Gets or sets whether the icon is rendered before or after the text.
    /// </summary>
    [Category("Appearance")]
    [Description("Places the icon to the left or right of the button text.")]
    [DefaultValue(BootstrapIconPosition.Left)]
    public BootstrapIconPosition IconPosition
    {
        get => _iconPosition;
        set
        {
            ValidateIconPosition(value);
            if (_iconPosition == value)
            {
                return;
            }

            _iconPosition = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the renderer used for <see cref="Icon"/>. The default renderer supports
    /// the framework vector and Segoe MDL2 sources; applications may inject compatible SVG or external providers.
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
    /// Gets or sets a uniform logical corner radius. Use -1 to select the current theme radius for <see cref="ButtonSize"/>.
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
    /// Gets or sets the logical selected state used by button groups and other composed controls.
    /// Selection changes presentation but does not automatically toggle when the button is clicked.
    /// </summary>
    [Category("Behavior")]
    [Description("Gets or sets the visual selected state. Selection policy is owned by composed controls such as ButtonGroup.")]
    [DefaultValue(false)]
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the button displays a spinner-backed loading presentation and suppresses click activation.
    /// </summary>
    [Category("Behavior")]
    [Description("Displays a loading spinner and suppresses click activation while preserving the button's preferred size.")]
    [DefaultValue(false)]
    public bool Loading
    {
        get => _loading;
        set
        {
            if (_loading == value)
            {
                return;
            }

            _loading = value;
            _pressed = false;
            UpdateLoadingState();
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets optional text displayed while <see cref="Loading"/> is true.
    /// An empty value keeps the normal button text beside the spinner.
    /// </summary>
    [Category("Behavior")]
    [Description("Specifies loading-state text. Empty keeps the normal button text beside the spinner.")]
    [DefaultValue("")]
    public string LoadingText
    {
        get => _loadingText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_loadingText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _loadingText = normalized;
            ApplyPreferredSize();
            PerformLayout();
            Invalidate();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = theme.Metrics;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var height = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalHeight(metrics, _buttonSize), dpi);
        var horizontalPadding = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalHorizontalPadding(metrics, _buttonSize), dpi);
        var spacing = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalContentSpacing(metrics, _buttonSize), dpi);
        var iconExtent = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalIconSize(metrics, _buttonSize), dpi);

        var normalText = MeasureText(Text);
        var normalIcon = _icon is null ? Size.Empty : new Size(iconExtent, iconExtent);
        var normalWidth = GetCombinedWidth(normalIcon, normalText, spacing);

        var loadingText = MeasureText(GetLoadingDisplayText());
        var spinnerSize = new Size(iconExtent, iconExtent);
        var loadingWidth = GetCombinedWidth(spinnerSize, loadingText, spacing);

        var contentWidth = Math.Max(normalWidth, loadingWidth);
        return new Size(Math.Max(1, contentWidth + (horizontalPadding * 2)), Math.Max(1, height));
    }

    /// <inheritdoc />
    protected override void OnClick(EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        base.OnClick(e);
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = Enabled && !_loading;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        _pressed = false;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left && Enabled && !_loading)
        {
            _pressed = true;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        if (_pressed)
        {
            _pressed = false;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs kevent)
    {
        base.OnKeyDown(kevent);
        if (Enabled && !_loading && (kevent.KeyCode == Keys.Space || kevent.KeyCode == Keys.Enter))
        {
            _pressed = true;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs kevent)
    {
        base.OnKeyUp(kevent);
        if (_pressed && (kevent.KeyCode == Keys.Space || kevent.KeyCode == Keys.Enter))
        {
            _pressed = false;
            Invalidate();
        }
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
        }

        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ApplyPreferredSize();
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

        ApplyPreferredSize();
        PerformLayout();
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
        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutLoadingSpinner();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs pevent)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var palette = ResolveCurrentPalette(theme);

        var graphics = pevent.Graphics;
        var previousSmoothingMode = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            PaintSurface(graphics, theme, palette);
            if (_loading)
            {
                _loadingSpinner.CustomColor = palette.Foreground;
                PaintLoadingContent(graphics, theme, palette.Foreground);
            }
            else
            {
                PaintNormalContent(graphics, theme, palette.Foreground);
            }

            PaintFocus(graphics, theme);
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothingMode;
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

    internal CornerRadius? GroupCornerRadius
    {
        get => _groupCornerRadius;
        set
        {
            if (_groupCornerRadius == value)
            {
                return;
            }

            _groupCornerRadius = value;
            Invalidate();
        }
    }

    internal BootstrapButtonPalette ResolveCurrentPalette(BootstrapTheme theme)
    {
        if (theme is null)
        {
            throw new ArgumentNullException(nameof(theme));
        }

        var visualState = _pressed
            ? BootstrapButtonVisualState.Pressed
            : (_hovered ? BootstrapButtonVisualState.Hover : BootstrapButtonVisualState.Normal);
        return BootstrapButtonRenderLogic.ResolvePalette(
            theme.Colors,
            _variant,
            _outline,
            Enabled,
            _selected,
            visualState);
    }

    internal CornerRadius GetEffectiveCornerRadius(BootstrapThemeMetrics metrics)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (_groupCornerRadius.HasValue)
        {
            return _groupCornerRadius.Value;
        }

        var radius = _borderRadius >= 0
            ? _borderRadius
            : BootstrapButtonRenderLogic.GetThemeBorderRadius(metrics, _buttonSize);
        return new CornerRadius(radius);
    }

    private void PaintSurface(Graphics graphics, BootstrapTheme theme, BootstrapButtonPalette palette)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var borderWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.BorderWidth, dpi));
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

        var radius = ScaleCornerRadius(GetEffectiveCornerRadius(theme.Metrics), dpi);
        using var path = RoundedPath.Create(bounds, radius);
        using var background = new SolidBrush(palette.Background);
        using var border = new Pen(palette.Border, borderWidth);
        graphics.FillPath(background, path);
        graphics.DrawPath(border, path);
    }

    private void PaintNormalContent(Graphics graphics, BootstrapTheme theme, Color foreground)
    {
        var layout = GetNormalContentLayout(theme);
        Rectangle textBounds;
        Rectangle iconBounds;
        if (_iconPosition == BootstrapIconPosition.Left)
        {
            iconBounds = layout.LeadingBounds;
            textBounds = layout.TrailingBounds;
        }
        else
        {
            textBounds = layout.LeadingBounds;
            iconBounds = layout.TrailingBounds;
        }

        if (_icon is not null && !iconBounds.IsEmpty)
        {
            _iconRenderer.TryRender(graphics, _icon, iconBounds, foreground);
        }

        DrawText(graphics, Text, textBounds, foreground);
    }

    private void PaintLoadingContent(Graphics graphics, BootstrapTheme theme, Color foreground)
    {
        var layout = GetLoadingContentLayout(theme);
        DrawText(graphics, GetLoadingDisplayText(), layout.TrailingBounds, foreground);
    }

    private void PaintFocus(Graphics graphics, BootstrapTheme theme)
    {
        if (!Focused || !ShowFocusCues || !Enabled)
        {
            return;
        }

        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var focusWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.FocusBorderWidth, dpi));
        var inset = DpiScaler.Scale((float)theme.Metrics.SpacingXS, dpi);
        var bounds = new RectangleF(
            inset,
            inset,
            Math.Max(0f, ClientSize.Width - (inset * 2f)),
            Math.Max(0f, ClientSize.Height - (inset * 2f)));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var radius = ScaleCornerRadius(GetEffectiveCornerRadius(theme.Metrics), dpi).NormalizeTo(bounds);
        using var path = RoundedPath.Create(bounds, radius);
        using var pen = new Pen(theme.Colors.Focus, focusWidth)
        {
            DashStyle = DashStyle.Dot
        };
        graphics.DrawPath(pen, path);
    }

    private HorizontalContentLayout GetNormalContentLayout(BootstrapTheme theme)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = theme.Metrics;
        var padding = GetContentPadding(metrics, dpi);
        var spacing = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalContentSpacing(metrics, _buttonSize), dpi);
        var iconExtent = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalIconSize(metrics, _buttonSize), dpi);
        var iconSize = _icon is null ? Size.Empty : new Size(iconExtent, iconExtent);
        var textSize = MeasureText(Text);

        return _iconPosition == BootstrapIconPosition.Left
            ? ContentLayoutHelper.ArrangeHorizontal(ClientRectangle, padding, iconSize, textSize, spacing, ContentAlignment.MiddleCenter)
            : ContentLayoutHelper.ArrangeHorizontal(ClientRectangle, padding, textSize, iconSize, spacing, ContentAlignment.MiddleCenter);
    }

    private HorizontalContentLayout GetLoadingContentLayout(BootstrapTheme theme)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = theme.Metrics;
        var padding = GetContentPadding(metrics, dpi);
        var spacing = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalContentSpacing(metrics, _buttonSize), dpi);
        var spinnerExtent = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalIconSize(metrics, _buttonSize), dpi);
        var spinnerSize = new Size(spinnerExtent, spinnerExtent);
        var textSize = MeasureText(GetLoadingDisplayText());
        return ContentLayoutHelper.ArrangeHorizontal(
            ClientRectangle,
            padding,
            spinnerSize,
            textSize,
            spacing,
            ContentAlignment.MiddleCenter);
    }

    private Padding GetContentPadding(BootstrapThemeMetrics metrics, int dpi)
    {
        var horizontal = DpiScaler.Scale(BootstrapButtonRenderLogic.GetLogicalHorizontalPadding(metrics, _buttonSize), dpi);
        return new Padding(horizontal, 0, horizontal, 0);
    }

    private void LayoutLoadingSpinner()
    {
        if (!_loading)
        {
            return;
        }

        var layout = GetLoadingContentLayout(BootstrapThemeManager.CurrentTheme);
        if (_loadingSpinner.Bounds != layout.LeadingBounds)
        {
            _loadingSpinner.Bounds = layout.LeadingBounds;
        }
    }

    private void UpdateLoadingState()
    {
        _loadingSpinner.Visible = _loading;
        _loadingSpinner.Spinning = _loading;
        if (_loading)
        {
            LayoutLoadingSpinner();
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

        ApplyPreferredSize();
        PerformLayout();
        Invalidate();
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previousFont = _themeFont;
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

        previousFont?.Dispose();
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

        var preferredSize = GetPreferredSize(Size.Empty);
        if (Size != preferredSize)
        {
            Size = preferredSize;
        }
    }

    private Size MeasureText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Size.Empty;
        }

        return TextRenderer.MeasureText(
            text,
            Font,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }

    private static int GetCombinedWidth(Size first, Size second, int spacing)
    {
        var hasFirst = !first.IsEmpty;
        var hasSecond = !second.IsEmpty;
        return (hasFirst ? first.Width : 0)
            + (hasFirst && hasSecond ? spacing : 0)
            + (hasSecond ? second.Width : 0);
    }

    private string GetLoadingDisplayText()
    {
        return string.IsNullOrEmpty(_loadingText) ? Text : _loadingText;
    }

    private void DrawText(Graphics graphics, string? text, Rectangle bounds, Color color)
    {
        if (string.IsNullOrEmpty(text) || bounds.IsEmpty)
        {
            return;
        }

        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            bounds,
            color,
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPadding |
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }

    private static CornerRadius ScaleCornerRadius(CornerRadius radius, int dpi)
    {
        return new CornerRadius(
            DpiScaler.Scale(radius.TopLeft, dpi),
            DpiScaler.Scale(radius.TopRight, dpi),
            DpiScaler.Scale(radius.BottomRight, dpi),
            DpiScaler.Scale(radius.BottomLeft, dpi));
    }

    private static void ValidateVariant(BootstrapVariant value)
    {
        if (value < BootstrapVariant.Primary || value > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported Bootstrap variant.");
        }
    }

    private static void ValidateButtonSize(BootstrapButtonSize value)
    {
        if (value != BootstrapButtonSize.Small &&
            value != BootstrapButtonSize.Default &&
            value != BootstrapButtonSize.Large)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported button size.");
        }
    }

    private static void ValidateIconPosition(BootstrapIconPosition value)
    {
        if (value != BootstrapIconPosition.Left && value != BootstrapIconPosition.Right)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported icon position.");
        }
    }
}
