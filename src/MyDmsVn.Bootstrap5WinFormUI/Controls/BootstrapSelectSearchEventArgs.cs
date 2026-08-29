using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides data for the start of an asynchronous Select search.</summary>
public class BootstrapSelectSearchEventArgs : EventArgs
{
    /// <summary>Initializes search event data.</summary>
    public BootstrapSelectSearchEventArgs(string searchText, int page)
    {
        SearchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        Page = page;
    }

    /// <summary>Gets the search text.</summary>
    public string SearchText { get; }

    /// <summary>Gets the one-based page number.</summary>
    public int Page { get; }
}

/// <summary>Provides data for a successfully completed asynchronous Select search.</summary>
public sealed class BootstrapSelectSearchCompletedEventArgs : BootstrapSelectSearchEventArgs
{
    /// <summary>Initializes completed search event data.</summary>
    public BootstrapSelectSearchCompletedEventArgs(string searchText, int page, int resultCount, bool hasMore)
        : base(searchText, page)
    {
        if (resultCount < 0) throw new ArgumentOutOfRangeException(nameof(resultCount));
        ResultCount = resultCount;
        HasMore = hasMore;
    }

    /// <summary>Gets the number of items currently loaded for the logical query.</summary>
    public int ResultCount { get; }

    /// <summary>Gets whether another result page is available.</summary>
    public bool HasMore { get; }
}

/// <summary>Provides data for a failed asynchronous Select search.</summary>
public sealed class BootstrapSelectSearchFailedEventArgs : BootstrapSelectSearchEventArgs
{
    /// <summary>Initializes failed search event data.</summary>
    public BootstrapSelectSearchFailedEventArgs(string searchText, int page, Exception error)
        : base(searchText, page)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>Gets the provider exception.</summary>
    public Exception Error { get; }
}
