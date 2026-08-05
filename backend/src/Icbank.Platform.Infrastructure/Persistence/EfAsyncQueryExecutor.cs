using Icbank.Platform.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed implementation of <see cref="IAsyncQueryExecutor"/> — the only place in the
/// solution where Application-layer handlers' queries actually touch
/// <c>Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions</c>, keeping that
/// dependency confined to Infrastructure per R-BE-002.
/// </summary>
public sealed class EfAsyncQueryExecutor : IAsyncQueryExecutor
{
    /// <inheritdoc />
    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        where T : class => EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(query, cancellationToken);

    /// <inheritdoc />
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
        EntityFrameworkQueryableExtensions.ToListAsync(query, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
        EntityFrameworkQueryableExtensions.AnyAsync(query, cancellationToken);
}
