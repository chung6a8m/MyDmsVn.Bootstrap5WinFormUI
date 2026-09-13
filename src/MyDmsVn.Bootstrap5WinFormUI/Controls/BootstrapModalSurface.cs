using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapModalSurface : Panel
{
    private readonly BootstrapModalHeader _header;
    private BootstrapModalMetrics _metrics;
    private BootstrapModalVisualState _state;

    public BootstrapModalSurface(BootstrapModalHeader header)
    {
        _header = header;
        Dock = DockStyle.Fill;
        TabStop = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BodyPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, TabStop = false };
        FooterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = false,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            TabStop = false,
            Visible = false
        };
        FooterPanel.ControlAdded += OnFooterControlAdded;
        FooterPanel.ControlRemoved += OnFooterControlRemoved;
        Controls.Add(BodyPanel);
        Controls.Add(FooterPanel);
        Controls.Add(_header);
    }

    public Panel BodyPanel { get; }
    public FlowLayoutPanel FooterPanel { get; }

    public void ApplyMetrics(BootstrapModalMetrics metrics)
    {
        _metrics = metrics;
        Padding = new Padding(metrics.BorderWidth);
        BodyPanel.Padding = new Padding(metrics.Padding);
        FooterPanel.Height = metrics.FooterHeight;
        FooterPanel.Padding = new Padding(metrics.Padding, Math.Max(0, (metrics.FooterHeight - metrics.CloseTargetSize) / 2), metrics.Padding, 0);
        Invalidate();
    }

    public void ApplyVisualState(BootstrapModalVisualState state)
    {
        _state = state;
        BackColor = state.Surface;
        ForeColor = state.Text;
        BodyPanel.BackColor = state.Surface;
        BodyPanel.ForeColor = state.Text;
        FooterPanel.BackColor = state.Surface;
        FooterPanel.ForeColor = state.Text;
        Invalidate();
    }

    public int ResolveChromeAndContentHeight()
    {
        UpdateFooterVisibility();
        var footer = FooterPanel.Visible ? _metrics.FooterHeight : 0;
        var preferredBody = BodyPanel.Controls.Count == 0 ? _metrics.EmptyBodyHeight : BodyPanel.GetPreferredSize(new Size(Width, 0)).Height + _metrics.Padding * 2;
        return _metrics.HeaderHeight + footer + preferredBody + _metrics.BorderWidth * 2;
    }

    public void RefreshFooterVisibility() => UpdateFooterVisibility();

    protected override void OnLayout(LayoutEventArgs levent)
    {
        UpdateFooterVisibility();
        base.OnLayout(levent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        var inset = _metrics.BorderWidth / 2f;
        var bounds = new RectangleF(inset, inset, Math.Max(0, Width - _metrics.BorderWidth), Math.Max(0, Height - _metrics.BorderWidth));
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var path = RoundedPath.Create(bounds, new CornerRadius(_metrics.Radius));
        var previous = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(_state.Surface);
        using var pen = new Pen(_state.Border, Math.Max(1, _metrics.BorderWidth));
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
        e.Graphics.SmoothingMode = previous;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FooterPanel.ControlAdded -= OnFooterControlAdded;
            FooterPanel.ControlRemoved -= OnFooterControlRemoved;
            foreach (Control control in FooterPanel.Controls)
            {
                control.VisibleChanged -= OnFooterChildVisibleChanged;
            }
        }
        base.Dispose(disposing);
    }

    private void OnFooterControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is null) return;
        e.Control.VisibleChanged += OnFooterChildVisibleChanged;
        UpdateFooterVisibility();
    }

    private void OnFooterControlRemoved(object? sender, ControlEventArgs e)
    {
        if (e.Control is null) return;
        e.Control.VisibleChanged -= OnFooterChildVisibleChanged;
        UpdateFooterVisibility();
    }

    private void OnFooterChildVisibleChanged(object? sender, System.EventArgs e) => UpdateFooterVisibility();

    private void UpdateFooterVisibility()
    {
        if (FooterPanel.Controls.Count == 0)
        {
            FooterPanel.Visible = false;
            return;
        }

        FooterPanel.Visible = true;
        var hasVisibleActions = FooterPanel.Controls.Cast<Control>().Any(control => control.Visible);
        if (FooterPanel.Visible != hasVisibleActions) FooterPanel.Visible = hasVisibleActions;
    }
}
