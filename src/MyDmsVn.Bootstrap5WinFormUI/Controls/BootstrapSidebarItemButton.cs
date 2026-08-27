using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Keeps BootstrapButton interaction/surface behavior while adding sidebar-specific content layout.
/// </summary>
internal sealed class BootstrapSidebarItemButton : BootstrapButton
{
    private string _displayText = string.Empty;
    private bool _suppressBaseText;
    private IconDescriptor? _navigationIcon;
    private IIconRenderer _navigationIconRenderer;
    private string _badgeText = string.Empty;
    private bool _hasChildren;
    private bool _sectionExpanded;
    private bool _collapsedMode;

    public BootstrapSidebarItemButton(IIconRenderer iconRenderer)
    {
        _navigationIconRenderer = iconRenderer ?? throw new ArgumentNullException(nameof(iconRenderer));
        AutoSize = false;
        Outline = true;
        ButtonSize = BootstrapButtonSize.Default;
        BorderRadius = -1;
        TextAlign = ContentAlignment.MiddleLeft;
    }

    public override string Text
    {
        get => _suppressBaseText ? string.Empty : _displayText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_displayText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _displayText = normalized;
            base.Text = normalized;
            Invalidate();
        }
    }

    public IconDescriptor? NavigationIcon
    {
        get => _navigationIcon;
        set
        {
            if (ReferenceEquals(_navigationIcon, value))
            {
                return;
            }

            _navigationIcon = value;
            Invalidate();
        }
    }

    public IIconRenderer NavigationIconRenderer
    {
        get => _navigationIconRenderer;
        set
        {
            _navigationIconRenderer = value ?? throw new ArgumentNullException(nameof(value));
            Invalidate();
        }
    }

    public string BadgeText
    {
        get => _badgeText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_badgeText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _badgeText = normalized;
            Invalidate();
        }
    }

    public bool HasChildren
    {
        get => _hasChildren;
        set
        {
            if (_hasChildren == value)
            {
                return;
            }

            _hasChildren = value;
            Invalidate();
        }
    }

    public bool SectionExpanded
    {
        get => _sectionExpanded;
        set
        {
            if (_sectionExpanded == value)
            {
                return;
            }

            _sectionExpanded = value;
            Invalidate();
        }
    }

    public bool CollapsedMode
    {
        get => _collapsedMode;
        set
        {
            if (_collapsedMode == value)
            {
                return;
            }

            _collapsedMode = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        _suppressBaseText = true;
        try
        {
            base.OnPaint(pevent);
        }
        finally
        {
            _suppressBaseText = false;
        }

        PaintSidebarContent(pevent.Graphics);
    }

    private void PaintSidebarContent(Graphics graphics)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var horizontalPadding = DpiScaler.Scale(theme.Metrics.SpacingMD, dpi);
        var contentGap = DpiScaler.Scale(theme.Metrics.SpacingSM, dpi);
        var iconExtent = DpiScaler.Scale(theme.Metrics.SpacingLG, dpi);
        var badgeHeight = Math.Max(14, DpiScaler.Scale(theme.Metrics.SpacingLG, dpi));
        var foreground = ResolveForeground(theme);
        var left = horizontalPadding;
        var right = Math.Max(left, ClientSize.Width - horizontalPadding);

        if (_navigationIcon is not null)
        {
            var iconBounds = new Rectangle(
                left,
                Math.Max(0, (ClientSize.Height - iconExtent) / 2),
                iconExtent,
                iconExtent);
            _navigationIconRenderer.TryRender(graphics, _navigationIcon, iconBounds, foreground);
            left = iconBounds.Right + contentGap;
        }

        if (_collapsedMode)
        {
            if (_navigationIcon is null)
            {
                PaintCollapsedInitial(graphics, theme, foreground);
            }

            return;
        }

        if (_hasChildren)
        {
            var chevronBounds = new Rectangle(
                Math.Max(left, right - iconExtent),
                Math.Max(0, (ClientSize.Height - iconExtent) / 2),
                iconExtent,
                iconExtent);
            PaintChevron(graphics, chevronBounds, foreground, theme, dpi);
            right = Math.Max(left, chevronBounds.Left - contentGap);
        }

        if (!string.IsNullOrEmpty(_badgeText))
        {
            var measured = TextRenderer.MeasureText(
                _badgeText,
                Font,
                Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            var badgeWidth = Math.Max(badgeHeight, measured.Width + (contentGap * 2));
            var badgeBounds = new Rectangle(
                Math.Max(left, right - badgeWidth),
                Math.Max(0, (ClientSize.Height - badgeHeight) / 2),
                badgeWidth,
                badgeHeight);
            PaintBadge(graphics, badgeBounds, theme, dpi);
            right = Math.Max(left, badgeBounds.Left - contentGap);
        }

        if (right > left && !string.IsNullOrEmpty(_displayText))
        {
            TextRenderer.DrawText(
                graphics,
                _displayText,
                Font,
                Rectangle.FromLTRB(left, 0, right, ClientSize.Height),
                foreground,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }
    }

    private Color ResolveForeground(BootstrapTheme theme)
    {
        return ResolveCurrentPalette(theme).Foreground;
    }

    private void PaintBadge(Graphics graphics, Rectangle bounds, BootstrapTheme theme, int dpi)
    {
        var fill = theme.Colors.Secondary;
        var foreground = ColorUtil.GetContrastingTextColor(fill, theme.Colors.Light, theme.Colors.Dark);
        var radius = Math.Max(1, bounds.Height / 2);
        using var path = RoundedPath.Create(bounds, new CornerRadius(radius));
        using var brush = new SolidBrush(fill);
        graphics.FillPath(brush, path);
        TextRenderer.DrawText(
            graphics,
            _badgeText,
            Font,
            bounds,
            foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPadding);
    }

    private void PaintChevron(Graphics graphics, Rectangle bounds, Color foreground, BootstrapTheme theme, int dpi)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var previous = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            var stroke = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.BorderWidth + 0.5f, dpi));
            var span = Math.Max(3f, Math.Min(bounds.Width, bounds.Height) * 0.45f);
            var centerX = bounds.Left + (bounds.Width / 2f);
            var centerY = bounds.Top + (bounds.Height / 2f);
            using var pen = new Pen(foreground, stroke)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            if (_sectionExpanded)
            {
                graphics.DrawLines(
                    pen,
                    new[]
                    {
                        new PointF(centerX - (span / 2f), centerY + (span / 4f)),
                        new PointF(centerX, centerY - (span / 4f)),
                        new PointF(centerX + (span / 2f), centerY + (span / 4f))
                    });
            }
            else
            {
                graphics.DrawLines(
                    pen,
                    new[]
                    {
                        new PointF(centerX - (span / 4f), centerY - (span / 2f)),
                        new PointF(centerX + (span / 4f), centerY),
                        new PointF(centerX - (span / 4f), centerY + (span / 2f))
                    });
            }
        }
        finally
        {
            graphics.SmoothingMode = previous;
        }
    }

    private void PaintCollapsedInitial(Graphics graphics, BootstrapTheme theme, Color foreground)
    {
        if (string.IsNullOrEmpty(AccessibleName))
        {
            return;
        }

        var initial = AccessibleName!.Substring(0, 1).ToUpperInvariant();
        TextRenderer.DrawText(
            graphics,
            initial,
            Font,
            ClientRectangle,
            foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPadding);
    }
}
