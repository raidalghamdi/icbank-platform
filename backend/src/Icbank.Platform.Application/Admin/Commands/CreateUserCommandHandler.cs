using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Handles <see cref="CreateUserCommand"/>. Closes SEC-01 in depth: creating a user
/// pre-assigned the <c>super_admin</c> role is refused unless the actor already holds that
/// capability, exactly mirroring <see cref="AssignUserRoleCommandHandler"/>'s enforcement point.
/// </summary>
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResult>>
{
    private const string SuperAdminRoleName = "super_admin";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITemporaryPasswordGenerator _passwordGenerator;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="CreateUserCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="passwordHasher">The password hashing port.</param>
    /// <param name="passwordGenerator">The temporary-password generation port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public CreateUserCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IPasswordHasher passwordHasher,
        ITemporaryPasswordGenerator passwordGenerator,
        IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _passwordHasher = passwordHasher;
        _passwordGenerator = passwordGenerator;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<CreateUserResult>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await _queryExecutor.AnyAsync(_dbContext.Users.Where(u => u.Email == normalizedEmail), cancellationToken);
        if (emailTaken)
        {
            return Result<CreateUserResult>.Failure("email_already_in_use");
        }

        Role? role = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Roles.Where(r => r.Id == request.RoleId), cancellationToken);
        if (role is null)
        {
            return Result<CreateUserResult>.Failure("role_not_found");
        }

        // Why: SEC-01 — creating a user with the super_admin role pre-assigned is the same
        // escalation vector as AssignUserRoleCommand; a plain admin must not be able to bypass
        // that enforcement point by routing through account creation instead of role assignment.
        if (string.Equals(role.Name, SuperAdminRoleName, StringComparison.OrdinalIgnoreCase) && !request.ActorIsSuperAdmin)
        {
            return Result<CreateUserResult>.Failure("forbidden_super_admin_grant");
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(request.Password) ? _passwordGenerator.Generate() : null;
        var effectivePassword = temporaryPassword ?? request.Password!;

        var user = new User
        {
            Email = normalizedEmail,
            Name = request.Name.Trim(),
            Title = request.Title,
            Department = request.Department,
            PasswordHash = _passwordHasher.HashPassword(effectivePassword),
            IsActive = true,
            MustChangePassword = true,
            CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        _dbContext.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedById = request.ActorUserId,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "user.create",
            "User",
            user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { user.Email, UserName = user.Name, RoleId = role.Id, RoleName = role.Name },
            cancellationToken);

        var dto = new UserDetailDto(user.Id, user.Email, user.Name, user.Title, user.Department, new[] { role.Name }, user.IsActive, user.IsLocked, user.MustChangePassword, user.LastLogin, user.CreatedAt);
        return Result<CreateUserResult>.Success(new CreateUserResult(dto, temporaryPassword));
    }
}
