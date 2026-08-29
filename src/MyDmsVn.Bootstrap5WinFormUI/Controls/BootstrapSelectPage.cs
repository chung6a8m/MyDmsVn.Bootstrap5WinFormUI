using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Represents one immutable snapshot of asynchronously loaded Select items.</summary>
public sealed class BootstrapSelectPage
{
    private readonly ReadOnlyCollection<BootstrapSelectItem> _items;

    /// <summary>Initializes a page by snapshotting the supplied items.</summary>
    public BootstrapSelectPage(IEnumerable<BootstrapSelectItem> items, bool hasMore)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        var snapshot = new List<BootstrapSelectItem>();
        foreach (var item in items)
        {
            if (item is null) throw new ArgumentException("Select pages cannot contain null items.", nameof(items));
            snapshot.Add(item);
        }
        _items = snapshot.AsReadOnly();
        HasMore = hasMore;
    }

    /// <summary>Gets the snapshotted page items.</summary>
    public IReadOnlyList<BootstrapSelectItem> Items => _items;

    /// <summary>Gets whether another page may be requested.</summary>
    public bool HasMore { get; }
}
