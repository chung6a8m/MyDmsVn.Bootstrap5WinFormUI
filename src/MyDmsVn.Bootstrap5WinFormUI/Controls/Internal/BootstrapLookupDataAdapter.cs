using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupDataAdapter : IDisposable
{
    private readonly object? _dataSource;
    private readonly string _displayMember;
    private readonly string _valueMember;
    private readonly BindingSource? _bindingSource;
    private readonly IBindingList? _bindingList;
    private List<BootstrapLookupSourceItem> _snapshot = new List<BootstrapLookupSourceItem>();
    private bool _disposed;

    internal BootstrapLookupDataAdapter(object? dataSource, string displayMember, string valueMember)
    {
        _dataSource = dataSource;
        _displayMember = displayMember ?? string.Empty;
        _valueMember = valueMember ?? string.Empty;
        _bindingSource = dataSource as BindingSource;
        if (_bindingSource != null)
        {
            _bindingSource.ListChanged += OnSourceChanged;
        }
        else
        {
            _bindingList = ResolveList(dataSource) as IBindingList;
            if (_bindingList != null) _bindingList.ListChanged += OnSourceChanged;
        }

        try
        {
            Refresh();
        }
        catch
        {
            Detach();
            throw;
        }
    }

    internal event EventHandler? SourceChanged;

    internal IReadOnlyList<BootstrapLookupSourceItem> Snapshot => _snapshot;

    internal bool CanAdd
    {
        get
        {
            var list = ResolveList(_dataSource) as IList;
            return list != null && !list.IsReadOnly && !list.IsFixedSize;
        }
    }

    internal void Refresh()
    {
        ThrowIfDisposed();
        var result = new List<BootstrapLookupSourceItem>();
        var enumerable = ResolveEnumerable(_dataSource);
        if (enumerable != null)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (item is null) continue;
                BootstrapLookupMemberAccessor.Validate(item.GetType(), _displayMember);
                BootstrapLookupMemberAccessor.Validate(item.GetType(), _valueMember);
                var display = BootstrapLookupMemberAccessor.GetValue(item, _displayMember)?.ToString() ?? string.Empty;
                var value = BootstrapLookupMemberAccessor.GetValue(item, _valueMember);
                result.Add(new BootstrapLookupSourceItem(item, value, display, index++));
            }
        }

        _snapshot = result;
    }

    internal bool TryFindByValue(object? value, out BootstrapLookupSourceItem? sourceItem)
    {
        ThrowIfDisposed();
        var comparer = EqualityComparer<object?>.Default;
        foreach (var candidate in _snapshot)
        {
            if (comparer.Equals(candidate.Value, value))
            {
                sourceItem = candidate;
                return true;
            }
        }

        sourceItem = null;
        return false;
    }

    internal bool TryFindByItem(object? item, out BootstrapLookupSourceItem? sourceItem)
    {
        ThrowIfDisposed();
        foreach (var candidate in _snapshot)
        {
            if (ReferenceEquals(candidate.Item, item) || Equals(candidate.Item, item))
            {
                sourceItem = candidate;
                return true;
            }
        }

        sourceItem = null;
        return false;
    }

    internal void Add(object item)
    {
        ThrowIfDisposed();
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (!CanAdd) throw new NotSupportedException("The lookup data source does not accept new items.");
        if (_bindingSource != null) _bindingSource.Add(item);
        else ((IList)ResolveList(_dataSource)!).Add(item);
        Refresh();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Detach();
        _snapshot.Clear();
    }

    private void OnSourceChanged(object? sender, ListChangedEventArgs e)
    {
        if (_disposed) return;
        Refresh();
        SourceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Detach()
    {
        if (_bindingSource != null) _bindingSource.ListChanged -= OnSourceChanged;
        if (_bindingList != null) _bindingList.ListChanged -= OnSourceChanged;
    }

    private static IEnumerable? ResolveEnumerable(object? source)
    {
        if (source is BindingSource bindingSource) return bindingSource.List as IEnumerable;
        if (source is IListSource listSource) return listSource.GetList() as IEnumerable;
        return source as IEnumerable;
    }

    private static object? ResolveList(object? source)
    {
        if (source is BindingSource bindingSource) return bindingSource.List;
        if (source is IListSource listSource) return listSource.GetList();
        return source;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BootstrapLookupDataAdapter));
    }
}
