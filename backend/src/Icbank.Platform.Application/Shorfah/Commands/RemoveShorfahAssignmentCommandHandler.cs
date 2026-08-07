using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="RemoveShorfahAssignmentCommand"/>. Ports <c>shorfah.ts:883-887</c>.</summary>
public sealed class RemoveShorfahAssignmentCommandHandler : IRequestHandler<RemoveShorfahAssignmentCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="RemoveShorfahAssignmentCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public RemoveShorfahAssignmentCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(RemoveShorfahAssignmentCommand request, CancellationToken cancellationToken)
    {
        ShorfahAssignment? assignment = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahAssignments.Where(a => a.Id == request.AssignmentId), cancellationToken);
        if (assignment is null)
        {
            return Result<bool>.Failure("التكليف غير موجود");
        }

        _dbContext.Remove(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_assignment.delete",
            "ShorfahAssignment",
            ShorfahMappers.IdString(request.AssignmentId),
            before: new { assignment.SectionId, assignment.UserId },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
