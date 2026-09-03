using System;
using System.Collections.Generic;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
    private bool _searchPending;
    private BootstrapLookupSearchResult _currentSearchResult = new BootstrapLookupSearchResult(
        BootstrapLookupSearchState.Results,
        Array.Empty<BootstrapLookupSourceItem>());

    internal void ExecuteSearchNow()
    {
        CancelPendingSearch();
        var source = _dataAdapter?.Snapshot ?? Array.Empty<BootstrapLookupSourceItem>();
        var next = BootstrapLookupSearchEngine.Search(
            source,
            Text,
            MinimumSearchLength,
            EmptyQueryBehavior,
            SearchMembers.ToArray(),
            DisplayMember,
            SearchTextNormalizer);
        var changed = !HasSameProjection(_currentSearchResult, next);
        _currentSearchResult = next;
        PreserveOrChooseHighlight(next.Items);
        ApplyCurrentResultsToContent();
        if (changed)
        {
            if (IsDropDownOpen) _dropDownController.Reposition();
            RaiseResultsChanged();
        }
    }

    internal void FlushPendingSearch()
    {
        if (!_searchPending) return;
        ExecuteSearchNow();
    }

    private void ScheduleSearchForEditorText()
    {
        _searchPending = true;
        _searchDebouncer.Schedule(TimeSpan.FromMilliseconds(SearchDebounceMilliseconds), ExecuteScheduledSearch);
    }

    private void CancelPendingSearch()
    {
        _searchPending = false;
        _searchDebouncer.Cancel();
    }

    private void ExecuteScheduledSearch()
    {
        ExecuteSearchNow();
        var normalized = SearchTextNormalizer(Text) ?? string.Empty;
        if (TypingPopupBehavior == BootstrapLookupTypingPopupBehavior.AutoOpen &&
            normalized.Length >= MinimumSearchLength &&
            normalized.Length > 0 &&
            ContainsFocus &&
            !IsDropDownOpen)
        {
            _dropDownController.Open();
        }
    }

    private void PreserveOrChooseHighlight(IReadOnlyList<BootstrapLookupSourceItem> items)
    {
        BootstrapLookupSourceItem? chosen = null;
        if (_highlightedItem is not null)
            chosen = items.FirstOrDefault(item => SameLogicalItem(item, _highlightedItem));
        if (chosen is null && _selectedValue is not null)
            chosen = items.FirstOrDefault(item => EqualityComparer<object?>.Default.Equals(item.Value, _selectedValue));
        if (chosen is null && items.Count > 0) chosen = items[0];
        SetHighlightedItem(chosen?.Item);
    }

    private bool SameLogicalItem(BootstrapLookupSourceItem candidate, object item)
    {
        if (ReferenceEquals(candidate.Item, item)) return true;
        if (_dataAdapter is null || !_dataAdapter.TryFindByItem(item, out var previous)) return Equals(candidate.Item, item);
        return EqualityComparer<object?>.Default.Equals(candidate.Value, previous!.Value);
    }

    private static bool HasSameProjection(BootstrapLookupSearchResult left, BootstrapLookupSearchResult right)
    {
        if (left.State != right.State || left.Items.Count != right.Items.Count) return false;
        for (var index = 0; index < left.Items.Count; index++)
        {
            var first = left.Items[index];
            var second = right.Items[index];
            if (!EqualityComparer<object?>.Default.Equals(first.Value, second.Value) ||
                (!ReferenceEquals(first.Item, second.Item) && first.Value is null && second.Value is null))
                return false;
        }
        return true;
    }
}
