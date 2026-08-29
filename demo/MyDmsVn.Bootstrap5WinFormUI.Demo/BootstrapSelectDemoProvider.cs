using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class BootstrapSelectDemoProvider : IBootstrapSelectDataProvider
{
    private readonly List<BootstrapSelectItem> _items;
    private int _firstPageFailureIssued;
    private int _retryFailureIssued;

    internal BootstrapSelectDemoProvider()
    {
        _items = new List<BootstrapSelectItem>();
        for (var i = 1; i <= 240; i++)
        {
            _items.Add(new BootstrapSelectItem(i, "Customer " + i.ToString("000"))
            {
                Group = i <= 120 ? "North region" : "South region"
            });
        }

        for (var i = 1; i <= 36; i++)
        {
            _items.Add(new BootstrapSelectItem(800 + i, "Fail-first sample " + i.ToString("00"))
            {
                Group = "First-page retry samples"
            });
        }

        for (var i = 1; i <= 48; i++)
        {
            _items.Add(new BootstrapSelectItem(1000 + i, "Retry sample " + i.ToString("00"))
            {
                Group = "Later-page retry samples"
            });
        }

        for (var i = 1; i <= 24; i++)
        {
            _items.Add(new BootstrapSelectItem(2000 + i, "Race sample " + i.ToString("00"))
            {
                Group = "Race samples"
            });
        }
    }

    public async Task<BootstrapSelectPage> SearchAsync(
        BootstrapSelectQuery query,
        CancellationToken cancellationToken)
    {
        var searchText = query.SearchText.Trim();
        var delay = searchText.StartsWith("race", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(80, 520 - (searchText.Length * 70))
            : 260;

        if (searchText.StartsWith("race", StringComparison.OrdinalIgnoreCase))
        {
            // Deliberately ignore cancellation for this scenario so the control's
            // logical generation guard is visible even with an uncooperative provider.
            await Task.Delay(delay).ConfigureAwait(false);
        }
        else
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(searchText, "fail-first", StringComparison.OrdinalIgnoreCase)
            && query.Page == 1
            && Interlocked.Exchange(ref _firstPageFailureIssued, 1) == 0)
        {
            throw new InvalidOperationException(
                "Demo page-1 failure. Activate the retry row to rerun the same query.");
        }

        if (string.Equals(searchText, "retry", StringComparison.OrdinalIgnoreCase)
            && query.Page == 2
            && Interlocked.Exchange(ref _retryFailureIssued, 1) == 0)
        {
            throw new InvalidOperationException(
                "Demo page-2 failure. Activate the retry row to continue.");
        }

        var filtered = _items
            .Where(item => searchText.Length == 0
                || item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
        var start = (query.Page - 1) * query.PageSize;
        var pageItems = filtered.Skip(start).Take(query.PageSize).ToList();
        return new BootstrapSelectPage(pageItems, start + pageItems.Count < filtered.Count);
    }
}
