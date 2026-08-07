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
        if (await _queryExecutor.AnyAsync(_dbContext.Users.Where(u => u.Email == normalizedEmail), cancellationToken))
        {
            return Result<CreateUserResult>.Failure("email_already_in_use");
        }

        Role? role = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Roles.Where(r => r.Id == request.RoleId), cancellationToken);
        Result<CreateUserResult>? guardFailure = ValidateRole(role, request.ActorIsSuperAdmin);
        if (guardFailure is not null || role is null)
        {
            return guardFailure!.Value;
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(request.Password) ? _passwordGenerator.Generate() : null;
        User user = CreateUser(request, normalizedEmail, temporaryPassword ?? request.Password!);
        _dbContext.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Add(CreateRoleAssignment(request.ActorUserId, user.Id, role.Id));
        await _dbContext.SaveChangesAsync(cancellationToken);

        await RecordCreationAuditAsync(request.ActorUserId, user, role, cancellationToken);

        var dto = new UserDetailDto(user.Id, user.Email, user.Name, user.Title, user.Department, new[] { role.Name }, user.IsActive, user.IsLocked, user.MustChangePassword, user.LastLogin, user.CreatedAt);
        return Result<CreateUserResult>.Success(new CreateUserResult(dto, temporaryPassword));
    }

    /// <summary>
    /// Validates the target role exists and, per SEC-01, that a non-super-admin actor is not
    /// pre-assigning the <c>super_admin</c> role via account creation.
    /// </summary>
    /// <returns>A failure result if a guard is violated; otherwise <see langword="null"/>.</returns>
    private static Result<CreateUserResult>? ValidateRole(Role? role, bool actorIsSuperAdmin)
    {
        if (role is null)
        {
            return Result<CreateUserResult>.Failure("role_not_found");
        }

        // Why: SEC-01 — creating a user with the super_admin role pre-assigned is the same
        // escalation vector as AssignUserRoleCommand; a plain admin must not be able to bypass
        // that enforcement point by routing through account creation instead of role assignment.
        if (string.Equals(role.Name, SuperAdminRoleName, StringComparison.OrdinalIgnoreCase) && !actorIsSuperAdmin)
        {
            return Result<CreateUserResult>.Failure("forbidden_super_admin_grant");
        }

        return null;
    }

    /// <summary>Builds the initial <see cref="UserRole"/> assignment for a newly created user.</summary>
    private static UserRole CreateRoleAssignment(int actorUserId, int userId, int roleId) => new()
    {
        UserId = userId,
        RoleId = roleId,
        AssignedById = actorUserId,
        AssignedAt = DateTime.UtcNow,
        CreatedBy = actorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>Builds the new <see cref="User"/> entity from the command, with the effective password hashed.</summary>
    private User CreateUser(CreateUserCommand request, string normalizedEmail, string effectivePassword) => new()
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

    /// <summary>Writes the privileged-action audit-log entry for the newly created user.</summary>
    private Task RecordCreationAuditAsync(int actorUserId, User user, Role role, CancellationToken cancellationToken) =>
        _auditLog.RecordAsync(
            actorUserId,
            "user.create",
            "User",
            user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { user.Email, UserName = user.Name, RoleId = role.Id, RoleName = role.Name },
            cancellationToken);
}
