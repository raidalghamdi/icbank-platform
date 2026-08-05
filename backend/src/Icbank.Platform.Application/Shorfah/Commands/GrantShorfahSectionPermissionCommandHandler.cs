using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="GrantShorfahSectionPermissionCommand"/>. Ports <c>shorfah.ts:516-528</c>.</summary>
public sealed class GrantShorfahSectionPermissionCommandHandler : IRequestHandler<GrantShorfahSectionPermissionCommand, Result<ShorfahSectionPermissionDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="GrantShorfahSectionPermissionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public GrantShorfahSectionPermissionCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahSectionPermissionDto>> Handle(GrantShorfahSectionPermissionCommand request, CancellationToken cancellationToken)
    {
        var sectionExists = await _queryExecutor.AnyAsync(_dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (!sectionExists)
        {
            return Result<ShorfahSectionPermissionDto>.Failure("القسم غير موجود");
        }

        if (!Enum.TryParse<ShorfahPermissionVerb>(request.Permission, ignoreCase: true, out ShorfahPermissionVerb permissionVerb))
        {
            return Result<ShorfahSectionPermissionDto>.Failure("صلاحية غير صالحة");
        }

        var grant = new ShorfahSectionPermission
        {
            SectionId = request.SectionId,
            UserId = request.UserId,
            RoleName = request.RoleName,
            Permission = permissionVerb,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        };
        _dbContext.Add(grant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_permission.grant",
            "ShorfahSectionPermission",
            ShorfahMappers.IdString(grant.Id),
            before: null,
            after: new { grant.SectionId, grant.UserId, grant.RoleName, grant.Permission },
            cancellationToken);

        return Result<ShorfahSectionPermissionDto>.Success(
            new ShorfahSectionPermissionDto(grant.Id, grant.SectionId, grant.UserId, grant.RoleName, grant.Permission.ToString()));
    }
}
