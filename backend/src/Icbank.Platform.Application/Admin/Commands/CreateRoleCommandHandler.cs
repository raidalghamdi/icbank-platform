using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="CreateRoleCommand"/>. New roles are always created as non-system (<c>IsSystem = false</c>) so they remain deletable.</summary>
public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="CreateRoleCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public CreateRoleCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<RoleSummaryDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var nameTaken = await _queryExecutor.AnyAsync(_dbContext.Roles.Where(r => r.Name == request.Name), cancellationToken);
        if (nameTaken)
        {
            return Result<RoleSummaryDto>.Failure("role_name_already_in_use");
        }

        var role = new Role
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            Description = request.Description,
            IsSystem = false,
            CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        _dbContext.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "role.create",
            "Role",
            role.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { role.Name, role.NameAr, role.Description },
            cancellationToken);

        return Result<RoleSummaryDto>.Success(new RoleSummaryDto(role.Id, role.Name, role.NameAr, role.Description, role.IsSystem, 0));
    }
}
