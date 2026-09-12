using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-aware presentation while retaining the native <see cref="TrackBar"/> contract.
/// </summary>
public class BootstrapRange : TrackBar
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _themeSubscribed;
    private bool _thumbHot;
    private bool _thumbPressed;

    /// <summary>Initializes a new instance of the <see cref="BootstrapRange"/> class.</summary>
    public BootstrapRange()
    {
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemePresentation();
    }

    /// <summary>Gets or sets the semantic variant used for the range thumb.</summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic variant used for the range thumb.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            Invalidate();
        }
    }

    internal BootstrapRangeVisualState CurrentVisualState => new BootstrapRangeVisualState(
        Enabled,
        Focused,
        _thumbHot,
        _thumbPressed);

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!IsDisposed && !Disposing)
        {
            ApplyThemePresentation();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        ClearTransientState(invalidate: false);
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        SetThumbHot(BootstrapRangeNativeMethods.GetThumbRectangle(Handle).Contains(e.Location));
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        SetThumbHot(false);
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        var pressedThumb = e.Button == MouseButtons.Left &&
            IsHandleCreated &&
            BootstrapRangeNativeMethods.GetThumbRectangle(Handle).Contains(e.Location);
        base.OnMouseDown(e);
        SetThumbPressed(pressedThumb);
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        SetThumbPressed(false);
    }

    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture)
        {
            SetThumbPressed(false);
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateThumb();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateThumb();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled)
        {
            ClearTransientState(invalidate: false);
        }

        InvalidateThumb();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        if (!IsDisposed && !Disposing)
        {
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        if (TryHandleCustomDraw(ref m))
        {
            return;
        }

        base.WndProc(ref m);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _themeSubscribed)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _themeSubscribed = false;
        }

        if (disposing)
        {
            ClearTransientState(invalidate: false);
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        ApplyThemePresentation();
        Invalidate();
    }

    private void ApplyThemePresentation()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
    }

    private bool TryHandleCustomDraw(ref Message message)
    {
        if (message.Msg != BootstrapRangeNativeMethods.WmReflectNotify ||
            !IsHandleCreated ||
            !BootstrapRangeNativeMethods.TryReadCustomDraw(message.LParam, Handle, out var customDraw))
        {
            return false;
        }

        if (customDraw.DrawStage == BootstrapRangeNativeMethods.CddsPrePaint)
        {
            message.Result = new IntPtr(BootstrapRangeNativeMethods.CdrfNotifyItemDraw);
            return true;
        }

        if (customDraw.DrawStage != BootstrapRangeNativeMethods.CddsItemPrePaint)
        {
            return false;
        }

        var part = BootstrapRangeNativeMethods.ClassifyPart(customDraw.ItemSpec);
        if (part == BootstrapRangeNativePart.Ticks)
        {
            if (TickStyle == TickStyle.None || !PaintTicks(customDraw))
            {
                return false;
            }

            message.Result = new IntPtr(BootstrapRangeNativeMethods.CdrfSkipDefault);
            return true;
        }

        if (part != BootstrapRangeNativePart.Channel && part != BootstrapRangeNativePart.Thumb)
        {
            return false;
        }

        PaintNativePart(customDraw, part);
        message.Result = new IntPtr(BootstrapRangeNativeMethods.CdrfSkipDefault);
        return true;
    }

    private bool PaintTicks(BootstrapRangeNativeCustomDraw customDraw)
    {
        if (customDraw.DeviceContext == IntPtr.Zero)
        {
            return false;
        }

        var channelBounds = BootstrapRangeNativeMethods.GetChannelRectangle(Handle);
        var positions = BootstrapRangeNativeMethods.GetIntermediateTickPositions(Handle);
        var theme = BootstrapThemeManager.CurrentTheme;
        var ticks = BootstrapRangeRenderLogic.CalculateTicks(
            ClientRectangle,
            channelBounds,
            Orientation,
            TickStyle,
            positions,
            theme.Metrics,
            DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi);
        if (ticks.Count == 0)
        {
            return false;
        }

        var palette = BootstrapRangeRenderLogic.ResolvePalette(theme.Colors, _variant, CurrentVisualState);
        using var graphics = Graphics.FromHdc(customDraw.DeviceContext);
        using var brush = new SolidBrush(palette.TickColor);
        for (var index = 0; index < ticks.Count; index++)
        {
            graphics.FillRectangle(brush, ticks[index]);
        }

        return true;
    }

    private void PaintNativePart(BootstrapRangeNativeCustomDraw customDraw, BootstrapRangeNativePart part)
    {
        if (customDraw.DeviceContext == IntPtr.Zero || customDraw.Bounds.IsEmpty)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var visualState = CurrentVisualState;
        var palette = BootstrapRangeRenderLogic.ResolvePalette(theme.Colors, _variant, visualState);
        var geometry = BootstrapRangeRenderLogic.CalculateGeometry(
            part == BootstrapRangeNativePart.Channel ? customDraw.Bounds : Rectangle.Empty,
            part == BootstrapRangeNativePart.Thumb ? customDraw.Bounds : Rectangle.Empty,
            Orientation,
            theme.Metrics,
            DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi,
            palette.DrawFocusHalo);

        using var graphics = Graphics.FromHdc(customDraw.DeviceContext);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (part == BootstrapRangeNativePart.Channel)
        {
            PaintRail(graphics, geometry, palette);
        }
        else
        {
            PaintThumb(graphics, geometry, palette);
        }
    }

    private static void PaintRail(Graphics graphics, BootstrapRangeGeometry geometry, BootstrapRangePalette palette)
    {
        if (geometry.RailBounds.IsEmpty)
        {
            return;
        }

        using var brush = new SolidBrush(palette.RailColor);
        using var path = RoundedPath.Create(geometry.RailBounds, new CornerRadius(geometry.RailRadius));
        graphics.FillPath(brush, path);
    }

    private static void PaintThumb(Graphics graphics, BootstrapRangeGeometry geometry, BootstrapRangePalette palette)
    {
        if (geometry.ThumbBounds.IsEmpty)
        {
            return;
        }

        if (palette.DrawFocusHalo && !geometry.FocusHaloBounds.IsEmpty)
        {
            using var focusPen = new Pen(palette.FocusColor, geometry.FocusThickness);
            var focusBounds = RectangleF.Inflate(
                geometry.FocusHaloBounds,
                -geometry.FocusThickness / 2f,
                -geometry.FocusThickness / 2f);
            if (focusBounds.Width > 0f && focusBounds.Height > 0f)
            {
                graphics.DrawEllipse(focusPen, focusBounds);
            }
        }

        using var thumbBrush = new SolidBrush(palette.ThumbColor);
        graphics.FillEllipse(thumbBrush, geometry.ThumbBounds);
    }

    private void SetThumbHot(bool value)
    {
        if (_thumbHot == value)
        {
            return;
        }

        _thumbHot = value;
        InvalidateThumb();
    }

    private void SetThumbPressed(bool value)
    {
        if (_thumbPressed == value)
        {
            return;
        }

        _thumbPressed = value;
        InvalidateThumb();
    }

    private void ClearTransientState(bool invalidate)
    {
        var changed = _thumbHot || _thumbPressed;
        _thumbHot = false;
        _thumbPressed = false;
        if (invalidate && changed)
        {
            InvalidateThumb();
        }
    }

    private void InvalidateThumb()
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        var bounds = BootstrapRangeNativeMethods.GetThumbRectangle(Handle);
        var inflation = DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.SpacingXS, DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi);
        bounds.Inflate(inflation, inflation);
        Invalidate(Rectangle.Intersect(ClientRectangle, bounds));
    }
}
