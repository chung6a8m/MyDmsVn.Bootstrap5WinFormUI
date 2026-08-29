using System;
using System.ComponentModel;
using System.Threading.Tasks;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapSelect
{
    private IBootstrapSelectDataProvider? _dataProvider;
    private BootstrapSelectSearchController? _searchController;
    private BootstrapSelectDebouncer? _searchDebouncer;

    /// <summary>Occurs when an asynchronous provider query starts.</summary>
    public event EventHandler<BootstrapSelectSearchEventArgs>? SearchStarted;

    /// <summary>Occurs after the current asynchronous provider query completes successfully.</summary>
    public event EventHandler<BootstrapSelectSearchCompletedEventArgs>? SearchCompleted;

    /// <summary>Occurs after the current asynchronous provider query fails with a non-cancellation exception.</summary>
    public event EventHandler<BootstrapSelectSearchFailedEventArgs>? SearchFailed;

    /// <summary>Gets or sets the optional transport-agnostic asynchronous provider. Local <see cref="Items"/> are ignored while a provider is set.</summary>
    [Browsable(false)]
    public IBootstrapSelectDataProvider? DataProvider
    {
        get => _dataProvider;
        set
        {
            if (ReferenceEquals(_dataProvider, value)) return;
            _dataProvider = value;
            CancelRemoteSearch();
            if (_dropDownController?.IsOpen == true)
            {
                _dropDownController.RefreshResults();
                if (value is not null) NotifyPopupSearchTextChanged(_dropDownController.CurrentSearchText);
            }
        }
    }

    internal BootstrapSelectResultSet BuildCurrentPopupResultSet(string searchText)
    {
        if (_dataProvider is null) return BuildCurrentLocalResultSet(searchText);
        var effectiveText = SearchEnabled ? searchText : string.Empty;
        if (effectiveText.Length < MinimumSearchLength)
        {
            return BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Instruction,
                "Type at least " + MinimumSearchLength + " character(s) to search.");
        }
        if (_searchController is null || !string.Equals(_searchController.SearchText, effectiveText, StringComparison.Ordinal))
        {
            return BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Loading, "Loading...");
        }
        return _searchController.Results;
    }

    internal void NotifyPopupSearchTextChanged(string searchText)
    {
        if (_dataProvider is null) return;
        var effectiveText = SearchEnabled ? searchText : string.Empty;
        if (effectiveText.Length < MinimumSearchLength)
        {
            CancelRemoteSearch();
            _dropDownController?.RefreshResults();
            return;
        }
        _searchDebouncer ??= new BootstrapSelectDebouncer();
        _searchDebouncer.Schedule(SearchDebounce, () => _ = StartRemoteSearchAsync(effectiveText, _dataProvider));
    }

    internal void InvalidateRemoteSearchOnClose()
    {
        _searchDebouncer?.Cancel();
        _searchController?.Invalidate();
    }

    internal void DisposeSearchInfrastructure()
    {
        _searchDebouncer?.Dispose();
        _searchDebouncer = null;
        _searchController?.Dispose();
        _searchController = null;
    }

    private async Task StartRemoteSearchAsync(string searchText, IBootstrapSelectDataProvider? provider)
    {
        if (provider is null || !ReferenceEquals(provider, _dataProvider) || IsDisposed) return;
        var controller = EnsureSearchController();
        var generation = controller.BeginQuery(searchText, PageSize);
        SearchStarted?.Invoke(this, new BootstrapSelectSearchEventArgs(searchText, 1));
        _dropDownController?.RefreshResults();
        try
        {
            await controller.LoadFirstPageAsync(provider, generation);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (IsDisposed || !ReferenceEquals(provider, _dataProvider) || !controller.IsCurrentGeneration(generation)) return;
        _dropDownController?.RefreshResults();
        if (controller.LastError is Exception error)
        {
            SearchFailed?.Invoke(this, new BootstrapSelectSearchFailedEventArgs(searchText, 1, error));
        }
        else
        {
            SearchCompleted?.Invoke(this, new BootstrapSelectSearchCompletedEventArgs(searchText, 1, controller.LoadedItems.Count, controller.HasMore));
        }
    }

    private BootstrapSelectSearchController EnsureSearchController()
    {
        _searchController ??= new BootstrapSelectSearchController(ValueComparer, IsItemSelected, RefreshSelectedSnapshot);
        return _searchController;
    }

    private void RefreshSelectedSnapshot(BootstrapSelectItem item)
    {
        _selectionState.RefreshSelectedItem(item);
        Invalidate();
    }

    private void CancelRemoteSearch()
    {
        _searchDebouncer?.Cancel();
        _searchController?.Invalidate();
    }
}
