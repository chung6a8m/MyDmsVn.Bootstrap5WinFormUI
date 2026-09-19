using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapModalHeader : Panel
{
    private readonly Label _titleLabel;
    private readonly Button _closeButton;
    private BootstrapModalMetrics _metrics;
    private Font? _themeFont;

    public BootstrapModalHeader()
    {
        Dock = DockStyle.Top;
        TabStop = false;
        _titleLabel = new Label { Name = "BootstrapModalTitle", Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, TabStop = false };
        _closeButton = new Button
        {
            Name = "BootstrapModalClose",
            Dock = DockStyle.Right,
            Text = "×",
            AccessibleName = "Close",
            FlatStyle = FlatStyle.Flat,
            TabStop = false
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += OnCloseClick;
        Controls.Add(_titleLabel);
        Controls.Add(_closeButton);
    }

    public event EventHandler? DismissRequested;
    public string Title { get => _titleLabel.Text; set => _titleLabel.Text = value; }
    public bool ShowCloseButton { get => _closeButton.Visible; set => _closeButton.Visible = value; }

    public void ApplyMetrics(BootstrapModalMetrics metrics)
    {
        _metrics = metrics;
        Height = metrics.HeaderHeight;
        Padding = new Padding(metrics.Padding, 0, metrics.Gap, 0);
        _closeButton.Width = metrics.CloseTargetSize;
        Invalidate();
    }

    public void ApplyVisualState(BootstrapModalVisualState state)
    {
        BackColor = state.Surface;
        ForeColor = state.Text;
        _titleLabel.BackColor = state.Surface;
        _titleLabel.ForeColor = state.Text;
        _closeButton.BackColor = state.Surface;
        _closeButton.ForeColor = state.MutedText;
    }

    public void ApplyTypography(BootstrapThemeTypography typography)
    {
        var token = typography.HeadingSmall;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = next;
        _titleLabel.Font = next;
        previous?.Dispose();
    }

    public void ApplyRightToLeft(bool rtl)
    {
        RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
        _titleLabel.TextAlign = rtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _closeButton.Dock = rtl ? DockStyle.Left : DockStyle.Right;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(BootstrapThemeManager.CurrentTheme.Colors.Border, Math.Max(1, _metrics.BorderWidth));
        e.Graphics.DrawLine(pen, 0, Height - _metrics.BorderWidth, Width, Height - _metrics.BorderWidth);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closeButton.Click -= OnCloseClick;
            _themeFont?.Dispose();
            _themeFont = null;
        }
        base.Dispose(disposing);
    }

    private void OnCloseClick(object? sender, EventArgs e) => DismissRequested?.Invoke(this, EventArgs.Empty);
}
