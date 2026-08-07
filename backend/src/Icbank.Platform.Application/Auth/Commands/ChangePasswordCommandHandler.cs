using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Handles a first-login or voluntary self-service password change.</summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="ChangePasswordCommandHandler"/> class.</summary>
    public ChangePasswordCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(candidate => candidate.Id == request.UserId), cancellationToken);
        if (user is null || !user.IsActive || user.IsLocked || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Result<bool>.Failure("account_unavailable");
        }

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.CurrentPassword))
        {
            return Result<bool>.Failure("invalid_current_password");
        }

        var wasRequired = user.MustChangePassword;
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedBy = request.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.UserId,
            "user.password.change",
            "User",
            request.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { MustChangePassword = wasRequired },
            after: new { MustChangePassword = false },
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
