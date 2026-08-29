using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides the default case-insensitive ordinal substring matcher for local select items.
/// </summary>
public sealed class BootstrapSelectTextMatcher : IBootstrapSelectMatcher
{
    /// <inheritdoc />
    public bool IsMatch(BootstrapSelectItem item, string searchText)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (searchText is null)
        {
            throw new ArgumentNullException(nameof(searchText));
        }

        return searchText.Length == 0
            || item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
