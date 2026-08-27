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
/// Displays an animated Bootstrap-inspired indicator for ongoing activity.
/// </summary>
[DefaultProperty(nameof(Type))]
public class BootstrapSpinner : Control
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(750);

    private BootstrapSpinnerType _type = BootstrapSpinnerType.Border;
    private BootstrapSpinnerSize _spinnerSize = BootstrapSpinnerSize.Default;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private Color _customColor = Color.Empty;
    private TimeSpan _animationDuration = DefaultAnimationDuration;
    private bool _spinning = true;
    private BootstrapLoopAnimation? _animation;
    private bool _themeSubscribed;

    /// <summary>
    /// Initializes a designer-safe spinner using the current application theme.
    /// </summary>
    public BootstrapSpinner()
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
        AccessibleRole = AccessibleRole.Animation;
        AccessibleDescription = "Indicates ongoing activity.";

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyPreferredSize();
    }

    /// <summary>
    /// Gets or sets whether the spinner renders as a rotating border or a growing pulse.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the Border or Grow spinner rendering mode.")]
    [DefaultValue(BootstrapSpinnerType.Border)]
    public BootstrapSpinnerType Type
    {
        get => _type;
        set
        {
            ValidateType(value);
            if (_type == value)
            {
                return;
            }

            _type = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the standard spinner size.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the Small, Default, or Large spinner size.")]
    [DefaultValue(BootstrapSpinnerSize.Default)]
    public BootstrapSpinnerSize SpinnerSize
    {
        get => _spinnerSize;
        set
        {
            ValidateSpinnerSize(value);
            if (_spinnerSize == value)
            {
                return;
            }

            _spinnerSize = value;
            ApplyPreferredSize();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the semantic color variant used when <see cref="CustomColor"/> is empty.
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
    /// Gets or sets an optional color that overrides <see cref="Variant"/>.
    /// Use <see cref="Color.Empty"/> to use the current theme's semantic variant color.
    /// </summary>
    [Category("Appearance")]
    [Description("Overrides the semantic variant color when set to a non-empty color.")]
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

    /// <summary>
    /// Gets or sets the duration of one animation cycle.
    /// </summary>
    [Category("Behavior")]
    [Description("Specifies the duration of one spinner animation cycle.")]
    [DefaultValue(typeof(TimeSpan), "00:00:00.7500000")]
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

            _animationDuration = value;
            RecreateAnimation();
        }
    }

    /// <summary>
    /// Gets or sets whether the spinner is logically active.
    /// Reduced-motion themes keep an active spinner on a stable visible frame without continuous scheduling.
    /// </summary>
    [Category("Behavior")]
    [Description("Gets or sets whether spinner animation is active.")]
    [DefaultValue(true)]
    public bool Spinning
    {
        get => _spinning;
        set
        {
            if (_spinning == value)
            {
                return;
            }

            _spinning = value;
            if (IsHandleCreated && !IsInDesignMode())
            {
                EnsureAnimation();
                if (_spinning)
                {
                    _animation!.Start();
                }
                else
                {
                    _animation!.Stop();
                }
            }

            Invalidate();
        }
    }

    /// <summary>
    /// Starts spinner animation. If reduced motion is enabled, the spinner remains on a stable visible frame.
    /// </summary>
    public void Start()
    {
        Spinning = true;
    }

    /// <summary>
    /// Stops spinner animation and preserves the current visual frame.
    /// </summary>
    public void Stop()
    {
        Spinning = false;
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var logicalDiameter = BootstrapSpinnerRenderLogic.GetLogicalDiameter(theme.Metrics, _spinnerSize);
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var diameter = DpiScaler.Scale(logicalDiameter, dpi);
        return new Size(diameter, diameter);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyPreferredSize();

        if (_spinning && !IsInDesignMode())
        {
            EnsureAnimation();
            _animation!.Start();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_animation is not null && _animation.IsRunning)
        {
            _animation.Stop();
        }

        base.OnHandleDestroyed(e);
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
        base.OnPaint(e);

        var diameter = Math.Min(ClientSize.Width, ClientSize.Height);
        if (diameter <= 1)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var color = BootstrapSpinnerRenderLogic.ResolveColor(theme.Colors, _variant, _customColor);
        var progress = _animation?.Progress ?? 0.0;

        var previousSmoothingMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            if (_type == BootstrapSpinnerType.Grow)
            {
                PaintGrow(e.Graphics, diameter, color, progress);
            }
            else
            {
                PaintBorder(e.Graphics, diameter, color, progress);
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
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            DisposeAnimation();
        }

        base.Dispose(disposing);
    }

    private void PaintBorder(Graphics graphics, int diameter, Color color, double progress)
    {
        var strokeWidth = Math.Max(1f, diameter * 0.125f);
        var arcDiameter = diameter - strokeWidth - 1f;
        if (arcDiameter <= 0f)
        {
            return;
        }

        var bounds = new RectangleF(
            (ClientSize.Width - arcDiameter) / 2f,
            (ClientSize.Height - arcDiameter) / 2f,
            arcDiameter,
            arcDiameter);

        using var pen = new Pen(color, strokeWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        var startAngle = (float)((progress * 360.0) - 90.0);
        graphics.DrawArc(pen, bounds, startAngle, 280f);
    }

    private void PaintGrow(Graphics graphics, int diameter, Color color, double progress)
    {
        var scale = BootstrapSpinnerRenderLogic.GetGrowScale(progress);
        var pulse = (scale - 0.65) / 0.35;
        var alpha = (int)Math.Round(150.0 + (105.0 * pulse), MidpointRounding.AwayFromZero);
        var scaledDiameter = Math.Max(1f, (float)(diameter * scale));
        var bounds = new RectangleF(
            (ClientSize.Width - scaledDiameter) / 2f,
            (ClientSize.Height - scaledDiameter) / 2f,
            scaledDiameter,
            scaledDiameter);

        using var brush = new SolidBrush(Color.FromArgb(alpha, color));
        graphics.FillEllipse(brush, bounds);
    }

    private void OnAnimationProgressChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed)
        {
            Invalidate();
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        ApplyPreferredSize();
        RecreateAnimation();
        Invalidate();
    }

    private void EnsureAnimation()
    {
        if (_animation is not null)
        {
            return;
        }

        _animation = new BootstrapLoopAnimation(
            _animationDuration,
            BootstrapEasing.Linear,
            this);
        _animation.ProgressChanged += OnAnimationProgressChanged;
    }

    private void RecreateAnimation()
    {
        DisposeAnimation();

        if (!IsHandleCreated || IsInDesignMode())
        {
            return;
        }

        EnsureAnimation();
        if (_spinning)
        {
            _animation!.Start();
        }

        Invalidate();
    }

    private void DisposeAnimation()
    {
        if (_animation is null)
        {
            return;
        }

        _animation.ProgressChanged -= OnAnimationProgressChanged;
        _animation.Dispose();
        _animation = null;
    }

    private void ApplyPreferredSize()
    {
        if (!AutoSize)
        {
            return;
        }

        var preferredSize = GetPreferredSize(Size.Empty);
        if (Size != preferredSize)
        {
            Size = preferredSize;
        }
    }

    private bool IsInDesignMode()
    {
        return DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }

    private static void ValidateType(BootstrapSpinnerType value)
    {
        if (value != BootstrapSpinnerType.Border && value != BootstrapSpinnerType.Grow)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported spinner type.");
        }
    }

    private static void ValidateSpinnerSize(BootstrapSpinnerSize value)
    {
        if (value != BootstrapSpinnerSize.Small &&
            value != BootstrapSpinnerSize.Default &&
            value != BootstrapSpinnerSize.Large)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported spinner size.");
        }
    }

    private static void ValidateVariant(BootstrapVariant value)
    {
        if (value < BootstrapVariant.Primary || value > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported Bootstrap variant.");
        }
    }
}
