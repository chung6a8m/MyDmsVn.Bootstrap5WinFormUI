using System;
using System.Collections.Generic;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectSearchState
{
    internal string SearchText { get; set; } = string.Empty;
    internal int PageSize { get; set; } = 20;
    internal int CurrentPage { get; set; }
    internal bool HasMore { get; set; }
    internal List<BootstrapSelectItem> LoadedItems { get; } = new List<BootstrapSelectItem>();
    internal BootstrapSelectResultSet Results { get; set; } = BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Empty, "No results found.");
    internal Exception? LastError { get; set; }

    internal void Reset(string searchText, int pageSize)
    {
        SearchText = searchText;
        PageSize = pageSize;
        CurrentPage = 0;
        HasMore = false;
        LoadedItems.Clear();
        LastError = null;
        Results = BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Loading, "Loading...");
    }
}
