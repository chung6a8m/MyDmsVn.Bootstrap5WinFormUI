using System;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Stores caller-owned dropdown item models in insertion order and rejects null entries.
/// </summary>
public sealed class BootstrapDropdownItemCollection : Collection<BootstrapDropdownItem>
{
    /// <inheritdoc />
    protected override void InsertItem(int index, BootstrapDropdownItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, BootstrapDropdownItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        base.SetItem(index, item);
    }
}
