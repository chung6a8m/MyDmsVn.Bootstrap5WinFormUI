using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapOverlaySurface : Panel
{
    private BootstrapTheme _theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    private Padding _logicalContentPadding = new Padding(12, 8, 12, 8);
    private int _logicalBorderRadius = -1;
    private int _dpi = DpiScaler.DefaultDpi;
    private int _borderWidth = 1;
    private int _radius = 6;
    private Region? _ownedRegion;

    public BootstrapOverlaySurface()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        BackColor = _theme.Colors.Surface;
    }

    public Control? HostedContent { get; private set; }

    public Padding LogicalContentPadding
    {
        get => _logicalContentPadding;
        set
        {
            ValidatePadding(value);
            if (_logicalContentPadding == value)
            {
                return;
            }

            _logicalContentPadding = value;
            PerformLayout();
        }
    }

    public int LogicalBorderRadius
    {
        get => _logicalBorderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or non-negative.");
            }

            if (_logicalBorderRadius == value)
            {
                return;
            }

            _logicalBorderRadius = value;
            ResolveMetrics();
            ReplaceRegion();
            Invalidate();
        }
    }

    public void AttachContent(Control content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (content.IsDisposed)
        {
            throw new ArgumentException("Overlay content cannot be disposed.", nameof(content));
        }

        if (HostedContent is not null && !ReferenceEquals(HostedContent, content))
        {
            throw new InvalidOperationException("Detach the existing overlay content before attaching another control.");
        }

        if (content.Parent is not null && !ReferenceEquals(content.Parent, this))
        {
            throw new InvalidOperationException("Overlay content must be unparented when attached.");
        }

        if (ReferenceEquals(HostedContent, content))
        {
            return;
        }

        HostedContent = content;
        Controls.Add(content);
        PerformLayout();
    }

    public Control? DetachContent()
    {
        var content = HostedContent;
        if (content is null)
        {
            return null;
        }

        Controls.Remove(content);
        HostedContent = null;
        PerformLayout();
        return content;
    }

    public void ApplyTheme(BootstrapTheme theme, int dpi)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be positive.");
        }

        _dpi = dpi;
        BackColor = theme.Colors.Surface;
        ResolveMetrics();
        PerformLayout();
        ReplaceRegion();
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var contentSize = HostedContent?.GetPreferredSize(Size.Empty) ?? Size.Empty;
        var padding = DpiScaler.Scale(_logicalContentPadding, _dpi);
        return new Size(
            SaturateSize((long)Math.Max(0, contentSize.Width) + padding.Horizontal + (2L * _borderWidth)),
            SaturateSize((long)Math.Max(0, contentSize.Height) + padding.Vertical + (2L * _borderWidth)));
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var content = HostedContent;
        if (content is null)
        {
            return;
        }

        var padding = DpiScaler.Scale(_logicalContentPadding, _dpi);
        var x = Math.Min(ClientSize.Width, padding.Left + _borderWidth);
        var y = Math.Min(ClientSize.Height, padding.Top + _borderWidth);
        var width = Math.Max(0, ClientSize.Width - x - padding.Right - _borderWidth);
        var height = Math.Max(0, ClientSize.Height - y - padding.Bottom - _borderWidth);
        content.Bounds = new Rectangle(x, y, width, height);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ReplaceRegion();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Transparent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var borderInset = _borderWidth / 2f;
        var bounds = new RectangleF(
            borderInset,
            borderInset,
            Math.Max(0f, ClientSize.Width - _borderWidth),
            Math.Max(0f, ClientSize.Height - _borderWidth));
        var previous = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            using var path = RoundedPath.Create(bounds, new CornerRadius(_radius));
            using var background = new SolidBrush(_theme.Colors.Surface);
            e.Graphics.FillPath(background, path);
            if (_borderWidth > 0)
            {
                using var border = new Pen(_theme.Colors.Border, _borderWidth);
                e.Graphics.DrawPath(border, path);
            }
        }
        finally
        {
            e.Graphics.SmoothingMode = previous;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Region = null;
            _ownedRegion?.Dispose();
            _ownedRegion = null;
        }

        base.Dispose(disposing);
    }

    private void ResolveMetrics()
    {
        _borderWidth = Math.Max(0, DpiScaler.Scale(_theme.Metrics.BorderWidth, _dpi));
        var logicalRadius = _logicalBorderRadius < 0 ? _theme.Metrics.Radius : _logicalBorderRadius;
        _radius = Math.Max(0, DpiScaler.Scale(logicalRadius, _dpi));
    }

    private void ReplaceRegion()
    {
        Region? next = null;
        if (ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            using var path = RoundedPath.Create(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), new CornerRadius(_radius));
            next = new Region(path);
        }

        var previous = _ownedRegion;
        _ownedRegion = next;
        Region = next;
        previous?.Dispose();
    }

    private static int SaturateSize(long value)
    {
        return value >= int.MaxValue ? int.MaxValue : (int)Math.Max(0L, value);
    }

    private static void ValidatePadding(Padding padding)
    {
        if (padding.Left < 0 || padding.Top < 0 || padding.Right < 0 || padding.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), padding, "Overlay content padding cannot contain negative edges.");
        }
    }
}
