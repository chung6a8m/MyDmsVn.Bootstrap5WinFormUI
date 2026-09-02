using System;
using System.Collections.Generic;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
    private object? _dataSource;
    private string _displayMember = string.Empty;
    private string _valueMember = string.Empty;
    private BootstrapLookupDataAdapter? _dataAdapter;

    private void SetDataSource(object? value)
    {
        if (ReferenceEquals(_dataSource, value)) return;
        _dataSource = value;
        RebuildDataAdapter();
    }

    private void SetDisplayMember(string? value)
    {
        var normalized = value ?? string.Empty;
        if (string.Equals(_displayMember, normalized, StringComparison.Ordinal)) return;
        _displayMember = normalized;
        RebuildDataAdapter();
    }

    private void SetValueMember(string? value)
    {
        var normalized = value ?? string.Empty;
        if (string.Equals(_valueMember, normalized, StringComparison.Ordinal)) return;
        _valueMember = normalized;
        RebuildDataAdapter();
    }

    private void RebuildDataAdapter()
    {
        DisposeDataAdapter();
        _dataAdapter = new BootstrapLookupDataAdapter(_dataSource, _displayMember, _valueMember);
        _dataAdapter.SourceChanged += OnLookupSourceChanged;
        ReconcileCommittedSelection();
    }

    private void DisposeDataAdapter()
    {
        if (_dataAdapter is null) return;
        _dataAdapter.SourceChanged -= OnLookupSourceChanged;
        _dataAdapter.Dispose();
        _dataAdapter = null;
    }

    private void OnLookupSourceChanged(object? sender, EventArgs e) => ReconcileCommittedSelection();

    private void ReconcileCommittedSelection()
    {
        if (_dataAdapter is null || _selectedValue is null) return;
        if (_dataAdapter.TryFindByValue(_selectedValue, out var found))
        {
            _selectedItem = found!.Item;
            _committedDisplayText = found.DisplayText;
            if (!_hasPendingText) SynchronizeText(_committedDisplayText);
        }
        else
        {
            _selectedItem = null;
        }
    }

    private void SetSelectedValue(object? value)
    {
        if (value is null)
        {
            ClearSelection();
            return;
        }

        if (!SelectValueCore(value, BootstrapLookupCommitReason.Programmatic))
            CommitSelection(null, value, string.Empty, BootstrapLookupCommitReason.Programmatic);
    }

    private bool SelectValueCore(object? value, BootstrapLookupCommitReason reason)
    {
        if (value is null)
        {
            CommitSelection(null, null, string.Empty, reason == BootstrapLookupCommitReason.Programmatic ? BootstrapLookupCommitReason.Clear : reason);
            return true;
        }
        if (_dataAdapter is null || !_dataAdapter.TryFindByValue(value, out var found)) return false;
        if (found!.Value is null && _valueMember.Length > 0) return false;
        CommitSelection(found.Item, found.Value, found.DisplayText, reason);
        return true;
    }

    private bool SelectItemCore(object? item, BootstrapLookupCommitReason reason)
    {
        if (item is null)
        {
            ClearSelection();
            return true;
        }
        if (_dataAdapter is null || !_dataAdapter.TryFindByItem(item, out var found)) return false;
        if (found!.Value is null && _valueMember.Length > 0) return false;
        CommitSelection(found.Item, found.Value, found.DisplayText, reason);
        return true;
    }

    internal void CommitSelection(object? item, object? value, string displayText, BootstrapLookupCommitReason reason)
    {
        var changed = !EqualityComparer<object?>.Default.Equals(_selectedValue, value);
        _selectedItem = item;
        _selectedValue = value;
        _committedDisplayText = displayText ?? string.Empty;
        SynchronizeText(_committedDisplayText);
        _hasPendingText = false;
        ClearLookupValidation();
        if (changed) SelectedValueChanged?.Invoke(this, EventArgs.Empty);
        SelectionCommitted?.Invoke(this, new BootstrapLookupSelectionCommittedEventArgs(item, value, _committedDisplayText, reason));
    }
}
