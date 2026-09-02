using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides data for a committed lookup selection.</summary>
public class BootstrapLookupSelectionCommittedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public BootstrapLookupSelectionCommittedEventArgs(object? item, object? value, string displayText, BootstrapLookupCommitReason reason)
    { Item = item; Value = value; DisplayText = displayText ?? string.Empty; Reason = reason; }
    /// <summary>Gets the committed item.</summary>
    public object? Item { get; }
    /// <summary>Gets the committed logical value.</summary>
    public object? Value { get; }
    /// <summary>Gets the committed display text.</summary>
    public string DisplayText { get; }
    /// <summary>Gets the commit reason.</summary>
    public BootstrapLookupCommitReason Reason { get; }
}

/// <summary>Provides data for a change to the highlighted lookup item.</summary>
public sealed class BootstrapLookupHighlightedItemChangedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public BootstrapLookupHighlightedItemChangedEventArgs(object? oldItem, object? newItem) { OldItem = oldItem; NewItem = newItem; }
    /// <summary>Gets the previous highlighted item.</summary>
    public object? OldItem { get; }
    /// <summary>Gets the new highlighted item.</summary>
    public object? NewItem { get; }
}

/// <summary>Provides data for a lookup refresh request.</summary>
public class BootstrapLookupRefreshRequestedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public BootstrapLookupRefreshRequestedEventArgs(string queryText) { QueryText = queryText ?? string.Empty; }
    /// <summary>Gets the active query text.</summary>
    public string QueryText { get; }
}

/// <summary>Provides data for an explicit lookup Add New request.</summary>
public class BootstrapLookupAddNewRequestedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public BootstrapLookupAddNewRequestedEventArgs(string queryText) { QueryText = queryText ?? string.Empty; }
    /// <summary>Gets the active query text.</summary>
    public string QueryText { get; }
    /// <summary>Gets or sets the item accepted by the application workflow.</summary>
    public object? NewItem { get; set; }
    /// <summary>Gets or sets whether the request is canceled.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Provides data for creation from unmatched lookup text.</summary>
public class BootstrapLookupCreateItemFromTextEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public BootstrapLookupCreateItemFromTextEventArgs(string originalText, string normalizedText)
    { OriginalText = originalText ?? string.Empty; NormalizedText = normalizedText ?? string.Empty; }
    /// <summary>Gets the original editor text.</summary>
    public string OriginalText { get; }
    /// <summary>Gets the exact-match-normalized text.</summary>
    public string NormalizedText { get; }
    /// <summary>Gets or sets the created item.</summary>
    public object? Item { get; set; }
    /// <summary>Gets or sets whether creation is canceled.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Provides native grid coordinates for a lookup-column event.</summary>
public class BootstrapLookupCellEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public BootstrapLookupCellEventArgs(DataGridView dataGridView, int rowIndex, int columnIndex)
    { DataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView)); RowIndex = rowIndex; ColumnIndex = columnIndex; }
    /// <summary>Gets the owning grid.</summary>
    public DataGridView DataGridView { get; }
    /// <summary>Gets the row index.</summary>
    public int RowIndex { get; }
    /// <summary>Gets the column index.</summary>
    public int ColumnIndex { get; }
}
