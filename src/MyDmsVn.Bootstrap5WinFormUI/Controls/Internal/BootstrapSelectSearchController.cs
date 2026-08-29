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

    internal int BeginQuery(string searchText, int pageSize)
    {
        ThrowIfDisposed();
        if (searchText is null) throw new ArgumentNullException(nameof(searchText));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        CancelRequest();
        _generation++;
        _requestCancellation = new CancellationTokenSource();
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
        var cancellation = _requestCancellation ?? throw new InvalidOperationException("BeginQuery must be called before loading results.");
        var query = new BootstrapSelectQuery(_state.SearchText, 1, _state.PageSize);
        try
        {
            var page = await provider.SearchAsync(query, cancellation.Token).ConfigureAwait(false);
            if (page is null) throw new InvalidOperationException("BootstrapSelect data providers must return a non-null page.");
            if (!IsCurrentGeneration(generation)) return;
            PublishFirstPage(page);
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
            _state.Results = BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Error, error.Message);
        }
    }

    internal void Invalidate()
    {
        if (_disposed) return;
        CancelRequest();
        _generation++;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelRequest();
        _generation++;
    }

    private void PublishFirstPage(BootstrapSelectPage page)
    {
        _state.LoadedItems.Clear();
        for (var i = 0; i < page.Items.Count; i++)
        {
            var item = page.Items[i];
            var duplicate = false;
            for (var existingIndex = 0; existingIndex < _state.LoadedItems.Count; existingIndex++)
            {
                if (_valueComparer.Equals(_state.LoadedItems[existingIndex].Value, item.Value))
                {
                    duplicate = true;
                    break;
                }
            }
            if (duplicate) continue;
            _state.LoadedItems.Add(item);
            _refreshSelectedItem(item);
        }
        _state.CurrentPage = 1;
        _state.HasMore = page.HasMore;
        _state.LastError = null;
        _state.Results = _state.LoadedItems.Count == 0
            ? BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Empty, "No results found.")
            : BootstrapSelectResultBuilder.BuildLoaded(_state.LoadedItems, _isSelected);
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
