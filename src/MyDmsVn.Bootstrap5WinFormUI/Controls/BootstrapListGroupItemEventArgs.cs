using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides the item associated with a list-group activation.</summary>
public sealed class BootstrapListGroupItemEventArgs : EventArgs
{
    /// <summary>Initializes event data for the supplied item.</summary>
    /// <param name="item">The activated item.</param>
    public BootstrapListGroupItemEventArgs(BootstrapListGroupItem item)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    /// <summary>Gets the item associated with the event.</summary>
    public BootstrapListGroupItem Item { get; }
}
