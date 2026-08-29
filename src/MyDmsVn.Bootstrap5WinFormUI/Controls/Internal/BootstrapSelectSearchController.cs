using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectSearchController : IDisposable
{
    private readonly IEqualityComparer<object> _valueComparer;
    private readonly Func<BootstrapSelectItem, bool> _isSelected;
    private readonly Action<BootstrapSelectItem> _refreshSelectedItem;
    private readonly BootstrapSelectSearchState _state = new BootstrapSelectSearchState();
    private CancellationTokenSource? _requestCancellation;
    private int _generation;
    private int _failedPage;
    private bool _isLoadingMore;
    private bool _disposed;

    internal BootstrapSelectSearchController(
        IEqualityComparer<object> valueComparer,
        Func<BootstrapSelectItem, bool> isSelected,
        Action<BootstrapSelectItem> refreshSelectedItem)
    {
        _valueComparer = valueComparer ?? throw new ArgumentNullException(nameof(valueComparer));
        _isSelected = isSelected ?? throw new ArgumentNullException(nameof(isSelected));
        _refreshSelectedItem = refreshSelectedItem ?? throw new ArgumentNullException(nameof(refreshSelectedItem));
    }

    internal string SearchText => _state.SearchText;
    internal int CurrentPage => _state.CurrentPage;
    internal bool HasMore => _state.HasMore;
    internal IReadOnlyList<BootstrapSelectItem> LoadedItems => new ReadOnlyCollection<BootstrapSelectItem>(_state.LoadedItems);
    internal BootstrapSelectResultSet Results => _state.Results;
    internal Exception? LastError => _state.LastError;
    internal int Generation => _generation;
    internal int FailedPage => _failedPage;
    internal bool IsLoadingMore => _isLoadingMore;

    internal int BeginQuery(string searchText, int pageSize)
    {
        ThrowIfDisposed();
        if (searchText is null) throw new ArgumentNullException(nameof(searchText));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        CancelRequest();
        _generation++;
        _requestCancellation = new CancellationTokenSource();
        _failedPage = 0;
        _isLoadingMore = false;
        _state.Reset(searchText, pageSize);
        return _generation;
    }

    internal bool IsCurrentGeneration(int generation)
    {
        return !_disposed && generation == _generation;
    }

    internal async Task LoadFirstPageAsync(IBootstrapSelectDataProvider provider, int generation)
    {
        ThrowIfDisposed();
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (!IsCurrentGeneration(generation)) return;
        var cancellation = GetCurrentCancellation();
        var query = new BootstrapSelectQuery(_state.SearchText, 1, _state.PageSize);
        try
        {
            var page = await provider.SearchAsync(query, cancellation.Token).ConfigureAwait(false);
            if (page is null) throw new InvalidOperationException("BootstrapSelect data providers must return a non-null page.");
            if (!IsCurrentGeneration(generation)) return;
            _state.LoadedItems.Clear();
            MergeItems(page.Items);
            PublishSuccess(1, page.HasMore);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            if (!IsCurrentGeneration(generation)) return;
            _state.CurrentPage = 0;
            _state.HasMore = false;
            _state.LoadedItems.Clear();
            _state.LastError = error;
            _failedPage = 1;
            _state.Results = BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Error, error.Message);
        }
    }

    internal Task<bool> LoadNextPageAsync(IBootstrapSelectDataProvider provider, int generation)
    {
        ThrowIfDisposed();
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (!IsCurrentGeneration(generation) || _state.CurrentPage < 1 || !_state.HasMore || _isLoadingMore || _failedPage > 0)
        {
            return Task.FromResult(false);
        }

        return LoadAdditionalPageAsync(provider, generation, _state.CurrentPage + 1);
    }

    internal Task<bool> RetryLastFailureAsync(IBootstrapSelectDataProvider provider, int generation)
    {
        ThrowIfDisposed();
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (!IsCurrentGeneration(generation) || _failedPage <= 1 || _isLoadingMore)
        {
            return Task.FromResult(false);
        }

        return LoadAdditionalPageAsync(provider, generation, _failedPage);
    }

    internal void Invalidate()
    {
        if (_disposed) return;
        CancelRequest();
        _generation++;
        _isLoadingMore = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelRequest();
        _generation++;
        _isLoadingMore = false;
    }

    private async Task<bool> LoadAdditionalPageAsync(IBootstrapSelectDataProvider provider, int generation, int pageNumber)
    {
        var cancellation = GetCurrentCancellation();
        var query = new BootstrapSelectQuery(_state.SearchText, pageNumber, _state.PageSize);
        _isLoadingMore = true;
        try
        {
            var page = await provider.SearchAsync(query, cancellation.Token).ConfigureAwait(false);
            if (page is null) throw new InvalidOperationException("BootstrapSelect data providers must return a non-null page.");
            if (!IsCurrentGeneration(generation)) return false;
            MergeItems(page.Items);
            PublishSuccess(pageNumber, page.HasMore);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            if (!IsCurrentGeneration(generation)) return false;
            _state.LastError = error;
            _failedPage = pageNumber;
            _state.Results = BuildLoadMoreErrorResults(error);
            return false;
        }
        finally
        {
            if (IsCurrentGeneration(generation)) _isLoadingMore = false;
        }
    }

    private void MergeItems(IReadOnlyList<BootstrapSelectItem> items)
    {
        for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            var item = items[itemIndex];
            var existingIndex = FindLoadedIndex(item.Value);
            if (existingIndex >= 0) _state.LoadedItems[existingIndex] = item;
            else _state.LoadedItems.Add(item);
            _refreshSelectedItem(item);
        }
    }

    private int FindLoadedIndex(object value)
    {
        for (var i = 0; i < _state.LoadedItems.Count; i++)
        {
            if (_valueComparer.Equals(_state.LoadedItems[i].Value, value)) return i;
        }
        return -1;
    }

    private void PublishSuccess(int pageNumber, bool hasMore)
    {
        _state.CurrentPage = pageNumber;
        _state.HasMore = hasMore;
        _state.LastError = null;
        _failedPage = 0;
        _state.Results = BuildLoadedResults();
    }

    private BootstrapSelectResultSet BuildLoadedResults()
    {
        return _state.LoadedItems.Count == 0
            ? BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Empty, "No results found.")
            : BootstrapSelectResultBuilder.BuildLoaded(_state.LoadedItems, _isSelected);
    }

    private BootstrapSelectResultSet BuildLoadMoreErrorResults(Exception error)
    {
        var normal = BuildLoadedResults();
        var rows = new List<BootstrapSelectResultRow>(normal.Rows.Count + 1);
        for (var i = 0; i < normal.Rows.Count; i++) rows.Add(normal.Rows[i]);
        rows.Add(BootstrapSelectResultRow.Message(BootstrapSelectResultRowKind.LoadMoreError, "Retry loading more: " + error.Message));
        return new BootstrapSelectResultSet(rows);
    }

    private CancellationTokenSource GetCurrentCancellation()
    {
        return _requestCancellation ?? throw new InvalidOperationException("BeginQuery must be called before loading results.");
    }

    private void CancelRequest()
    {
        var cancellation = _requestCancellation;
        _requestCancellation = null;
        if (cancellation is null) return;
        try { cancellation.Cancel(); }
        finally { cancellation.Dispose(); }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BootstrapSelectSearchController));
    }
}
