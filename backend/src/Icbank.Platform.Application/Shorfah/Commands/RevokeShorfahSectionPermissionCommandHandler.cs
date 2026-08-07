using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="RevokeShorfahSectionPermissionCommand"/>. Ports <c>shorfah.ts:529-533</c>.</summary>
public sealed class RevokeShorfahSectionPermissionCommandHandler : IRequestHandler<RevokeShorfahSectionPermissionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="RevokeShorfahSectionPermissionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public RevokeShorfahSectionPermissionCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(RevokeShorfahSectionPermissionCommand request, CancellationToken cancellationToken)
    {
        ShorfahSectionPermission? grant = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSectionPermissions.Where(p => p.Id == request.PermissionId), cancellationToken);
        if (grant is null)
        {
            return Result<bool>.Failure("الصلاحية غير موجودة");
        }

        _dbContext.Remove(grant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_permission.revoke",
            "ShorfahSectionPermission",
            ShorfahMappers.IdString(request.PermissionId),
            before: new { grant.SectionId, grant.UserId, grant.RoleName, grant.Permission },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
