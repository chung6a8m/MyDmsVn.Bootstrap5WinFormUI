using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapOverlayDropDown : ToolStripDropDown
{
    private readonly BootstrapOverlaySurface _surface;
    private readonly ToolStripControlHost _host;
    private Region? _ownedRegion;

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

    public void ShowAt(Rectangle screenBounds)
    {
        ApplyBounds(screenBounds);
        Show(screenBounds.Location);
    }

    public void MoveTo(Rectangle screenBounds)
    {
        ApplyBounds(screenBounds);
        Location = screenBounds.Location;
    }

    protected override bool ProcessCmdKey(ref Message m, Keys keyData)
    {
        if (CloseOnEscape && keyData == Keys.Escape && EscapeRequested is not null)
        {
            EscapeRequested();
            return true;
        }

        return base.ProcessCmdKey(ref m, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            EscapeRequested = null;
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
}
