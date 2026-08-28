using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-inspired owner-drawn tooltip presentation while preserving native WinForms tooltip association, timing, popup, and lifecycle behavior.
/// </summary>
[ProvideProperty("ToolTip", typeof(Control))]
public class BootstrapTooltip : Component, IExtenderProvider
{
    private const TextFormatFlags ToolTipTextFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

    private readonly ToolTip _toolTip;
    private BootstrapVariant _variant = BootstrapVariant.Dark;
    private Color _customColor = Color.Empty;
    private int _borderRadius = -1;
    private Padding _contentPadding = CreateDefaultContentPadding();

    /// <summary>
    /// Initializes a designer-safe tooltip extender that owns one native WinForms <see cref="ToolTip"/> instance.
    /// </summary>
    public BootstrapTooltip()
    {
        _toolTip = new ToolTip
        {
            OwnerDraw = true,
            IsBalloon = false
        };
        _toolTip.Popup += OnToolTipPopup;
        _toolTip.Draw += OnToolTipDraw;
    }

    /// <summary>
    /// Initializes a tooltip extender and adds this wrapper component to the supplied container.
    /// The wrapper retains sole ownership of its inner native tooltip.
    /// </summary>
    /// <param name="container">The component container that owns this wrapper.</param>
    public BootstrapTooltip(IContainer container)
        : this()
    {
        if (container is null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        container.Add(this);
    }

    /// <summary>
    /// Gets or sets the semantic Bootstrap-inspired tooltip background variant.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired tooltip background variant.")]
    [DefaultValue(BootstrapVariant.Dark)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            ValidateVariant(value);
            _variant = value;
        }
    }

