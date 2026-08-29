using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Describes one immutable page request sent to an <see cref="IBootstrapSelectDataProvider"/>.</summary>
public sealed class BootstrapSelectQuery
{
    /// <summary>Initializes a validated Select query.</summary>
    public BootstrapSelectQuery(string searchText, int page, int pageSize)
    {
        SearchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Gets the non-null search text.</summary>
    public string SearchText { get; }

    /// <summary>Gets the one-based page number.</summary>
    public int Page { get; }

    /// <summary>Gets the requested page size.</summary>
    public int PageSize { get; }
}
