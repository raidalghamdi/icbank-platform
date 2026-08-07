using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="UpdateRoleCommand"/>.</summary>
public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="UpdateRoleCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public UpdateRoleCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IResourceAuthorizationService resourceAuthorization, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeRoleResourceAsync(request.RoleId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return Result<bool>.Failure("role_not_found");
        }

        Role role = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Roles.Where(r => r.Id == request.RoleId), cancellationToken)
            ?? throw new InvalidOperationException("Role existence was already confirmed by resource authorization.");

        var before = new { role.NameAr, role.Description };
        role.NameAr = request.NameAr ?? role.NameAr;
        role.Description = request.Description ?? role.Description;
        role.UpdatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "role.update",
            "Role",
            role.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before,
            after: new { role.NameAr, role.Description },
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
