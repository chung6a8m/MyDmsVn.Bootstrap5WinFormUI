namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Defines caller-replaceable local matching logic for <see cref="BootstrapSelectItem"/> results.
/// </summary>
public interface IBootstrapSelectMatcher
{
    /// <summary>
    /// Determines whether an item matches the supplied search text.
    /// </summary>
    /// <param name="item">The non-null item to evaluate.</param>
    /// <param name="searchText">The non-null search text.</param>
    /// <returns><see langword="true"/> when the item should be included; otherwise <see langword="false"/>.</returns>
    bool IsMatch(BootstrapSelectItem item, string searchText);
}
