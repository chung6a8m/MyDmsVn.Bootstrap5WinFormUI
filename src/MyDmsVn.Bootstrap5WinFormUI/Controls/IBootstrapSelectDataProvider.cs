using System.Threading;
using System.Threading.Tasks;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides transport-agnostic asynchronous result pages for <see cref="BootstrapSelect"/>.</summary>
public interface IBootstrapSelectDataProvider
{
    /// <summary>Searches for one page of Select results.</summary>
    /// <param name="query">The immutable search request.</param>
    /// <param name="cancellationToken">Signals that the current logical query is no longer needed.</param>
    /// <returns>The requested result page.</returns>
    Task<BootstrapSelectPage> SearchAsync(BootstrapSelectQuery query, CancellationToken cancellationToken);
}
