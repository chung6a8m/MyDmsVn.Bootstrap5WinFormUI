using System;
using System.Collections.Generic;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal static class BootstrapLookupSearchEngine
{
    internal static BootstrapLookupSearchResult Search(
        IReadOnlyList<BootstrapLookupSourceItem> source,
        string? query,
        int minimumSearchLength,
        BootstrapLookupEmptyQueryBehavior emptyQueryBehavior,
        IReadOnlyList<string> searchMembers,
        string displayMember,
        Func<string, string> normalizer)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (minimumSearchLength < 0) throw new ArgumentOutOfRangeException(nameof(minimumSearchLength));
        if (!Enum.IsDefined(typeof(BootstrapLookupEmptyQueryBehavior), emptyQueryBehavior)) throw new ArgumentOutOfRangeException(nameof(emptyQueryBehavior));
        if (searchMembers is null) throw new ArgumentNullException(nameof(searchMembers));
        if (normalizer is null) throw new ArgumentNullException(nameof(normalizer));

        var normalizedQuery = normalizer(query ?? string.Empty) ?? string.Empty;
        if (normalizedQuery.Length < minimumSearchLength)
            return new BootstrapLookupSearchResult(BootstrapLookupSearchState.WaitingForMinimumLength, Array.Empty<BootstrapLookupSourceItem>());

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var emptyItems = emptyQueryBehavior == BootstrapLookupEmptyQueryBehavior.ShowAll
                ? source.ToArray()
                : Array.Empty<BootstrapLookupSourceItem>();
            return new BootstrapLookupSearchResult(BootstrapLookupSearchState.Results, emptyItems);
        }

        var members = searchMembers.Count == 0 ? new[] { displayMember ?? string.Empty } : searchMembers.ToArray();
        var tokens = normalizedQuery.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var ranked = new List<RankedItem>();

        foreach (var sourceItem in source)
        {
            var minQuality = int.MaxValue;
            var totalQuality = 0;
            var displayMatches = 0;
            var priorityTotal = 0;
            var matches = true;

            foreach (var token in tokens)
            {
                var bestQuality = BootstrapLookupMatchQuality.NoMatch;
                var bestPriority = int.MaxValue;
                var bestIsDisplay = false;
                for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
                {
                    var member = members[memberIndex] ?? string.Empty;
                    var raw = string.IsNullOrEmpty(member)
                        ? sourceItem.Item.ToString() ?? string.Empty
                        : BootstrapLookupMemberAccessor.GetValue(sourceItem.Item, member)?.ToString() ?? string.Empty;
                    var quality = GetQuality(normalizer(raw) ?? string.Empty, token);
                    var isDisplay = string.Equals(member, displayMember, StringComparison.Ordinal);
                    if (quality > bestQuality ||
                        (quality == bestQuality && isDisplay && !bestIsDisplay) ||
                        (quality == bestQuality && isDisplay == bestIsDisplay && memberIndex < bestPriority))
                    {
                        bestQuality = quality;
                        bestPriority = memberIndex;
                        bestIsDisplay = isDisplay;
                    }
                }

                if (bestQuality == BootstrapLookupMatchQuality.NoMatch)
                {
                    matches = false;
                    break;
                }

                minQuality = Math.Min(minQuality, (int)bestQuality);
                totalQuality += (int)bestQuality;
                if (bestIsDisplay) displayMatches++;
                priorityTotal += bestPriority;
            }

            if (matches)
                ranked.Add(new RankedItem(sourceItem, minQuality, totalQuality, displayMatches, priorityTotal));
        }

        var items = ranked
            .OrderByDescending(item => item.MinQuality)
            .ThenByDescending(item => item.TotalQuality)
            .ThenByDescending(item => item.DisplayMatches)
            .ThenBy(item => item.PriorityTotal)
            .ThenBy(item => item.Item.SourceIndex)
            .Select(item => item.Item)
            .ToArray();
        return new BootstrapLookupSearchResult(BootstrapLookupSearchState.Results, items);
    }

    private static BootstrapLookupMatchQuality GetQuality(string value, string token)
    {
        if (string.Equals(value, token, StringComparison.Ordinal)) return BootstrapLookupMatchQuality.Exact;
        if (value.StartsWith(token, StringComparison.Ordinal)) return BootstrapLookupMatchQuality.StartsWith;
        var index = value.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            if (index > 0 && !char.IsLetterOrDigit(value[index - 1])) return BootstrapLookupMatchQuality.WordStart;
            index = value.IndexOf(token, index + 1, StringComparison.Ordinal);
        }
        return value.IndexOf(token, StringComparison.Ordinal) >= 0 ? BootstrapLookupMatchQuality.Contains : BootstrapLookupMatchQuality.NoMatch;
    }

    private sealed class RankedItem
    {
        internal RankedItem(BootstrapLookupSourceItem item, int minQuality, int totalQuality, int displayMatches, int priorityTotal)
        { Item = item; MinQuality = minQuality; TotalQuality = totalQuality; DisplayMatches = displayMatches; PriorityTotal = priorityTotal; }
        internal BootstrapLookupSourceItem Item { get; }
        internal int MinQuality { get; }
        internal int TotalQuality { get; }
        internal int DisplayMatches { get; }
        internal int PriorityTotal { get; }
    }
}
