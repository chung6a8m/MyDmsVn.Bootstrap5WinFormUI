using System;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Stores caller-owned <see cref="BootstrapBreadcrumbItem"/> instances in logical trail order.
/// </summary>
public sealed class BootstrapBreadcrumbItemCollection : Collection<BootstrapBreadcrumbItem>
{
    private Action? _structureChanged;
    private Action<BootstrapBreadcrumbItem>? _itemTextChanged;

    internal BootstrapBreadcrumbItemCollection(
        Action structureChanged,
        Action<BootstrapBreadcrumbItem> itemTextChanged)
    {
        _structureChanged = structureChanged ?? throw new ArgumentNullException(nameof(structureChanged));
        _itemTextChanged = itemTextChanged ?? throw new ArgumentNullException(nameof(itemTextChanged));
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, BootstrapBreadcrumbItem item)
    {
        ValidateItem(item);
        ThrowIfDuplicate(item, ignoredIndex: -1);

        base.InsertItem(index, item);
        if (_itemTextChanged is not null)
        {
            item.TextChangedForOwner += OnItemTextChanged;
        }

        _structureChanged?.Invoke();
    }

    /// <inheritdoc />
    protected override void SetItem(int index, BootstrapBreadcrumbItem item)
    {
        ValidateItem(item);
        var previous = this[index];
        if (ReferenceEquals(previous, item))
        {
            return;
        }

        ThrowIfDuplicate(item, index);
        base.SetItem(index, item);
        previous.TextChangedForOwner -= OnItemTextChanged;
        if (_itemTextChanged is not null)
        {
            item.TextChangedForOwner += OnItemTextChanged;
        }

        _structureChanged?.Invoke();
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        var removed = this[index];
        base.RemoveItem(index);
        removed.TextChangedForOwner -= OnItemTextChanged;
        _structureChanged?.Invoke();
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        if (Count == 0)
        {
            return;
        }

        var removed = new BootstrapBreadcrumbItem[Count];
        CopyTo(removed, 0);
        base.ClearItems();

        foreach (var item in removed)
        {
            item.TextChangedForOwner -= OnItemTextChanged;
        }

        _structureChanged?.Invoke();
    }

    internal void DetachOwner()
    {
        foreach (var item in this)
        {
            item.TextChangedForOwner -= OnItemTextChanged;
        }

        _structureChanged = null;
        _itemTextChanged = null;
    }

    private static void ValidateItem(BootstrapBreadcrumbItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }
    }

    private void ThrowIfDuplicate(BootstrapBreadcrumbItem item, int ignoredIndex)
    {
        for (var index = 0; index < Count; index++)
        {
            if (index != ignoredIndex && ReferenceEquals(this[index], item))
            {
                throw new ArgumentException(
                    "The same breadcrumb item instance cannot appear more than once.",
                    nameof(item));
            }
        }
    }

    private void OnItemTextChanged(object? sender, EventArgs e)
    {
        if (sender is BootstrapBreadcrumbItem item)
        {
            _itemTextChanged?.Invoke(item);
        }
    }
}