    /// <summary>
    /// Gets or sets a custom tooltip background color. <see cref="Color.Empty"/> uses <see cref="Variant"/>.
    /// </summary>
    [Category("Appearance")]
    [Description("Overrides the semantic tooltip background color; Color.Empty uses Variant.")]
    [DefaultValue(typeof(Color), "Empty")]
    public Color CustomColor
    {
        get => _customColor;
        set => _customColor = value;
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

            _borderRadius = value;
        }
    }

    /// <summary>
    /// Gets or sets logical tooltip content padding. Negative edge values are not allowed.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets logical content padding around tooltip text.")]
    [DefaultValue(typeof(Padding), "8, 4, 8, 4")]
    public Padding ContentPadding
    {
        get => _contentPadding;
        set
        {
            ValidatePadding(value);
            _contentPadding = value;
        }
    }

    /// <summary>
    /// Gets or sets the native delay, in milliseconds, before the tooltip first appears.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(500)]
    public int InitialDelay
    {
        get => _toolTip.InitialDelay;
        set => _toolTip.InitialDelay = value;
    }

    /// <summary>
    /// Gets or sets the native delay, in milliseconds, before subsequent tooltips appear while moving between associated controls.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(100)]
    public int ReshowDelay
    {
        get => _toolTip.ReshowDelay;
        set => _toolTip.ReshowDelay = value;
    }

    /// <summary>
    /// Gets or sets the native duration, in milliseconds, for which a tooltip remains visible when the pointer is stationary.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(5000)]
    public int AutoPopDelay
    {
        get => _toolTip.AutoPopDelay;
        set => _toolTip.AutoPopDelay = value;
    }

    /// <summary>
    /// Gets or sets whether the native tooltip service is active.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Active
    {
        get => _toolTip.Active;
        set => _toolTip.Active = value;
    }

    /// <summary>
    /// Gets or sets whether tooltips are shown even when the parent window is inactive.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool ShowAlways
    {
        get => _toolTip.ShowAlways;
        set => _toolTip.ShowAlways = value;
    }

    /// <summary>
    /// Returns whether this extender can provide tooltip text to the supplied object.
    /// </summary>
    /// <param name="extendee">The candidate extended object.</param>
    public bool CanExtend(object extendee)
    {
        return extendee is Control;
    }

    /// <summary>
    /// Associates tooltip text with a WinForms control using the owned native tooltip as the single source of truth.
    /// An empty caption removes the native association.
    /// </summary>
    /// <param name="control">The control to associate with tooltip text.</param>
    /// <param name="caption">The tooltip text. Explicit newline characters are preserved.</param>
    public void SetToolTip(Control control, string caption)
    {
        if (control is null)
        {
            throw new ArgumentNullException(nameof(control));
        }

        if (caption is null)
        {
            throw new ArgumentNullException(nameof(caption));
        }

        _toolTip.SetToolTip(control, caption);
    }

    /// <summary>
    /// Gets the tooltip text associated with a WinForms control from the owned native tooltip.
    /// </summary>
    /// <param name="control">The associated control.</param>
    /// <returns>The native tooltip caption, or an empty string when no caption is associated.</returns>
    public string GetToolTip(Control control)
    {
        if (control is null)
        {
            throw new ArgumentNullException(nameof(control));
        }

        return _toolTip.GetToolTip(control) ?? string.Empty;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Popup -= OnToolTipPopup;
            _toolTip.Draw -= OnToolTipDraw;
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnToolTipPopup(object? sender, PopupEventArgs e)
    {
        var associatedControl = e.AssociatedControl;
        if (associatedControl is null)
        {
            return;
        }

        var caption = _toolTip.GetToolTip(associatedControl);
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetControlDpi(associatedControl);
        using var font = CreateFont(theme.Typography.BodySmall);
        var measuredText = TextRenderer.MeasureText(caption, font, Size.Empty, ToolTipTextFlags);
        var metrics = BootstrapTooltipRenderLogic.ResolveMetrics(theme.Metrics, _contentPadding, _borderRadius, dpi);
        e.ToolTipSize = BootstrapTooltipRenderLogic.CalculatePopupSize(measuredText, metrics);
    }

    private void OnToolTipDraw(object? sender, DrawToolTipEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetControlDpi(e.AssociatedControl);
        var palette = BootstrapTooltipRenderLogic.ResolvePalette(theme.Colors, _variant, _customColor);
        var metrics = BootstrapTooltipRenderLogic.ResolveMetrics(theme.Metrics, _contentPadding, _borderRadius, dpi);
        var borderWidth = Math.Max(0f, metrics.BorderWidth);
        var borderInset = borderWidth / 2f;
        var surfaceBounds = new RectangleF(
            e.Bounds.X + borderInset,
            e.Bounds.Y + borderInset,
            Math.Max(0f, e.Bounds.Width - borderWidth),
            Math.Max(0f, e.Bounds.Height - borderWidth));
        if (surfaceBounds.Width <= 0f || surfaceBounds.Height <= 0f)
        {
            return;
        }

        var previousSmoothingMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(surfaceBounds, new CornerRadius(metrics.Radius));
            using var backgroundBrush = new SolidBrush(palette.Background);
            e.Graphics.FillPath(backgroundBrush, path);

            if (metrics.BorderWidth > 0)
            {
                using var borderPen = new Pen(palette.Border, borderWidth);
                e.Graphics.DrawPath(borderPen, path);
            }
        }
        finally
        {
            e.Graphics.SmoothingMode = previousSmoothingMode;
        }

        var textBounds = BootstrapTooltipRenderLogic.CalculateTextBounds(e.Bounds, metrics);
        if (textBounds.Width <= 0 || textBounds.Height <= 0)
        {
            return;
        }

        using var font = CreateFont(theme.Typography.BodySmall);
        TextRenderer.DrawText(
            e.Graphics,
            e.ToolTipText ?? string.Empty,
            font,
            textBounds,
            palette.Foreground,
            ToolTipTextFlags);
    }

    private static Padding CreateDefaultContentPadding()
    {
        var metrics = BootstrapThemeMetrics.Default;
        return new Padding(metrics.SpacingSM, metrics.SpacingXS, metrics.SpacingSM, metrics.SpacingXS);
    }

    private static Font CreateFont(BootstrapFontToken token)
    {
        return new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
    }

    private static int GetControlDpi(Control? control)
    {
        return control is not null && control.DeviceDpi > 0
            ? control.DeviceDpi
            : DpiScaler.DefaultDpi;
    }

    private static void ValidateVariant(BootstrapVariant variant)
    {
        if (variant < BootstrapVariant.Primary || variant > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported Bootstrap variant.");
        }
    }

    private static void ValidatePadding(Padding padding)
    {
        if (padding.Left < 0 || padding.Top < 0 || padding.Right < 0 || padding.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), padding, "Tooltip content padding cannot contain negative edges.");
        }
    }
}
