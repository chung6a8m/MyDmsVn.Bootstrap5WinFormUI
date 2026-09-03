using System;
using System.Collections.Generic;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
    private bool _projectionDirty;
    private bool _hasAppliedSearchConfiguration;
    private int _appliedMinimumSearchLength;
    private BootstrapLookupEmptyQueryBehavior _appliedEmptyQueryBehavior;
    private Func<string, string>? _appliedSearchTextNormalizer;
    private string _appliedDisplayMember = string.Empty;
    private string[] _appliedSearchMembers = Array.Empty<string>();
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
        _projectionDirty = false;
        CaptureSearchConfiguration();
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
        if (!_projectionDirty && !HasSearchConfigurationChanged()) return;
        ExecuteSearchNow();
    }

    private void ScheduleSearchForEditorText()
    {
        _projectionDirty = true;
        _searchDebouncer.Schedule(TimeSpan.FromMilliseconds(SearchDebounceMilliseconds), ExecuteScheduledSearch);
    }

    private void CancelPendingSearch()
    {
        _searchDebouncer.Cancel();
    }

    private bool HasSearchConfigurationChanged()
    {
        if (!_hasAppliedSearchConfiguration ||
            _appliedMinimumSearchLength != MinimumSearchLength ||
            _appliedEmptyQueryBehavior != EmptyQueryBehavior ||
            !ReferenceEquals(_appliedSearchTextNormalizer, SearchTextNormalizer) ||
            !string.Equals(_appliedDisplayMember, DisplayMember, StringComparison.Ordinal) ||
            _appliedSearchMembers.Length != SearchMembers.Count)
            return true;

        for (var index = 0; index < _appliedSearchMembers.Length; index++)
        {
            if (!string.Equals(_appliedSearchMembers[index], SearchMembers[index], StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private void CaptureSearchConfiguration()
    {
        _hasAppliedSearchConfiguration = true;
        _appliedMinimumSearchLength = MinimumSearchLength;
        _appliedEmptyQueryBehavior = EmptyQueryBehavior;
        _appliedSearchTextNormalizer = SearchTextNormalizer;
        _appliedDisplayMember = DisplayMember;
        _appliedSearchMembers = SearchMembers.ToArray();
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
        if (_hasHighlightedItem)
            chosen = items.FirstOrDefault(item => ReferenceEquals(item.Item, _highlightedItem)) ??
                items.FirstOrDefault(IsHighlightedSourceItem);
        if (chosen is null && _selectedValue is not null)
            chosen = items.FirstOrDefault(item => EqualityComparer<object?>.Default.Equals(item.Value, _selectedValue));
        if (chosen is null && items.Count > 0) chosen = items[0];
        SetHighlightedSourceItem(chosen);
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
