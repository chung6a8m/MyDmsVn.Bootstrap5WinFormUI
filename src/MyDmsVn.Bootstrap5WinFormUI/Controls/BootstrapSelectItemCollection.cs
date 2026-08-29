using System;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Stores caller-owned <see cref="BootstrapSelectItem"/> instances in insertion order and rejects null entries.
/// </summary>
public sealed class BootstrapSelectItemCollection : Collection<BootstrapSelectItem>
{
    private readonly Action? _changed;

    /// <summary>
    /// Initializes an independent item collection.
    /// </summary>
    public BootstrapSelectItemCollection()
    {
    }

    internal BootstrapSelectItemCollection(Action changed)
    {
        _changed = changed ?? throw new ArgumentNullException(nameof(changed));
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, BootstrapSelectItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        base.InsertItem(index, item);
        _changed?.Invoke();
    }

    /// <inheritdoc />
    protected override void SetItem(int index, BootstrapSelectItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        base.SetItem(index, item);
        _changed?.Invoke();
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        _changed?.Invoke();
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        if (Count == 0)
        {
            return;
        }

        base.ClearItems();
        _changed?.Invoke();
    }
}
