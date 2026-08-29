using System;
using System.Collections.Generic;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal static class BootstrapSelectResultBuilder
{
    internal static BootstrapSelectResultSet BuildLocal(
        IEnumerable<BootstrapSelectItem> items,
        string searchText,
        IBootstrapSelectMatcher matcher,
        Func<BootstrapSelectItem, bool> isSelected)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (searchText is null)
        {
            throw new ArgumentNullException(nameof(searchText));
        }

        if (matcher is null)
        {
            throw new ArgumentNullException(nameof(matcher));
        }

        if (isSelected is null)
        {
            throw new ArgumentNullException(nameof(isSelected));
        }

        var filtered = new List<BootstrapSelectItem>();
        foreach (var item in items)
        {
            ValidateItem(item, nameof(items));
            if (matcher.IsMatch(item, searchText))
            {
                filtered.Add(item);
            }
        }

        return BuildLoaded(filtered, isSelected);
    }

    internal static BootstrapSelectResultSet BuildLoaded(
        IEnumerable<BootstrapSelectItem> items,
        Func<BootstrapSelectItem, bool> isSelected)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (isSelected is null)
        {
            throw new ArgumentNullException(nameof(isSelected));
        }

        var rows = new List<BootstrapSelectResultRow>();
        AppendRows(rows, items, isSelected, null);
        return new BootstrapSelectResultSet(rows);
    }

    internal static BootstrapSelectResultSet AppendLoaded(
        BootstrapSelectResultSet existing,
        IEnumerable<BootstrapSelectItem> items,
        Func<BootstrapSelectItem, bool> isSelected)
    {
        if (existing is null)
        {
            throw new ArgumentNullException(nameof(existing));
        }

        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (isSelected is null)
        {
            throw new ArgumentNullException(nameof(isSelected));
        }

        var rows = new List<BootstrapSelectResultRow>(existing.Rows.Count);
        for (var i = 0; i < existing.Rows.Count; i++)
        {
            rows.Add(existing.Rows[i]);
        }

        var activeGroup = ResolveTrailingActiveGroup(existing.Rows);
        AppendRows(rows, items, isSelected, activeGroup);
        return new BootstrapSelectResultSet(rows);
    }

    internal static bool HasExactTextMatch(IEnumerable<BootstrapSelectItem> items, string searchText)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (searchText is null)
        {
            throw new ArgumentNullException(nameof(searchText));
        }

        foreach (var item in items)
        {
            ValidateItem(item, nameof(items));
            if (string.Equals(item.Text, searchText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendRows(
        List<BootstrapSelectResultRow> rows,
        IEnumerable<BootstrapSelectItem> items,
        Func<BootstrapSelectItem, bool> isSelected,
        string? initialActiveGroup)
    {
        var activeGroup = initialActiveGroup;
        foreach (var item in items)
        {
            ValidateItem(item, nameof(items));
            var group = NormalizeGroup(item.Group);
            if (group is null)
            {
                activeGroup = null;
            }
            else if (!string.Equals(activeGroup, group, StringComparison.Ordinal))
            {
                rows.Add(BootstrapSelectResultRow.GroupHeader(group));
                activeGroup = group;
            }

            rows.Add(BootstrapSelectResultRow.ItemRow(item, isSelected(item)));
        }
    }

    private static string? ResolveTrailingActiveGroup(IReadOnlyList<BootstrapSelectResultRow> rows)
    {
        if (rows.Count == 0)
        {
            return null;
        }

        var last = rows[rows.Count - 1];
        if (last.Kind != BootstrapSelectResultRowKind.Item || last.Item is null)
        {
            return null;
        }

        return NormalizeGroup(last.Item.Group);
    }

    private static string? NormalizeGroup(string? group)
    {
        return string.IsNullOrEmpty(group) ? null : group;
    }

    private static void ValidateItem(BootstrapSelectItem? item, string parameterName)
    {
        if (item is null)
        {
            throw new ArgumentException("Select item sequences cannot contain null entries.", parameterName);
        }
    }
}
