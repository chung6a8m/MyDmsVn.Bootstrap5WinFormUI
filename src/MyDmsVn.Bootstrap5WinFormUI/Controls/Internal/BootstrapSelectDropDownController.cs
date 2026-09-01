using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectDropDownController : IDisposable
{
    private readonly BootstrapSelect _owner;
    private BootstrapOverlaySurface? _surface;
    private BootstrapOverlayDropDown? _dropDown;
    private BootstrapSelectDropDownContent? _content;
    private BootstrapOverlayAnchorTracker? _tracker;
    private bool _isOpen;
    private bool _disposed;
    private bool _restoreFocusOnClosed;
    private Rectangle _currentBounds;
    private int _creationCount;

    internal BootstrapSelectDropDownController(BootstrapSelect owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        owner.Items.Changed += OnItemsChanged;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
    }

    internal bool IsCreated => _dropDown is not null;
    internal bool IsOpen => _isOpen;
    internal int CreationCount => _creationCount;
    internal Rectangle CurrentBounds => _currentBounds;
    internal IntPtr DropDownHandle => _dropDown?.IsHandleCreated == true ? _dropDown.Handle : IntPtr.Zero;
    internal BootstrapSelectDropDownContent? Content => _content;
    internal string CurrentSearchText => _content?.SearchText ?? string.Empty;

    internal void Open()
    {
        ThrowIfDisposed();
        if (_isOpen)
        {
            Reposition();
            _content?.FocusSearch();
            return;
        }
        if (_owner.IsDisposed || !_owner.Enabled || !_owner.Visible || !_owner.IsHandleCreated) return;
        EnsureCreated();
        RefreshResults();
        ApplyPresentation();
        _currentBounds = ComputeBounds();
        _isOpen = true;
        _restoreFocusOnClosed = false;
        _dropDown!.ShowAt(_currentBounds);
        _content!.FocusSearch();
        _tracker = new BootstrapOverlayAnchorTracker(_owner, Reposition, () => Close(false));
        _owner.NotifyDropDownOpened();
        _owner.NotifyPopupSearchTextChanged(_content.SearchText);
    }

    internal void Close(bool restoreFocus)
    {
        if (_disposed || !_isOpen) return;
        _restoreFocusOnClosed = restoreFocus;
        _dropDown?.Close(ToolStripDropDownCloseReason.CloseCalled);
        if (_dropDown is null || !_dropDown.Visible) CompleteClose();
    }

    internal void Reposition()
    {
        if (_disposed || !_isOpen || _dropDown is null || _dropDown.IsDisposed || !_owner.IsHandleCreated) return;
        _currentBounds = ComputeBounds();
        _dropDown.MoveTo(_currentBounds);
    }

    internal void RefreshResults(
        BootstrapSelectResultsUpdateMode updateMode = BootstrapSelectResultsUpdateMode.ResetNavigation)
    {
        if (_content is null) return;
        _content.SetResults(
            _owner.BuildCurrentPopupResultSet(_content.SearchEnabled ? _content.SearchText : string.Empty),
            updateMode,
            _owner.ValueComparer);
        if (_isOpen) Reposition();
    }

    internal void SetSearchText(string text)
    {
        EnsureCreated();
        _content!.SearchText = text ?? throw new ArgumentNullException(nameof(text));
    }

    internal bool ActivateHighlighted(BootstrapSelectChangeReason reason)
    {
        return _content is not null && _content.ActivateHighlighted(reason);
    }

    internal void ForwardCharacter(char character)
    {
        EnsureCreated();
        _content!.ForwardCharacter(character);
    }

    internal void ApplyPresentation()
    {
        if (_surface is null || _content is null) return;
        var dpi = _owner.DeviceDpi > 0 ? _owner.DeviceDpi : DpiScaler.DefaultDpi;
        var theme = BootstrapThemeManager.CurrentTheme;
        _surface.LogicalBorderRadius = _owner.BorderRadius;
        _surface.ApplyTheme(theme, dpi);
        _content.Font = _owner.Font;
        _content.SearchEnabled = _owner.SearchEnabled;
        _content.ApplyPresentation(_owner.Renderer, theme, dpi);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _owner.Items.Changed -= OnItemsChanged;
        BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        _tracker?.Dispose();
        _tracker = null;
        if (_dropDown is not null)
        {
            _dropDown.ApplicationDeactivated -= OnApplicationDeactivated;
            _dropDown.WindowDeactivated -= OnWindowDeactivated;
            _dropDown.Closed -= OnDropDownClosed;
            if (!_dropDown.IsDisposed) _dropDown.Dispose();
        }
        _dropDown = null;
        _surface = null;
        _content = null;
        _isOpen = false;
    }

    private void EnsureCreated()
    {
        ThrowIfDisposed();
        if (_dropDown is not null) return;
        _content = new BootstrapSelectDropDownContent();
        _content.SearchTextChanged += OnSearchTextChanged;
        _content.RowActivated += OnRowActivated;
        _content.EscapeRequested += () => Close(true);
        _content.TabRequested += OnTabRequested;
        _content.NearEndRequested += () => _owner.NotifyNearEndRequested();
        _surface = new BootstrapOverlaySurface { LogicalContentPadding = Padding.Empty, LogicalBorderRadius = _owner.BorderRadius };
        _surface.AttachContent(_content);
        _dropDown = new BootstrapOverlayDropDown(_surface)
        {
            AutoClose = true,
            CloseOnEscape = true,
            EscapeRequested = () => Close(true)
        };
        _dropDown.ApplicationDeactivated += OnApplicationDeactivated;
        _dropDown.WindowDeactivated += OnWindowDeactivated;
        _dropDown.Closed += OnDropDownClosed;
        _creationCount++;
        ApplyPresentation();
    }

    private Rectangle ComputeBounds()
    {
        var dpi = _owner.DeviceDpi > 0 ? _owner.DeviceDpi : DpiScaler.DefaultDpi;
        var requestedWidth = _owner.DropDownWidth == 0 ? _owner.Width : DpiScaler.Scale(_owner.DropDownWidth, dpi);
        var width = Math.Max(_owner.Width, requestedWidth);
        var maxHeight = DpiScaler.Scale(_owner.MaxDropDownHeight, dpi);
        var preferred = _content!.GetPreferredSize(new Size(width, maxHeight));
        var minimumHeight = DpiScaler.Scale(_owner.SearchEnabled ? 64 : 40, dpi);
        var height = Math.Min(maxHeight, Math.Max(minimumHeight, preferred.Height));
        var anchor = _owner.RectangleToScreen(_owner.ClientRectangle);
        var boundary = Screen.FromControl(_owner).WorkingArea;
        var request = new BootstrapOverlayPlacementRequest(
            anchor,
            new Size(width, height),
            boundary,
            BootstrapOverlayPlacement.BottomStart,
            BootstrapOverlayCollisionBehavior.FlipAndShift,
            DpiScaler.Scale(4, dpi),
            DpiScaler.Scale(8, dpi),
            _owner.RightToLeft == RightToLeft.Yes);
        return BootstrapOverlayPlacementEngine.Compute(request).Bounds;
    }

    private void OnSearchTextChanged(string text)
    {
        _owner.NotifyPopupSearchTextChanged(text);
        RefreshResults();
    }

    private void OnRowActivated(BootstrapSelectResultRow row, BootstrapSelectChangeReason reason)
    {
        if (_owner.ActivateResultRow(row, reason))
        {
            if (_owner.CloseOnSelect) Close(true);
            else RefreshResults(BootstrapSelectResultsUpdateMode.PreserveNavigation);
        }
    }

    private void OnTabRequested(bool reverse)
    {
        Close(false);

        if (_owner.IsDisposed || !_owner.IsHandleCreated)
        {
            return;
        }

        _owner.BeginInvoke(new Action(() =>
        {
            if (_owner.IsDisposed || !_owner.IsHandleCreated || !_owner.Visible || !_owner.Enabled || !_owner.CanFocus)
            {
                return;
            }

            if (!_owner.Focus())
            {
                return;
            }

            _owner.ContinueDialogTabNavigation(reverse);
        }));
    }

    private void OnItemsChanged()
    {
        if (_isOpen && _owner.DataProvider is null) RefreshResults();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (_disposed || _dropDown is null) return;
        ApplyPresentation();
        if (_isOpen) Reposition();
    }

    private void OnDropDownClosed(object? sender, ToolStripDropDownClosedEventArgs e)
    {
        CompleteClose();
    }

    private void OnApplicationDeactivated(object? sender, EventArgs e)
    {
        Close(false);
    }

    private void OnWindowDeactivated(IntPtr activatedWindow)
    {
        var ownerForm = _owner.FindForm();
        if (ownerForm?.IsHandleCreated == true && ownerForm.Handle == activatedWindow)
        {
            return;
        }

        Close(false);
    }

    private void CompleteClose()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _tracker?.Dispose();
        _tracker = null;
        _content?.ClearSearchSilently();
        _owner.NotifyDropDownClosed();
        if (_restoreFocusOnClosed && !_owner.IsDisposed && _owner.CanFocus) _owner.Focus();
        _restoreFocusOnClosed = false;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BootstrapSelectDropDownController));
    }
}
