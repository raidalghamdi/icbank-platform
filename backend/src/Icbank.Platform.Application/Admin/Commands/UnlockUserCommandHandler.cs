using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="UnlockUserCommand"/>.</summary>
public sealed class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="UnlockUserCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public UnlockUserCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.Id == request.TargetUserId), cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure("user_not_found");
        }

        var wasLocked = user.IsLocked;
        user.IsLocked = false;
        user.FailedAttempts = 0;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLog.RecordAsync(
            request.ActorUserId,
            "user.unlock",
            "User",
            request.TargetUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { isLocked = wasLocked },
            after: new { isLocked = false },
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
