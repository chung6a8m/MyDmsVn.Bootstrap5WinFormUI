using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

internal sealed class BootstrapSelectImmediateProvider : IBootstrapSelectDataProvider
{
    private readonly Func<BootstrapSelectQuery, BootstrapSelectPage> _factory;

    internal BootstrapSelectImmediateProvider(Func<BootstrapSelectQuery, BootstrapSelectPage> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal List<BootstrapSelectQuery> Queries { get; } = new List<BootstrapSelectQuery>();

    public Task<BootstrapSelectPage> SearchAsync(BootstrapSelectQuery query, CancellationToken cancellationToken)
    {
        Queries.Add(query);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_factory(query));
    }
}

internal sealed class BootstrapSelectControlledProvider : IBootstrapSelectDataProvider
{
    private readonly bool _honorCancellation;
    private readonly Dictionary<string, TaskCompletionSource<BootstrapSelectPage>> _pending = new Dictionary<string, TaskCompletionSource<BootstrapSelectPage>>(StringComparer.Ordinal);

    internal BootstrapSelectControlledProvider(bool honorCancellation)
    {
        _honorCancellation = honorCancellation;
    }

    internal List<BootstrapSelectQuery> Queries { get; } = new List<BootstrapSelectQuery>();
    internal List<CancellationToken> Tokens { get; } = new List<CancellationToken>();

    public Task<BootstrapSelectPage> SearchAsync(BootstrapSelectQuery query, CancellationToken cancellationToken)
    {
        Queries.Add(query);
        Tokens.Add(cancellationToken);
        var tcs = new TaskCompletionSource<BootstrapSelectPage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[query.SearchText + "#" + query.Page] = tcs;
        if (_honorCancellation)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }
        return tcs.Task;
    }

    internal void Complete(string searchText, int page, IEnumerable<BootstrapSelectItem> items, bool hasMore = false)
    {
        _pending[searchText + "#" + page].TrySetResult(new BootstrapSelectPage(items, hasMore));
    }

    internal void Fail(string searchText, int page, Exception error)
    {
        _pending[searchText + "#" + page].TrySetException(error);
    }
}
