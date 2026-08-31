using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapToastHostWindow : Form, IBootstrapToastHostWindow
{
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private readonly BootstrapToastContainer _toastContainer;
    private bool _retiring;
    private bool _disposing;
    private bool _regionRefreshPending;
    private bool _hadOwnedToasts;

    public BootstrapToastHostWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        ControlBox = false;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = false;

        _toastContainer = new BootstrapToastContainer
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        _toastContainer.ControlAdded += OnToastControlAdded;
        _toastContainer.ControlRemoved += OnToastControlRemoved;
        Controls.Add(_toastContainer);
    }

    public string ScreenDeviceName { get; private set; } = string.Empty;

    public bool HasOwnedToasts => _toastContainer.Controls.OfType<BootstrapToast>().Any();

    public event EventHandler? BecameEmpty;

    internal BootstrapToastContainer ToastContainer => _toastContainer;

    internal bool ShowWithoutActivationForTests => ShowWithoutActivation;

    internal int CreateParamsExStyleForTests => CreateParams.ExStyle;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    public void ApplySettings(BootstrapToastScreenInfo screen, BootstrapToastHostSettings settings)
    {
        ThrowIfDisposed();
        if (_retiring)
        {
            throw new InvalidOperationException("A retiring Toast host cannot be reconfigured.");
        }

        ScreenDeviceName = screen.DeviceName;
        _toastContainer.Placement = settings.Placement;
        _toastContainer.ToastSpacing = settings.ToastSpacing;
        _toastContainer.MaximumVisibleToasts = settings.MaximumVisibleToasts;
        TopMost = settings.TopMost;
        Bounds = BootstrapToastServiceLayoutLogic.InsetWorkingArea(screen.WorkingArea, settings.ScreenMargin, screen.Dpi);
        UpdateMaximumStackHeight();
        ScheduleRegionRefresh();
    }

    public void ShowToast(BootstrapToast toast)
    {
        ThrowIfDisposed();
        if (_retiring)
        {
            throw new InvalidOperationException("A retiring Toast host cannot accept notifications.");
        }

        _toastContainer.ShowToast(toast);
        _hadOwnedToasts = true;
        if (!Visible)
        {
            Show();
        }

        ScheduleRegionRefresh();
    }

    public void DismissAll()
    {
        if (_disposing || IsDisposed)
        {
            return;
        }

        _toastContainer.DismissAll();
    }

    public void RetireForScreenRemoval()
    {
        ThrowIfDisposed();
        if (_retiring)
        {
            return;
        }

        _retiring = true;
        Hide();
        ClearOwnedRegion();
        _toastContainer.DismissAll();
        HandlePotentialEmptyTransition();
    }

    internal void RefreshRegionNowForTests()
    {
        _regionRefreshPending = false;
        RefreshHostRegion();
    }

    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        if (!_disposing && !IsDisposed)
        {
            UpdateMaximumStackHeight();
            ScheduleRegionRefresh();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            _regionRefreshPending = false;
            _toastContainer.ControlAdded -= OnToastControlAdded;
            _toastContainer.ControlRemoved -= OnToastControlRemoved;
            foreach (var toast in _toastContainer.Controls.OfType<BootstrapToast>().ToArray())
            {
                UnsubscribeToastGeometry(toast);
            }

            ClearOwnedRegion();
        }

        base.Dispose(disposing);
    }

    private void OnToastControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is BootstrapToast toast)
        {
            SubscribeToastGeometry(toast);
            _hadOwnedToasts = true;
            ScheduleRegionRefresh();
        }
    }

    private void OnToastControlRemoved(object? sender, ControlEventArgs e)
    {
        if (e.Control is BootstrapToast toast)
        {
            UnsubscribeToastGeometry(toast);
        }

        HandlePotentialEmptyTransition();
        ScheduleRegionRefresh();
    }

    private void SubscribeToastGeometry(BootstrapToast toast)
    {
        toast.LocationChanged += OnToastGeometryChanged;
        toast.SizeChanged += OnToastGeometryChanged;
        toast.VisibleChanged += OnToastGeometryChanged;
    }

    private void UnsubscribeToastGeometry(BootstrapToast toast)
    {
        toast.LocationChanged -= OnToastGeometryChanged;
        toast.SizeChanged -= OnToastGeometryChanged;
        toast.VisibleChanged -= OnToastGeometryChanged;
    }

    private void OnToastGeometryChanged(object? sender, EventArgs e)
    {
        ScheduleRegionRefresh();
    }

    private void HandlePotentialEmptyTransition()
    {
        if (!_hadOwnedToasts || HasOwnedToasts)
        {
            return;
        }

        _hadOwnedToasts = false;
        Hide();
        ClearOwnedRegion();
        BecameEmpty?.Invoke(this, EventArgs.Empty);
    }

    private void ScheduleRegionRefresh()
    {
        if (_disposing || IsDisposed || _regionRefreshPending)
        {
            return;
        }

        if (!IsHandleCreated)
        {
            return;
        }

        _regionRefreshPending = true;
        try
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                _regionRefreshPending = false;
                if (!_disposing && !IsDisposed)
                {
                    RefreshHostRegion();
                }
            }));
        }
        catch (InvalidOperationException)
        {
            _regionRefreshPending = false;
        }
    }

    private void RefreshHostRegion()
    {
        if (_disposing || IsDisposed)
        {
            return;
        }

        var visibleToasts = _toastContainer.Controls
            .OfType<BootstrapToast>()
            .Where(toast => toast.Visible && !toast.IsDisposed)
            .ToArray();
        if (visibleToasts.Length == 0)
        {
            if (!HasOwnedToasts)
            {
                Hide();
            }

            ClearOwnedRegion();
            return;
        }

        var dpi = _toastContainer.DeviceDpi > 0 ? _toastContainer.DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapToastLayoutLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, dpi);
        var verticalEnvelope = Math.Max(0, DpiScaler.Scale(_toastContainer.ToastSpacing, dpi) / 2);
        Region? next = null;
        foreach (var toast in visibleToasts)
        {
            var bounds = toast.Bounds;
            bounds.Inflate(metrics.SlideDistance, verticalEnvelope);
            bounds.Intersect(_toastContainer.ClientRectangle);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                continue;
            }

            if (next is null)
            {
                next = new Region(bounds);
            }
            else
            {
                next.Union(bounds);
            }
        }

        ReplaceOwnedRegion(next);
    }

    private void ReplaceOwnedRegion(Region? next)
    {
        var previous = Region;
        Region = next;
        if (!ReferenceEquals(previous, next))
        {
            previous?.Dispose();
        }
    }

    private void ClearOwnedRegion()
    {
        ReplaceOwnedRegion(null);
    }

    private void UpdateMaximumStackHeight()
    {
        if (_toastContainer.IsDisposed)
        {
            return;
        }

        _toastContainer.MaximumStackHeightPixels = Math.Max(1, ClientSize.Height);
    }

    private void ThrowIfDisposed()
    {
        if (_disposing || IsDisposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapToastHostWindow));
        }
    }
}
