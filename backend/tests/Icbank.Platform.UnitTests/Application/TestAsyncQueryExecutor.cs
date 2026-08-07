using Icbank.Platform.Application.Common.Interfaces;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>
/// A real (non-mock) <see cref="IAsyncQueryExecutor"/> that executes queries synchronously over
/// in-memory <see cref="IQueryable{T}"/> sequences, matching the note on the interface itself
/// that any LINQ-to-objects provider can implement it via <see cref="Task.FromResult{TResult}"/>.
/// Used by handler unit tests so filter/order/paging LINQ expressions in the handler under test
/// are exercised for real instead of being short-circuited by a mock.
/// </summary>
public sealed class TestAsyncQueryExecutor : IAsyncQueryExecutor
{
    /// <inheritdoc />
    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        where T : class
        => Task.FromResult(query.SingleOrDefault());

    /// <inheritdoc />
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => Task.FromResult(query.ToList());

    /// <inheritdoc />
    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => Task.FromResult(query.Any());
}
