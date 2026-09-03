using System;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Represents the ordered result columns of a lookup.</summary>
public sealed class BootstrapLookupColumnDefinitionCollection : Collection<BootstrapLookupColumnDefinition>
{
    /// <inheritdoc />
    protected override void InsertItem(int index, BootstrapLookupColumnDefinition item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, BootstrapLookupColumnDefinition item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        base.SetItem(index, item);
    }
}
