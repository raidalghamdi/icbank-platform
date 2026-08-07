using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Handles <see cref="ResetUserPasswordCommand"/>. The generated temporary password is returned
/// exactly once, in the response body only (never logged, R-BE-054), and the account is flagged
/// <see cref="User.MustChangePassword"/> so the temp password cannot be used beyond first login.
/// </summary>
public sealed class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, Result<string>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IResourceAuthorizationService _resourceAuthorization;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITemporaryPasswordGenerator _passwordGenerator;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="ResetUserPasswordCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-level authorization port.</param>
    /// <param name="passwordHasher">The password hashing port.</param>
    /// <param name="passwordGenerator">The temporary-password generation port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public ResetUserPasswordCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IResourceAuthorizationService resourceAuthorization,
        IPasswordHasher passwordHasher,
        ITemporaryPasswordGenerator passwordGenerator,
        IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _resourceAuthorization = resourceAuthorization;
        _passwordHasher = passwordHasher;
        _passwordGenerator = passwordGenerator;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<string>> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeUserResourceAsync(
            request.ActorUserId, request.ActorIsSuperAdmin, request.TargetUserId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return Result<string>.Failure(authorization.Outcome == ResourceAuthorizationOutcome.NotFound ? "user_not_found" : "forbidden_peer_resource");
        }

        User user = await _queryExecutor.SingleOrDefaultAsync(_dbContext.Users.Where(u => u.Id == request.TargetUserId), cancellationToken)
            ?? throw new InvalidOperationException("User existence was already confirmed by resource authorization.");

        var temporaryPassword = _passwordGenerator.Generate();
        user.PasswordHash = _passwordHasher.HashPassword(temporaryPassword);
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "user.password.reset",
            "User",
            user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { MustChangePassword = true },
            cancellationToken);

        return Result<string>.Success(temporaryPassword);
    }
}
