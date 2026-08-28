using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Displays a Bootstrap-inspired transient notification surface that can be owned by a toast container.
/// </summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Dismissed))]
public class BootstrapToast : UserControl
{
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();
    private static readonly IconDescriptor CloseIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close);

    private readonly Func<IBootstrapToastAutoHideTimer> _timerFactory;
    private Button? _dismissButton;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private IconDescriptor? _icon;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private string _title = string.Empty;
    private bool _dismissible = true;
    private bool _autoHide = true;
    private int _autoHideDelay = 5000;
    private int _animationDuration = 200;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;
    private Font? _titleFont;
    private Action<BootstrapToast>? _dismissRequest;
    private Action<BootstrapToast>? _preferredHeightChanged;
    private bool _enterCompleted;
    private bool _hostVisible = true;
    private IBootstrapToastAutoHideTimer? _autoHideTimer;
    private EventHandler? _autoHideTickHandler;
    private int _autoHideGeneration;
    private int _autoHideRemainingDelay = 5000;
    private long _autoHideStartedTimestamp;

    /// <summary>Initializes a designer-safe toast using the current application theme.</summary>
    public BootstrapToast()
        : this(() => new WinFormsBootstrapToastAutoHideTimer())
    {
    }

    internal BootstrapToast(Func<IBootstrapToastAutoHideTimer> timerFactory)
    {
        _timerFactory = timerFactory ?? throw new ArgumentNullException(nameof(timerFactory));

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        _dismissButton = CreateDismissButton();
        Controls.Add(_dismissButton);

        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Alert;
        AccessibleDescription = "Transient notification.";
        Size = new Size(320, 96);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        RebuildTitleFont();
        ApplyTheme();
        UpdateDismissButtonState();
    }

    /// <summary>Gets or sets the optional single-line title displayed above the notification body.</summary>
    [Category("Appearance")]
    [Description("Specifies an optional single-line title displayed above the notification body.")]
    [DefaultValue("")]
    public string Title
    {
        get => _title;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_title, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _title = normalized;
            NotifyContentChanged();
        }
    }

    /// <summary>Gets or sets the semantic Bootstrap-inspired color variant.</summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired toast color variant.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapFeedbackRenderLogic.ValidateVariant(value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            ApplyTheme();
            Invalidate();
        }
    }

    /// <summary>Gets or sets an optional source-neutral icon displayed before the toast content.</summary>
    [Category("Appearance")]
    [Description("Specifies an optional source-neutral icon rendered before the toast content.")]
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
            NotifyContentChanged();
        }
    }

    /// <summary>Gets or sets the renderer used for the optional icon and framework close glyph.</summary>
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
            _dismissButton?.Invalidate();
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether a native keyboard-accessible close affordance is shown.</summary>
    [Category("Behavior")]
    [Description("Shows a native keyboard-accessible close affordance for dismissing the toast.")]
    [DefaultValue(true)]
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
            NotifyContentChanged();
        }
    }

    /// <summary>Gets or sets whether the owned toast dismisses itself after the auto-hide delay.</summary>
    [Category("Behavior")]
    [Description("Automatically dismisses the toast after it has fully entered and the delay elapses.")]
    [DefaultValue(true)]
    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            if (_autoHide == value)
            {
                return;
            }

            _autoHide = value;
            if (value)
            {
                RestartAutoHideTimerIfEligible();
            }
            else
            {
                StopAndDisposeAutoHideTimer();
                ResetAutoHideRemainingDelay();
            }
        }
    }

    /// <summary>Gets or sets the auto-hide delay in milliseconds.</summary>
    [Category("Behavior")]
    [Description("Sets the full auto-hide delay in milliseconds after the toast is fully visible.")]
    [DefaultValue(5000)]
    public int AutoHideDelay
    {
        get => _autoHideDelay;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Auto-hide delay must be greater than zero.");
            }

            if (_autoHideDelay == value)
            {
                return;
            }

            _autoHideDelay = value;
            RestartAutoHideTimerIfEligible();
        }
    }

    /// <summary>Gets or sets the enter and exit transition duration in milliseconds.</summary>
    [Category("Behavior")]
    [Description("Sets the enter and exit transition duration in milliseconds.")]
    [DefaultValue(200)]
    public int AnimationDuration
    {
        get => _animationDuration;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Animation duration must be greater than zero.");
            }

            _animationDuration = value;
        }
    }

    /// <summary>
    /// Occurs once when the toast begins its logical dismissal path.
    /// For container-owned toasts this event precedes exit-animation completion and disposal.
    /// </summary>
    [Category("Action")]
    [Description("Occurs once when the toast begins logical dismissal.")]
    public event EventHandler? Dismissed;

    /// <summary>
    /// Requests dismissal. Detached visible toasts hide immediately; owned toasts delegate lifecycle completion to their container.
    /// </summary>
    public void Dismiss()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_dismissRequest is not null)
        {
            StopAndDisposeAutoHideTimer();
            _dismissRequest(this);
            return;
        }

        if (!Visible)
        {
            return;
        }

        StopAndDisposeAutoHideTimer();
        Visible = false;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var width = proposedSize.Width > 0 ? proposedSize.Width : Width;
        width = Math.Max(1, width);
        return new Size(width, CalculatePreferredHeight(width));
    }

    internal bool IsOwned => _dismissRequest is not null;

    internal bool IsFullyVisible => _enterCompleted && Visible && _hostVisible && !IsDisposed;

    internal bool HasActiveAutoHideTimer => _autoHideTimer is not null && _autoHideTimer.Enabled;

    internal void AttachOwner(Action<BootstrapToast> dismissRequest, Action<BootstrapToast> preferredHeightChanged)
    {
        if (dismissRequest is null)
        {
            throw new ArgumentNullException(nameof(dismissRequest));
        }

        if (preferredHeightChanged is null)
        {
            throw new ArgumentNullException(nameof(preferredHeightChanged));
        }

        if (_dismissRequest is not null)
        {
            throw new InvalidOperationException("The toast is already owned by a container.");
        }

        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapToast));
        }

        _dismissRequest = dismissRequest;
        _preferredHeightChanged = preferredHeightChanged;
        _enterCompleted = false;
        StopAndDisposeAutoHideTimer();
        ResetAutoHideRemainingDelay();
    }

    internal void NotifyEnterStarted()
    {
        _enterCompleted = false;
        StopAndDisposeAutoHideTimer();
        ResetAutoHideRemainingDelay();
    }

    internal void NotifyEnterCompleted()
    {
        if (_dismissRequest is null || IsDisposed)
        {
            return;
        }

        _enterCompleted = true;
        RestartAutoHideTimerIfEligible();
    }

    internal void NotifyExitStarting()
    {
        _enterCompleted = false;
        StopAndDisposeAutoHideTimer();
        ResetAutoHideRemainingDelay();
    }

    internal void NotifyHostVisibilityChanged(bool visible)
    {
        _hostVisible = visible;
        if (visible)
        {
            RestartAutoHideTimerIfEligible(resetDelay: false);
        }
        else
        {
            PauseAutoHideTimer();
        }
    }

    internal void NotifyRemovedFromOwner()
    {
        _enterCompleted = false;
        _hostVisible = true;
        StopAndDisposeAutoHideTimer();
        ResetAutoHideRemainingDelay();
        _dismissRequest = null;
        _preferredHeightChanged = null;
    }

    internal void RaiseDismissedFromOwner()
    {
        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    internal int CalculatePreferredHeightForCurrentWidth()
    {
        return CalculatePreferredHeight(Math.Max(1, Width));
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (IsDisposed || _dismissButton is null)
        {
            return;
        }

        var layout = CalculateCurrentLayout();
        _dismissButton.Bounds = layout.CloseBounds;
        UpdateDismissButtonState();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var palette = BootstrapFeedbackRenderLogic.ResolvePalette(theme.Colors, _variant, Enabled);
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(theme.Metrics, dpi);
        var layout = CalculateCurrentLayout();
        if (layout.SurfaceBounds.Width <= 0 || layout.SurfaceBounds.Height <= 0)
        {
            return;
        }

        var borderWidth = Math.Max(0f, metrics.BorderWidth);
        var borderInset = borderWidth / 2f;
        var surfaceBounds = new RectangleF(
            layout.SurfaceBounds.X + borderInset,
            layout.SurfaceBounds.Y + borderInset,
            Math.Max(0f, layout.SurfaceBounds.Width - borderWidth),
            Math.Max(0f, layout.SurfaceBounds.Height - borderWidth));
        if (surfaceBounds.Width <= 0f || surfaceBounds.Height <= 0f)
        {
            return;
        }

        var previousSmoothingMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(surfaceBounds, layout.CornerRadius);
            using var surfaceBrush = new SolidBrush(palette.Surface);
            e.Graphics.FillPath(surfaceBrush, path);

            if (metrics.BorderWidth > 0)
            {
                using var borderPen = new Pen(palette.Border, borderWidth);
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

        if (!string.IsNullOrEmpty(_title) && layout.TitleBounds.Width > 0 && layout.TitleBounds.Height > 0)
        {
            TextRenderer.DrawText(
                e.Graphics,
                _title,
                _titleFont ?? Font,
                layout.TitleBounds,
                palette.Foreground,
                TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter);
        }

        if (!string.IsNullOrEmpty(Text) && layout.BodyBounds.Width > 0 && layout.BodyBounds.Height > 0)
        {
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                layout.BodyBounds,
                palette.Foreground,
                TextFormatFlags.NoPrefix |
                TextFormatFlags.WordBreak |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.Left |
                TextFormatFlags.Top);
        }
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        NotifyContentChanged();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        ApplyTheme();
        UpdateDismissButtonState();
        _dismissButton?.Invalidate();
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

        RebuildTitleFont();
        NotifyContentChanged();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        NotifyContentChanged();
        _dismissButton?.Invalidate();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopAndDisposeAutoHideTimer();
            _dismissRequest = null;
            _preferredHeightChanged = null;

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            DisposeTitleFont();
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private Button CreateDismissButton()
    {
        var button = new Button
        {
            AutoSize = false,
            Text = string.Empty,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Visible = true,
            TabStop = true,
            AccessibleRole = AccessibleRole.PushButton,
            AccessibleName = "Dismiss notification",
            AccessibleDescription = "Dismisses this notification."
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = Color.Transparent;
        button.FlatAppearance.MouseOverBackColor = Color.Transparent;
        button.Click += OnDismissButtonClick;
        button.Paint += OnDismissButtonPaint;
        return button;
    }

    private void OnDismissButtonClick(object? sender, EventArgs e)
    {
        Dismiss();
    }

    private void OnDismissButtonPaint(object? sender, PaintEventArgs e)
    {
        var button = _dismissButton;
        if (button is null || button.ClientSize.Width <= 0 || button.ClientSize.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var palette = BootstrapFeedbackRenderLogic.ResolvePalette(theme.Colors, _variant, Enabled);
        var inset = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        var glyphBounds = Rectangle.Inflate(button.ClientRectangle, -inset, -inset);
        if (glyphBounds.Width > 0 && glyphBounds.Height > 0)
        {
            _iconRenderer.TryRender(e.Graphics, CloseIcon, glyphBounds, palette.Foreground);
        }

        if (button.Focused)
        {
            var focusWidth = DpiScaler.Scale(theme.Metrics.FocusBorderWidth, dpi);
            var focusBounds = Rectangle.Inflate(button.ClientRectangle, -inset, -inset);
            focusBounds.Width = Math.Max(0, focusBounds.Width - 1);
            focusBounds.Height = Math.Max(0, focusBounds.Height - 1);
            if (focusWidth > 0 && focusBounds.Width > 0 && focusBounds.Height > 0)
            {
                using var focusPen = new Pen(palette.Focus, focusWidth);
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
        NotifyContentChanged();
        _dismissButton?.Invalidate();
    }

    private void ApplyTheme()
    {
        var button = _dismissButton;
        if (IsDisposed || button is null)
        {
            return;
        }

        var palette = BootstrapFeedbackRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme.Colors,
            _variant,
            Enabled);
        button.BackColor = palette.Surface;
        button.ForeColor = palette.Foreground;
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

    private void RebuildTitleFont()
    {
        if (Font is null)
        {
            return;
        }

        var next = new Font(Font, FontStyle.Bold);
        var previous = _titleFont;
        _titleFont = next;
        previous?.Dispose();
    }

    private void DisposeTitleFont()
    {
        var font = _titleFont;
        _titleFont = null;
        font?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private void NotifyContentChanged()
    {
        if (IsDisposed)
        {
            return;
        }

        PerformLayout();
        Invalidate();
        _preferredHeightChanged?.Invoke(this);
    }

    private BootstrapToastContentLayout CalculateCurrentLayout()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(theme.Metrics, dpi);
        MeasureTextSizes(Math.Max(1, Width), metrics, out var titleSize, out var bodySize);
        return BootstrapToastLayoutLogic.CalculateContentLayout(
            ClientRectangle,
            metrics,
            !string.IsNullOrEmpty(_title),
            _icon is not null,
            _dismissible,
            titleSize,
            bodySize);
    }

    private int CalculatePreferredHeight(int width)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(theme.Metrics, GetCurrentDpi());
        MeasureTextSizes(width, metrics, out var titleSize, out var bodySize);
        return BootstrapToastLayoutLogic.CalculatePreferredHeight(
            metrics,
            titleSize,
            bodySize,
            !string.IsNullOrEmpty(_title),
            _icon is not null,
            _dismissible);
    }

    private void MeasureTextSizes(int width, BootstrapToastMetrics metrics, out Size titleSize, out Size bodySize)
    {
        var textWidth = Math.Max(1, width - (metrics.HorizontalPadding * 2));
        if (_icon is not null)
        {
            textWidth = Math.Max(1, textWidth - metrics.IconSize - metrics.ContentSpacing);
        }

        if (_dismissible)
        {
            textWidth = Math.Max(1, textWidth - metrics.CloseButtonSize - metrics.ContentSpacing);
        }

        titleSize = string.IsNullOrEmpty(_title)
            ? Size.Empty
            : TextRenderer.MeasureText(
                _title,
                _titleFont ?? Font,
                new Size(textWidth, Math.Max(1, Font.Height * 2)),
                TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);

        bodySize = string.IsNullOrEmpty(Text)
            ? Size.Empty
            : TextRenderer.MeasureText(
                Text,
                Font,
                new Size(textWidth, 10000),
                TextFormatFlags.NoPrefix |
                TextFormatFlags.WordBreak |
                TextFormatFlags.NoPadding);
    }

    private void UpdateDismissButtonState()
    {
        var button = _dismissButton;
        if (button is null)
        {
            return;
        }

        button.Visible = _dismissible;
        button.Enabled = _dismissible && Enabled;
        button.TabStop = _dismissible && Enabled;
    }

    private void RestartAutoHideTimerIfEligible(bool resetDelay = true)
    {
        StopAndDisposeAutoHideTimer();
        if (resetDelay)
        {
            ResetAutoHideRemainingDelay();
        }

        if (!CanAutoHide())
        {
            return;
        }

        var generation = ++_autoHideGeneration;
        var timer = _timerFactory();
        if (timer is null)
        {
            throw new InvalidOperationException("The toast auto-hide timer factory returned null.");
        }

        EventHandler handler = (sender, args) => OnAutoHideTick(timer, generation, sender);
        _autoHideTimer = timer;
        _autoHideTickHandler = handler;
        timer.Interval = Math.Max(1, _autoHideRemainingDelay);
        timer.Tick += handler;
        _autoHideStartedTimestamp = Stopwatch.GetTimestamp();
        timer.Start();
    }

    private void PauseAutoHideTimer()
    {
        if (_autoHideTimer is null)
        {
            return;
        }

        if (_autoHideStartedTimestamp != 0)
        {
            var elapsedTicks = Math.Max(0L, Stopwatch.GetTimestamp() - _autoHideStartedTimestamp);
            var elapsedMilliseconds = (long)Math.Floor((elapsedTicks * 1000d) / Stopwatch.Frequency);
            _autoHideRemainingDelay = (int)Math.Max(1L, (long)_autoHideRemainingDelay - elapsedMilliseconds);
        }

        StopAndDisposeAutoHideTimer();
    }

    private void ResetAutoHideRemainingDelay()
    {
        _autoHideRemainingDelay = _autoHideDelay;
    }

    private bool CanAutoHide()
    {
        return _autoHide &&
               _dismissRequest is not null &&
               _enterCompleted &&
               _hostVisible &&
               Visible &&
               !IsDisposed;
    }

    private void OnAutoHideTick(IBootstrapToastAutoHideTimer timer, int generation, object? sender)
    {
        if (IsDisposed ||
            generation != _autoHideGeneration ||
            !ReferenceEquals(timer, _autoHideTimer) ||
            !ReferenceEquals(sender, timer) ||
            !CanAutoHide())
        {
            return;
        }

        StopAndDisposeAutoHideTimer();
        Dismiss();
    }

    private void StopAndDisposeAutoHideTimer()
    {
        _autoHideGeneration++;
        var timer = _autoHideTimer;
        var handler = _autoHideTickHandler;
        _autoHideTimer = null;
        _autoHideTickHandler = null;
        _autoHideStartedTimestamp = 0;
        if (timer is null)
        {
            return;
        }

        timer.Stop();
        if (handler is not null)
        {
            timer.Tick -= handler;
        }

        timer.Dispose();
    }

    private int GetCurrentDpi()
    {
        return DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
    }
}
