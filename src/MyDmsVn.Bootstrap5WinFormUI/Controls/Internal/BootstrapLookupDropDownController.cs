using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupDropDownController : IDisposable, IMessageFilter
{
    private readonly BootstrapLookupBox _owner;
    private readonly BootstrapLookupDropDownContent _content;
    private BootstrapOverlaySurface? _surface;
    private BootstrapOverlayDropDown? _dropDown;
    private BootstrapOverlayAnchorTracker? _tracker;
    private bool _isOpen;
    private bool _disposed;
    private int _activationGeneration;
    private int _queuedWindowDeactivationGeneration = -1;
    private bool _messageFilterInstalled;

    internal BootstrapLookupDropDownController(BootstrapLookupBox owner, BootstrapLookupDropDownContent content)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _content.ResultsGrid.CellMouseClick += OnResultCellMouseClick;
        _content.RefreshRequested += OnRefreshRequested;
        _content.AddNewRequested += OnAddNewRequested;
    }

    internal bool IsOpen => _isOpen;

    internal void Open()
    {
        ThrowIfDisposed();
        if (_owner.IsDisposed || !_owner.Enabled || !_owner.Visible || !_owner.IsHandleCreated) return;
        EnsureCreated();
        ApplyPresentation();
        if (_isOpen)
        {
            Reposition();
            _owner.FocusLookupEditor();
            return;
        }
        _isOpen = true;
        _activationGeneration++;
        _dropDown!.ShowAt(ComputeBounds());
        _owner.SynchronizeHighlightedResult();
        if (!_messageFilterInstalled)
        {
            Application.AddMessageFilter(this);
            _messageFilterInstalled = true;
        }
        _tracker = new BootstrapOverlayAnchorTracker(_owner, Reposition, () => Close(false));
        _owner.FocusLookupEditor();
    }

    internal void Close(bool restoreFocus)
    {
        if (_disposed || !_isOpen) return;
        _dropDown?.Close(ToolStripDropDownCloseReason.CloseCalled);
        if (_dropDown is null || !_dropDown.Visible) CompleteClose();
        if (restoreFocus) _owner.FocusLookupEditor();
    }

    internal void Reposition()
    {
        if (!_isOpen || _dropDown is null || _dropDown.IsDisposed || !_owner.IsHandleCreated) return;
        _dropDown.MoveTo(ComputeBounds());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _isOpen = false;
        RemoveMessageFilter();
        _tracker?.Dispose();
        _tracker = null;
        _content.ResultsGrid.CellMouseClick -= OnResultCellMouseClick;
        _content.RefreshRequested -= OnRefreshRequested;
        _content.AddNewRequested -= OnAddNewRequested;
        if (_dropDown is not null)
        {
            _dropDown.ApplicationDeactivated -= OnApplicationDeactivated;
            _dropDown.WindowDeactivated -= OnWindowDeactivated;
            _dropDown.Closed -= OnDropDownClosed;
        }
        _surface?.DetachContent();
        _dropDown?.Dispose();
        _dropDown = null;
        _surface = null;
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (!_isOpen || !IsPointerDownMessage(m.Msg)) return false;
        var target = Control.FromChildHandle(m.HWnd);
        if (IsWithin(target, _owner) || IsWithin(target, _dropDown) || IsWithin(target, _surface) || IsWithin(target, _content)) return false;
        Close(false);
        return false;
    }

    private void EnsureCreated()
    {
        if (_dropDown is not null) return;
        _surface = new BootstrapOverlaySurface { LogicalContentPadding = Padding.Empty, LogicalBorderRadius = _owner.BorderRadius };
        _surface.AttachContent(_content);
        _dropDown = new BootstrapOverlayDropDown(_surface) { AutoClose = false, CloseOnEscape = true, EscapeRequested = _owner.CancelPendingEdit };
        _dropDown.ApplicationDeactivated += OnApplicationDeactivated;
        _dropDown.WindowDeactivated += OnWindowDeactivated;
        _dropDown.Closed += OnDropDownClosed;
        ApplyPresentation();
    }

    private void ApplyPresentation()
    {
        if (_surface is null) return;
        var dpi = ResolveDpi();
        _surface.LogicalBorderRadius = _owner.BorderRadius;
        _surface.ApplyTheme(BootstrapThemeManager.CurrentTheme, dpi);
        _content.Font = _owner.Font;
    }

    private Rectangle ComputeBounds()
    {
        var dpi = ResolveDpi();
        var requestedWidth = _owner.DropDownWidth == 0 ? _owner.Width : DpiScaler.Scale(_owner.DropDownWidth, dpi);
        var width = Math.Max(_owner.Width, requestedWidth);
        var maxHeight = DpiScaler.Scale(_owner.MaxDropDownHeight, dpi);
        var preferred = _surface!.GetPreferredSize(new Size(width, maxHeight));
        var desiredHeight = Math.Min(maxHeight, Math.Max(DpiScaler.Scale(64, dpi), preferred.Height));
        var anchor = _owner.RectangleToScreen(_owner.ClientRectangle);
        var request = new BootstrapOverlayPlacementRequest(anchor, new Size(width, desiredHeight), Screen.FromControl(_owner).WorkingArea,
            BootstrapOverlayPlacement.BottomStart, BootstrapOverlayCollisionBehavior.FlipAndShift,
            DpiScaler.Scale(4, dpi), DpiScaler.Scale(8, dpi), _owner.RightToLeft == RightToLeft.Yes);
        return BootstrapOverlayPlacementEngine.Compute(request).Bounds;
    }

    private int ResolveDpi() => _owner.DeviceDpi > 0 ? _owner.DeviceDpi : DpiScaler.DefaultDpi;

    private void OnResultCellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || e.RowIndex < 0) return;
        var sourceItem = _content.GetSourceItem(e.RowIndex);
        if (sourceItem is null) return;
        _owner.CommitSelection(sourceItem.Item, sourceItem.Value, sourceItem.DisplayText, BootstrapLookupCommitReason.Mouse);
        Close(true);
    }

    private void OnRefreshRequested(object? sender, EventArgs e) => _owner.RefreshResults();
    private void OnAddNewRequested(object? sender, EventArgs e) => _owner.RequestExplicitAddNew();
    private void OnApplicationDeactivated(object? sender, EventArgs e) => Close(false);
    private void OnWindowDeactivated(IntPtr activatedWindow)
    {
        if (_disposed || !_isOpen) return;
        var ownerForm = _owner.FindForm();
        if (BootstrapOverlayActivationDomain.IsOwnerWindow(activatedWindow, ownerForm) ||
            BootstrapOverlayActivationDomain.IsPopupWindow(activatedWindow, _dropDown, _surface)) return;
        if (activatedWindow != IntPtr.Zero) { Close(false); return; }
        QueueWindowDeactivationCheck();
    }

    private void QueueWindowDeactivationCheck()
    {
        if (_dropDown?.IsHandleCreated != true || _dropDown.IsDisposed) return;
        var generation = _activationGeneration;
        if (_queuedWindowDeactivationGeneration == generation) return;
        _queuedWindowDeactivationGeneration = generation;
        try
        {
            _dropDown.BeginInvoke((Action)(() =>
            {
                if (_queuedWindowDeactivationGeneration == generation) _queuedWindowDeactivationGeneration = -1;
                if (_disposed || !_isOpen || generation != _activationGeneration) return;
                var popupActive = _dropDown?.ContainsFocus == true || _surface?.ContainsFocus == true || _content.ContainsFocus;
                var ownerForm = _owner.FindForm();
                var ownerActive = ownerForm?.IsHandleCreated == true && (ownerForm.ContainsFocus || Form.ActiveForm == ownerForm);
                if (!popupActive && !ownerActive) Close(false);
            }));
        }
        catch (ObjectDisposedException) { _queuedWindowDeactivationGeneration = -1; }
        catch (InvalidOperationException) { _queuedWindowDeactivationGeneration = -1; }
    }
    private void OnDropDownClosed(object? sender, ToolStripDropDownClosedEventArgs e) => CompleteClose();

    private void CompleteClose()
    {
        if (!_isOpen) return;
        _activationGeneration++;
        _queuedWindowDeactivationGeneration = -1;
        _isOpen = false;
        RemoveMessageFilter();
        _tracker?.Dispose();
        _tracker = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BootstrapLookupDropDownController));
    }

    private void RemoveMessageFilter()
    {
        if (!_messageFilterInstalled) return;
        Application.RemoveMessageFilter(this);
        _messageFilterInstalled = false;
    }

    private static bool IsWithin(Control? candidate, Control? ancestor)
    {
        while (candidate is not null)
        {
            if (ReferenceEquals(candidate, ancestor)) return true;
            candidate = candidate.Parent;
        }
        return false;
    }

    private static bool IsPointerDownMessage(int message) => message == 0x0201 || message == 0x0204 ||
        message == 0x0207 || message == 0x020B || message == 0x00A1 || message == 0x00A4 ||
        message == 0x00A7 || message == 0x00AB || message == 0x0246;
}
