using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupColumn
{
    /// <summary>Occurs when a cell lookup commits a selection.</summary>
    public event EventHandler<BootstrapLookupCellEventArgs>? SelectionCommitted;
    /// <summary>Occurs when a cell lookup requests Refresh.</summary>
    public event EventHandler<BootstrapLookupCellEventArgs>? RefreshRequested;
    /// <summary>Occurs when a cell lookup requests Add New.</summary>
    public event EventHandler<BootstrapLookupCellEventArgs>? AddNewRequested;
    /// <summary>Occurs when a cell lookup requests creation from text.</summary>
    public event EventHandler<BootstrapLookupCellEventArgs>? CreateItemFromText;

    internal void RaiseSelectionCommitted(BootstrapLookupCellEventArgs e)
    {
        var generation = ++_selectionCommitGeneration;
        var handlers = SelectionCommitted;
        if (handlers is null) return;
        foreach (EventHandler<BootstrapLookupCellEventArgs> handler in handlers.GetInvocationList())
        {
            if (generation != _selectionCommitGeneration) return;
            handler(this, e);
        }
    }
    internal void RaiseRefreshRequested(BootstrapLookupCellEventArgs e) => RefreshRequested?.Invoke(this, e);
    internal void RaiseAddNewRequested(BootstrapLookupCellEventArgs e) => AddNewRequested?.Invoke(this, e);
    internal void RaiseCreateItemFromText(BootstrapLookupCellEventArgs e) => CreateItemFromText?.Invoke(this, e);
}
