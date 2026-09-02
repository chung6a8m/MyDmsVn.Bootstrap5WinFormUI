using System.Collections.Generic;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapLookupSearchState
{
    Results,
    WaitingForMinimumLength
}

internal sealed class BootstrapLookupSearchResult
{
    internal BootstrapLookupSearchResult(BootstrapLookupSearchState state, IReadOnlyList<BootstrapLookupSourceItem> items)
    {
        State = state;
        Items = items;
    }

    internal BootstrapLookupSearchState State { get; }
    internal IReadOnlyList<BootstrapLookupSourceItem> Items { get; }
}
