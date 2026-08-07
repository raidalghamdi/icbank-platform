namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Application-layer port for executing <see cref="IQueryable{T}"/> terminal operators
/// asynchronously without Application referencing EF Core directly (R-BE-002). Infrastructure's
/// implementation delegates to EF Core's <c>Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions</c>;
/// any other <see cref="IQueryable{T}"/> provider (e.g. an in-memory list in tests) can implement
/// this against <c>System.Linq</c> synchronously wrapped in <see cref="Task.FromResult{TResult}"/>.
/// </summary>
public interface IAsyncQueryExecutor
{
    /// <summary>Asynchronously returns the single element satisfying the query, or <c>null</c> if none/more than one exist is an error per LINQ semantics.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The single matching element, or <c>null</c> if none exists.</returns>
    Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        where T : class;

    /// <summary>Asynchronously materializes the query into a list.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The materialized list.</returns>
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);

    /// <summary>Asynchronously returns whether any element satisfies the query.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if any element matches.</returns>
    Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);
}
