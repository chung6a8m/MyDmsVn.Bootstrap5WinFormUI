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
/// Displays a decorative Bootstrap-inspired loading placeholder.
/// </summary>
[DefaultProperty(nameof(Animation))]
public class BootstrapPlaceholder : Control
{
    private BootstrapPlaceholderSize _placeholderSize = BootstrapPlaceholderSize.Default;
    private BootstrapPlaceholderAnimation _animation = BootstrapPlaceholderAnimation.None;
    private BootstrapVariant _variant = BootstrapVariant.Secondary;
    private Color _customColor = Color.Empty;
    private int _borderRadius;
    private TimeSpan _animationDuration = TimeSpan.FromSeconds(2);
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;
    private BootstrapLoopAnimation? _animationLoop;

    /// <summary>
    /// Initializes a designer-safe placeholder using the current theme's body typography.
    /// </summary>
    public BootstrapPlaceholder()
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
        AccessibleRole = AccessibleRole.None;
        Cursor = Cursors.WaitCursor;

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyPreferredSize();
    }

    /// <summary>
    /// Gets or sets the Bootstrap-compatible intrinsic height.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapPlaceholderSize.Default)]
    public BootstrapPlaceholderSize PlaceholderSize
    {
        get => _placeholderSize;
        set
        {
            ValidatePlaceholderSize(value);
            if (_placeholderSize == value)
            {
                return;
            }

            _placeholderSize = value;
            ApplyPreferredSize();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the static, Glow, or Wave presentation.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapPlaceholderAnimation.None)]
    public BootstrapPlaceholderAnimation Animation
    {
        get => _animation;
        set
        {
            ValidateAnimation(value);
            if (_animation == value)
            {
                return;
            }

            _animation = value;
            RecreateAnimationLoop();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the semantic color used when <see cref="CustomColor"/> is empty.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapVariant.Secondary)]
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
    /// Gets or sets an optional opaque color that overrides <see cref="Variant"/>.
    /// </summary>
    [Category("Appearance")]
    public Color CustomColor
    {
        get => _customColor;
        set
        {
            ValidateCustomColor(value);
            if (_customColor == value)
            {
                return;
            }

            _customColor = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets a logical corner radius. Use -1 for the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(0)]
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
    /// Gets or sets the duration of one Glow or Wave cycle.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(typeof(TimeSpan), "00:00:02")]
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
            RecreateAnimationLoop();
            Invalidate();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return BootstrapPlaceholderRenderLogic.GetPreferredSize(Font.Height, _placeholderSize, dpi, proposedSize);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyPreferredSize();
        ReconcileAnimationLoop();
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_animationLoop is not null && _animationLoop.IsRunning)
        {
            _animationLoop.Stop();
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
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
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
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var baseColor = BootstrapPlaceholderRenderLogic.ResolveBaseColor(theme.Colors, _variant, _customColor, Enabled);
        var radius = BootstrapPlaceholderRenderLogic.GetRadius(theme.Metrics, _borderRadius, dpi);
        var progress = _animationLoop?.Progress ?? 0.0;

        using var brush = CreateFillBrush(baseColor, progress);
        if (radius <= 0f)
        {
            e.Graphics.FillRectangle(brush, ClientRectangle);
            return;
        }

        var previousSmoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(ClientRectangle, new CornerRadius(radius));
            e.Graphics.FillPath(brush, path);
        }
        finally
        {
            e.Graphics.SmoothingMode = previousSmoothing;
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
            DisposeAnimationLoop();
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
        RecreateAnimationLoop();
        Invalidate();
    }

    private Brush CreateFillBrush(Color baseColor, double progress)
    {
        if (_animation == BootstrapPlaceholderAnimation.Wave)
        {
            var positions = new[] { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f, 1f };
            var colors = new Color[positions.Length];
            for (var index = 0; index < positions.Length; index++)
            {
                var sampleOpacity = BootstrapPlaceholderRenderLogic.GetWaveOpacity(positions[index], progress);
                colors[index] = BootstrapPlaceholderRenderLogic.ApplyOpacity(baseColor, sampleOpacity);
            }

            var blend = new ColorBlend(positions.Length)
            {
                Positions = positions,
                Colors = colors
            };
            var brush = new LinearGradientBrush(
                ClientRectangle,
                Color.Transparent,
                Color.Transparent,
                BootstrapPlaceholderRenderLogic.WaveAngleDegrees);
            brush.InterpolationColors = blend;
            return brush;
        }

        var opacity = _animation == BootstrapPlaceholderAnimation.Glow
            ? BootstrapPlaceholderRenderLogic.GetGlowOpacity(progress)
            : BootstrapPlaceholderRenderLogic.OpacityMax;
        return new SolidBrush(BootstrapPlaceholderRenderLogic.ApplyOpacity(baseColor, opacity));
    }

    private void OnAnimationProgressChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed)
        {
            Invalidate();
        }
    }

    private void ReconcileAnimationLoop()
    {
        if (_animation == BootstrapPlaceholderAnimation.None)
        {
            DisposeAnimationLoop();
            return;
        }

        if (!IsHandleCreated || IsInDesignMode())
        {
            return;
        }

        if (_animationLoop is null)
        {
            _animationLoop = new BootstrapLoopAnimation(_animationDuration, BootstrapEasing.Linear, this);
            _animationLoop.ProgressChanged += OnAnimationProgressChanged;
        }

        _animationLoop.Start();
    }

    private void RecreateAnimationLoop()
    {
        DisposeAnimationLoop();
        ReconcileAnimationLoop();
    }

    private void DisposeAnimationLoop()
    {
        if (_animationLoop is null)
        {
            return;
        }

        _animationLoop.ProgressChanged -= OnAnimationProgressChanged;
        _animationLoop.Dispose();
        _animationLoop = null;
    }

    private bool IsInDesignMode()
    {
        return DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }

    private void ApplyThemeFont()
    {
        if (!_useThemeFont)
        {
            return;
        }

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

    private static void ValidatePlaceholderSize(BootstrapPlaceholderSize value)
    {
        if (value < BootstrapPlaceholderSize.ExtraSmall || value > BootstrapPlaceholderSize.Large)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported placeholder size.");
        }
    }

    private static void ValidateAnimation(BootstrapPlaceholderAnimation value)
    {
        if (value < BootstrapPlaceholderAnimation.None || value > BootstrapPlaceholderAnimation.Wave)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported placeholder animation.");
        }
    }

    private static void ValidateVariant(BootstrapVariant value)
    {
        if (value < BootstrapVariant.Primary || value > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported Bootstrap variant.");
        }
    }

    private static void ValidateCustomColor(Color value)
    {
        if (!value.IsEmpty && value.A != byte.MaxValue)
        {
            throw new ArgumentException("Custom color must be Color.Empty or fully opaque.", nameof(value));
        }
    }
}
