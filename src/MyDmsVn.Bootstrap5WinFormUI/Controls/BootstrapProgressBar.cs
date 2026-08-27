using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Displays Bootstrap-inspired determinate or indeterminate progress.
/// </summary>
[DefaultProperty(nameof(Value))]
public class BootstrapProgressBar : Control
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(600);

    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private Color _customColor = Color.Empty;
    private int _borderRadius = -1;
    private bool _showText;
    private string _textFormat = "{0}%";
    private bool _striped;
    private bool _animated;
    private TimeSpan _animationDuration = DefaultAnimationDuration;
    private bool _indeterminate;
    private BootstrapAnimation? _valueAnimation;
    private BootstrapLoopAnimation? _loopAnimation;
    private int _valueAnimationStart;
    private int _valueAnimationTarget;
    private bool _themeSubscribed;

    /// <summary>
    /// Initializes a designer-safe progress bar using the current application theme.
    /// </summary>
    public BootstrapProgressBar()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        SetStyle(ControlStyles.Selectable, false);

        BackColor = Color.Transparent;
        TabStop = false;
        Size = new Size(320, 16);
        AccessibleRole = AccessibleRole.ProgressBar;
        AccessibleDescription = "Displays operation progress.";

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    /// <summary>Gets or sets the minimum progress value.</summary>
    [Category("Behavior")]
    [Description("Specifies the minimum progress value. It must remain less than Maximum.")]
    [DefaultValue(0)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            if (value >= _maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Minimum must be less than Maximum.");
            }

            if (_minimum == value)
            {
                return;
            }

            CancelValueAnimation();
            _minimum = value;
            if (_value < _minimum)
            {
                _value = _minimum;
            }

            NotifyAccessibilityValueChanged();
            Invalidate();
        }
    }

    /// <summary>Gets or sets the maximum progress value.</summary>
    [Category("Behavior")]
    [Description("Specifies the maximum progress value. It must remain greater than Minimum.")]
    [DefaultValue(100)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            if (value <= _minimum)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum must be greater than Minimum.");
            }

            if (_maximum == value)
            {
                return;
            }

            CancelValueAnimation();
            _maximum = value;
            if (_value > _maximum)
            {
                _value = _maximum;
            }

            NotifyAccessibilityValueChanged();
            Invalidate();
        }
    }

    /// <summary>Gets or sets the current progress value.</summary>
    [Category("Behavior")]
    [Description("Specifies the current progress value inside the configured range.")]
    [DefaultValue(0)]
    public int Value
    {
        get => _value;
        set
        {
            ValidateValue(value, nameof(value));
            CancelValueAnimation();
            SetValueCore(value);
        }
    }

    /// <summary>Gets the current progress percentage rounded to the nearest whole percent.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Percentage => BootstrapProgressBarRenderLogic.GetPercentage(_minimum, _maximum, _value);

    /// <summary>Gets or sets the semantic fill variant.</summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired fill color.")]
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
    /// Gets or sets an optional fill color override. Color.Empty uses <see cref="Variant"/>.
    /// </summary>
    [Category("Appearance")]
    [Description("Overrides the semantic progress fill color when non-empty.")]
    public Color CustomColor
    {
        get => _customColor;
        set
        {
            if (_customColor == value)
            {
                return;
            }

            _customColor = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets the uniform logical corner radius, or -1 to use the theme default.</summary>
    [Category("Appearance")]
    [Description("Specifies a uniform logical radius, or -1 to use the theme radius.")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "BorderRadius must be -1 or non-negative.");
            }

            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether determinate progress text is rendered.</summary>
    [Category("Appearance")]
    [Description("Shows formatted determinate progress text when true.")]
    [DefaultValue(false)]
    public bool ShowText
    {
        get => _showText;
        set
        {
            if (_showText == value)
            {
                return;
            }

            _showText = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the composite format used for progress text. {0}=percentage, {1}=Value, {2}=Minimum, {3}=Maximum.
    /// </summary>
    [Category("Appearance")]
    [Description("Formats progress text using {0}=percentage, {1}=Value, {2}=Minimum, and {3}=Maximum.")]
    [DefaultValue("{0}%")]
    public string TextFormat
    {
        get => _textFormat;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            BootstrapProgressBarRenderLogic.FormatText(value, _minimum, _maximum, _value);
            if (string.Equals(_textFormat, value, StringComparison.Ordinal))
            {
                return;
            }

            _textFormat = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether the filled region uses diagonal stripes.</summary>
    [Category("Appearance")]
    [Description("Renders diagonal stripes inside the progress fill.")]
    [DefaultValue(false)]
    public bool Striped
    {
        get => _striped;
        set
        {
            if (_striped == value)
            {
                return;
            }

            _striped = value;
            UpdateLoopAnimation();
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether stripes move continuously while striped rendering is enabled.</summary>
    [Category("Behavior")]
    [Description("Animates stripes using the shared loop-animation infrastructure.")]
    [DefaultValue(false)]
    public bool Animated
    {
        get => _animated;
        set
        {
            if (_animated == value)
            {
                return;
            }

            _animated = value;
            UpdateLoopAnimation();
            Invalidate();
        }
    }

    /// <summary>Gets or sets the duration used by smooth value transitions and looped visuals.</summary>
    [Category("Behavior")]
    [Description("Specifies the duration of AnimateTo transitions and one stripe/indeterminate loop cycle.")]
    [DefaultValue(typeof(TimeSpan), "00:00:00.6000000")]
    public TimeSpan AnimationDuration
    {
        get => _animationDuration;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Animation duration must be greater than zero.");
            }

            if (_animationDuration == value)
            {
                return;
            }

            var pendingTarget = _valueAnimation is null ? (int?)null : _valueAnimationTarget;
            CancelValueAnimation();
            _animationDuration = value;
            RecreateLoopAnimation();

            if (pendingTarget.HasValue && pendingTarget.Value != _value && !_indeterminate && IsHandleCreated && !IsInDesignMode())
            {
                StartValueAnimation(pendingTarget.Value);
            }
        }
    }

    /// <summary>Gets or sets whether the control renders an indeterminate moving segment.</summary>
    [Category("Behavior")]
    [Description("Displays an indeterminate activity segment instead of a determinate percentage fill.")]
    [DefaultValue(false)]
    public bool Indeterminate
    {
        get => _indeterminate;
        set
        {
            if (_indeterminate == value)
            {
                return;
            }

            if (value)
            {
                CancelValueAnimation();
            }

            _indeterminate = value;
            UpdateLoopAnimation();
            NotifyAccessibilityValueChanged();
            Invalidate();
        }
    }

    /// <summary>
    /// Smoothly transitions the current value to <paramref name="value"/> using shared finite animation.
    /// In indeterminate mode, or before a runtime handle exists, the logical value is updated immediately.
    /// </summary>
    public void AnimateTo(int value)
    {
        ValidateValue(value, nameof(value));
        CancelValueAnimation();

        if (_value == value)
        {
            return;
        }

        if (_indeterminate || !IsHandleCreated || IsInDesignMode())
        {
            SetValueCore(value);
            return;
        }

        StartValueAnimation(value);
    }

    /// <inheritdoc />
    protected override AccessibleObject CreateAccessibilityInstance()
    {
        return new BootstrapProgressBarAccessibleObject(this);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateLoopAnimation();
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        CancelValueAnimation();
        DisposeLoopAnimation();
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
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
        var trackBounds = new RectangleF(0f, 0f, ClientSize.Width, ClientSize.Height);
        var radius = BootstrapProgressBarRenderLogic.ResolveRadius(theme.Metrics, _borderRadius, dpi);
        var corners = new CornerRadius(radius);
        var fillColor = BootstrapProgressBarRenderLogic.ResolveFillColor(theme.Colors, _variant, _customColor);
        var loopProgress = ResolveLoopProgress(theme);
        var fillBounds = _indeterminate
            ? BootstrapProgressBarRenderLogic.GetIndeterminateFillBounds(trackBounds, loopProgress)
            : BootstrapProgressBarRenderLogic.GetDeterminateFillBounds(
                trackBounds,
                BootstrapProgressBarRenderLogic.GetFraction(_minimum, _maximum, _value));

        var previousSmoothingMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var trackPath = RoundedPath.Create(trackBounds, corners);
            using (var trackBrush = new SolidBrush(theme.Colors.SurfaceSecondary))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
            }

            using var fillPath = RoundedPath.Create(fillBounds, corners);
            if (fillBounds.Width > 0f && fillBounds.Height > 0f)
            {
                var clipState = e.Graphics.Save();
                try
                {
                    e.Graphics.SetClip(trackPath, CombineMode.Intersect);
                    using var fillBrush = new SolidBrush(fillColor);
                    e.Graphics.FillPath(fillBrush, fillPath);

                    if (_striped)
                    {
                        PaintStripes(e.Graphics, fillPath, fillBounds, fillColor, theme, dpi, loopProgress);
                    }
                }
                finally
                {
                    e.Graphics.Restore(clipState);
                }
            }

            if (_showText && !_indeterminate)
            {
                PaintText(e.Graphics, fillPath, fillBounds.Width > 0f, fillColor, theme);
            }
        }
        finally
        {
            e.Graphics.SmoothingMode = previousSmoothingMode;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelValueAnimation();
            DisposeLoopAnimation();

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
        }

        base.Dispose(disposing);
    }

    private void StartValueAnimation(int target)
    {
        _valueAnimationStart = _value;
        _valueAnimationTarget = target;

        var animation = new BootstrapAnimation(
            _animationDuration,
            BootstrapEasing.EaseInOut,
            this);
        _valueAnimation = animation;
        animation.ProgressChanged += OnValueAnimationProgressChanged;
        animation.Completed += OnValueAnimationCompleted;
        animation.Start();
    }

    private void OnValueAnimationProgressChanged(object? sender, EventArgs e)
    {
        if (sender is not BootstrapAnimation animation || !ReferenceEquals(animation, _valueAnimation))
        {
            return;
        }

        SetValueCore(BootstrapProgressBarRenderLogic.InterpolateValue(
            _valueAnimationStart,
            _valueAnimationTarget,
            animation.Progress));
    }

    private void OnValueAnimationCompleted(object? sender, EventArgs e)
    {
        if (sender is not BootstrapAnimation animation || !ReferenceEquals(animation, _valueAnimation))
        {
            return;
        }

        SetValueCore(_valueAnimationTarget);
        DisposeValueAnimation();
    }

    private void CancelValueAnimation()
    {
        var animation = _valueAnimation;
        if (animation is null)
        {
            return;
        }

        if (animation.IsRunning)
        {
            animation.Stop();
        }

        DisposeValueAnimation();
    }

    private void DisposeValueAnimation()
    {
        var animation = _valueAnimation;
        if (animation is null)
        {
            return;
        }

        _valueAnimation = null;
        animation.ProgressChanged -= OnValueAnimationProgressChanged;
        animation.Completed -= OnValueAnimationCompleted;
        animation.Dispose();
    }

    private void UpdateLoopAnimation()
    {
        if (IsDisposed || !IsHandleCreated || IsInDesignMode())
        {
            return;
        }

        if (!ShouldRunLoopAnimation())
        {
            DisposeLoopAnimation();
            return;
        }

        if (_loopAnimation is null)
        {
            _loopAnimation = new BootstrapLoopAnimation(
                _animationDuration,
                BootstrapEasing.Linear,
                this);
            _loopAnimation.ProgressChanged += OnLoopAnimationProgressChanged;
        }

        if (!_loopAnimation.IsRunning)
        {
            _loopAnimation.Start();
        }
    }

    private void RecreateLoopAnimation()
    {
        DisposeLoopAnimation();
        UpdateLoopAnimation();
    }

    private void DisposeLoopAnimation()
    {
        var animation = _loopAnimation;
        if (animation is null)
        {
            return;
        }

        _loopAnimation = null;
        animation.ProgressChanged -= OnLoopAnimationProgressChanged;
        animation.Dispose();
    }

    private void OnLoopAnimationProgressChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _loopAnimation) && !IsDisposed)
        {
            Invalidate();
        }
    }

    private bool ShouldRunLoopAnimation()
    {
        return _indeterminate || (_striped && _animated);
    }

    private double ResolveLoopProgress(BootstrapTheme theme)
    {
        if (_indeterminate && theme.ReducedMotion)
        {
            return 0.5;
        }

        if (_loopAnimation is not null)
        {
            return _loopAnimation.Progress;
        }

        return _indeterminate ? 0.5 : 0.0;
    }

    private void PaintStripes(
        Graphics graphics,
        GraphicsPath fillPath,
        RectangleF fillBounds,
        Color fillColor,
        BootstrapTheme theme,
        int dpi,
        double loopProgress)
    {
        var stripeWidth = Math.Max(2f, DpiScaler.Scale((float)theme.Metrics.SpacingSM, dpi));
        var stripeSpan = stripeWidth * 2f;
        var offset = _animated ? (float)(loopProgress * stripeSpan) : 0f;
        var stripeBase = ColorUtil.GetContrastingTextColor(fillColor, theme.Colors.Light, theme.Colors.Dark);
        var stripeColor = Color.FromArgb(48, stripeBase);
        var state = graphics.Save();
        try
        {
            graphics.SetClip(fillPath, CombineMode.Intersect);
            using var stripeBrush = new SolidBrush(stripeColor);
            var start = fillBounds.Left - fillBounds.Height - stripeSpan + offset;
            var end = fillBounds.Right + fillBounds.Height + stripeSpan;

            for (var x = start; x < end; x += stripeSpan)
            {
                var points = new[]
                {
                    new PointF(x, fillBounds.Bottom),
                    new PointF(x + stripeWidth, fillBounds.Bottom),
                    new PointF(x + stripeWidth + fillBounds.Height, fillBounds.Top),
                    new PointF(x + fillBounds.Height, fillBounds.Top)
                };
                graphics.FillPolygon(stripeBrush, points);
            }
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private void PaintText(
        Graphics graphics,
        GraphicsPath fillPath,
        bool hasFill,
        Color fillColor,
        BootstrapTheme theme)
    {
        var text = BootstrapProgressBarRenderLogic.FormatText(
            _textFormat,
            _minimum,
            _maximum,
            _value);
        if (text.Length == 0)
        {
            return;
        }

        var flags = TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPadding;
        TextRenderer.DrawText(graphics, text, Font, ClientRectangle, theme.Colors.Text, flags);

        if (!hasFill)
        {
            return;
        }

        var state = graphics.Save();
        try
        {
            graphics.SetClip(fillPath, CombineMode.Intersect);
            var fillTextColor = ColorUtil.GetContrastingTextColor(fillColor, theme.Colors.Light, theme.Colors.Dark);
            TextRenderer.DrawText(graphics, text, Font, ClientRectangle, fillTextColor, flags);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private void SetValueCore(int value)
    {
        if (_value == value)
        {
            return;
        }

        _value = value;
        NotifyAccessibilityValueChanged();
        Invalidate();
    }

    private void NotifyAccessibilityValueChanged()
    {
        if (IsHandleCreated)
        {
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
        }
    }

    private void ValidateValue(int value, string parameterName)
    {
        if (value < _minimum || value > _maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be inside the configured range.");
        }
    }

    private static void ValidateVariant(BootstrapVariant value)
    {
        if (value < BootstrapVariant.Primary || value > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported Bootstrap variant.");
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (e.OldTheme.ReducedMotion != e.NewTheme.ReducedMotion)
        {
            var pendingTarget = _valueAnimation is null ? (int?)null : _valueAnimationTarget;
            CancelValueAnimation();
            RecreateLoopAnimation();

            if (pendingTarget.HasValue && pendingTarget.Value != _value && !_indeterminate && IsHandleCreated && !IsInDesignMode())
            {
                StartValueAnimation(pendingTarget.Value);
            }
        }

        Invalidate();
    }

    private bool IsInDesignMode()
    {
        return DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }

    private sealed class BootstrapProgressBarAccessibleObject : Control.ControlAccessibleObject
    {
        private readonly BootstrapProgressBar _owner;

        public BootstrapProgressBarAccessibleObject(BootstrapProgressBar owner)
            : base(owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public override string? Value
        {
            get => _owner.Indeterminate ? "Indeterminate" : $"{_owner.Percentage}%";
            set { }
        }
    }
}
