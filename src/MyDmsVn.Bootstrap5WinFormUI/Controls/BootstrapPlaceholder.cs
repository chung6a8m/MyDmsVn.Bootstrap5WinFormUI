using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
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
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private Font? _themeFont;

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

        ApplyThemeFont();
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
            Invalidate();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        return base.GetPreferredSize(proposedSize);
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

        Invalidate();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeThemeFont();
        }

        base.Dispose(disposing);
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
