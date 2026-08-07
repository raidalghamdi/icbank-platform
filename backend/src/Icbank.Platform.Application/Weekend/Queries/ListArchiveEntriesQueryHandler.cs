using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="ListArchiveEntriesQuery"/>.</summary>
public sealed class ListArchiveEntriesQueryHandler : IRequestHandler<ListArchiveEntriesQuery, Result<PagedResult<ArchiveEntryDto>>>
{
    private const int PreviewMaxLength = 200;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListArchiveEntriesQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListArchiveEntriesQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ArchiveEntryDto>>> Handle(ListArchiveEntriesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ArchiveEntry> ordered = _dbContext.ArchiveEntries.OrderByDescending(e => e.CreatedAt);
        var total = (await _queryExecutor.ToListAsync(ordered, cancellationToken)).Count;

        List<ArchiveEntry> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(ToDto).ToList();
        return Result<PagedResult<ArchiveEntryDto>>.Success(new PagedResult<ArchiveEntryDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static ArchiveEntryDto ToDto(ArchiveEntry entry)
    {
        var body = entry.BodyText;
        var preview = body.Length > PreviewMaxLength ? body[..PreviewMaxLength] : body;
        return new ArchiveEntryDto(entry.Id, entry.Title, entry.Occasion, entry.Tone, entry.SourceFile, entry.CreatedAt, preview);
    }
}
