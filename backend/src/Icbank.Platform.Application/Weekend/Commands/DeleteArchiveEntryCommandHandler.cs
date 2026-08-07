using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="DeleteArchiveEntryCommand"/>.</summary>
public sealed class DeleteArchiveEntryCommandHandler : IRequestHandler<DeleteArchiveEntryCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteArchiveEntryCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeleteArchiveEntryCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteArchiveEntryCommand request, CancellationToken cancellationToken)
    {
        ArchiveEntry? entry = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ArchiveEntries.Where(e => e.Id == request.EntryId), cancellationToken);
        if (entry is null)
        {
            return Result<bool>.Failure("entry غير موجود");
        }

        _dbContext.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "archive_entry.delete",
            "ArchiveEntry",
            request.EntryId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { entry.Title },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
