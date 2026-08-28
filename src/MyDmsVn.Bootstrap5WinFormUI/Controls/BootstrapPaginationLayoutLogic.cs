using System;
using System.Collections.Generic;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal enum BootstrapPaginationItemKind
{
    Page,
    Ellipsis
}

internal readonly struct BootstrapPaginationItem
{
    internal BootstrapPaginationItem(BootstrapPaginationItemKind kind, int page)
    {
        Kind = kind;
        Page = page;
    }

    internal BootstrapPaginationItemKind Kind { get; }

    internal int Page { get; }
}

internal static class BootstrapPaginationLayoutLogic
{
    internal static IReadOnlyList<BootstrapPaginationItem> Build(int totalPages, int currentPage, int maxVisiblePages)
    {
        if (totalPages < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPages), totalPages, "Total pages must be at least one.");
        }

        if (currentPage < 1 || currentPage > totalPages)
        {
            throw new ArgumentOutOfRangeException(nameof(currentPage), currentPage, "Current page must be within the total page range.");
        }

        if (maxVisiblePages < 5)
        {
            throw new ArgumentOutOfRangeException(nameof(maxVisiblePages), maxVisiblePages, "Maximum visible pages must be at least five.");
        }

        var items = new List<BootstrapPaginationItem>();
        if (totalPages <= maxVisiblePages)
        {
            for (var page = 1; page <= totalPages; page++)
            {
                items.Add(Page(page));
            }

            return items;
        }

        var middleCount = maxVisiblePages - 2;
        var middleStart = currentPage - (middleCount / 2);
        var middleEnd = middleStart + middleCount - 1;

        if (middleStart < 2)
        {
            middleStart = 2;
            middleEnd = middleStart + middleCount - 1;
        }

        var lastMiddlePage = totalPages - 1;
        if (middleEnd > lastMiddlePage)
        {
            middleEnd = lastMiddlePage;
            middleStart = middleEnd - middleCount + 1;
        }

        items.Add(Page(1));
        if (middleStart > 2)
        {
            items.Add(Ellipsis());
        }

        for (var page = middleStart; page <= middleEnd; page++)
        {
            items.Add(Page(page));
        }

        if (middleEnd < lastMiddlePage)
        {
            items.Add(Ellipsis());
        }

        items.Add(Page(totalPages));
        return items;
    }

    private static BootstrapPaginationItem Page(int page)
    {
        return new BootstrapPaginationItem(BootstrapPaginationItemKind.Page, page);
    }

    private static BootstrapPaginationItem Ellipsis()
    {
        return new BootstrapPaginationItem(BootstrapPaginationItemKind.Ellipsis, 0);
    }
}
