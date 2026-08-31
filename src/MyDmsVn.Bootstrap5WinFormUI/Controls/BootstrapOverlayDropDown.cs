using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapOverlayDropDown : ToolStripDropDown
{
    private readonly BootstrapOverlaySurface _surface;
    private readonly ToolStripControlHost _host;
    private Region? _ownedRegion;
    private Rectangle _requestedBounds;
    private int _boundsGeneration;
    private bool _boundsCorrectionQueued;

    public BootstrapOverlayDropDown(BootstrapOverlaySurface surface)
    {
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        AutoSize = false;
        Padding = Padding.Empty;
        Margin = Padding.Empty;
        DropShadowEnabled = true;
        Renderer = new BootstrapOverlayToolStripRenderer();
        _host = new ToolStripControlHost(surface)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        Items.Add(_host);
    }

    public bool CloseOnEscape { get; set; }

    public Action? EscapeRequested { get; set; }

    public Func<bool, bool>? TabNavigationRequested { get; set; }

    public void ShowAt(Rectangle screenBounds)
    {
        RecordRequestedBounds(screenBounds);
        ApplyBounds(screenBounds);
        Show(screenBounds.Location);
        ApplyRequestedWindowBounds();
        QueueBoundsCorrection();
    }

    public void MoveTo(Rectangle screenBounds)
    {
        RecordRequestedBounds(screenBounds);
        ApplyBounds(screenBounds);
        ApplyRequestedWindowBounds();
        QueueBoundsCorrection();
    }

    protected override bool ProcessCmdKey(ref Message m, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            if (CloseOnEscape && EscapeRequested is not null)
            {
                EscapeRequested();
            }

            return true;
        }

        return base.ProcessCmdKey(ref m, keyData);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (TryProcessTabNavigation(keyData))
        {
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }

    private bool TryProcessTabNavigation(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if (keyCode == Keys.Tab
            && (modifiers == Keys.None || modifiers == Keys.Shift)
            && TabNavigationRequested is not null
            && TabNavigationRequested(modifiers != Keys.Shift))
        {
            return true;
        }

        return false;
    }

    protected override void OnClosing(ToolStripDropDownClosingEventArgs e)
    {
        if (e.CloseReason == ToolStripDropDownCloseReason.Keyboard
            && (ModifierKeys & Keys.Alt) == Keys.Alt)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _boundsGeneration++;
            _boundsCorrectionQueued = false;
            EscapeRequested = null;
            TabNavigationRequested = null;
            Region = null;
            _ownedRegion?.Dispose();
            _ownedRegion = null;
        }

        base.Dispose(disposing);
    }

    private void ApplyBounds(Rectangle screenBounds)
    {
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(screenBounds), screenBounds, "Overlay bounds must have positive dimensions.");
        }

        _surface.Size = screenBounds.Size;
        _host.Size = screenBounds.Size;
        Size = screenBounds.Size;
        var next = _surface.Region?.Clone();
        var previous = _ownedRegion;
        _ownedRegion = next;
        Region = next;
        previous?.Dispose();
    }

    private void RecordRequestedBounds(Rectangle screenBounds)
    {
        _requestedBounds = screenBounds;
        _boundsGeneration++;
    }

    private void ApplyRequestedWindowBounds()
    {
        if (IsHandleCreated && !_requestedBounds.IsEmpty)
        {
            BootstrapOverlayWindowBounds.TrySetBounds(Handle, _requestedBounds);
        }
    }

    private void QueueBoundsCorrection()
    {
        if (_boundsCorrectionQueued || !IsHandleCreated || IsDisposed)
        {
            return;
        }

        _boundsCorrectionQueued = true;
        var generation = _boundsGeneration;
        try
        {
            BeginInvoke((Action)(() =>
            {
                _boundsCorrectionQueued = false;
                if (IsDisposed)
                {
                    return;
                }

                if (generation != _boundsGeneration)
                {
                    QueueBoundsCorrection();
                    return;
                }

                ApplyRequestedWindowBounds();
            }));
        }
        catch (ObjectDisposedException)
        {
            _boundsCorrectionQueued = false;
        }
        catch (InvalidOperationException)
        {
            _boundsCorrectionQueued = false;
        }
    }
}
