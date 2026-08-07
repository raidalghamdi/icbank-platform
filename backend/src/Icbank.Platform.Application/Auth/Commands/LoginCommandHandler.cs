using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>
/// Handles <see cref="LoginCommand"/> (BUSINESS-RULES.md §10.5 account lockout: 5 consecutive
/// failed attempts locks the account; the counter resets to 0 on any successful login).
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResultDto>>
{
    private const int LockoutThreshold = 5;
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly AuthSessionFactory _sessionFactory;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="LoginCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="passwordHasher">The password hashing/verification port.</param>
    /// <param name="refreshTokenService">The refresh-token issuance port.</param>
    /// <param name="permissionResolver">The effective-permission resolution port.</param>
    /// <param name="sessionFactory">Builds the access token + DTO pair shared by all auth endpoints.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public LoginCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IRefreshTokenService refreshTokenService,
        IPermissionResolver permissionResolver,
        AuthSessionFactory sessionFactory,
        IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _refreshTokenService = refreshTokenService;
        _permissionResolver = permissionResolver;
        _sessionFactory = sessionFactory;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? user = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.Email == normalizedEmail), cancellationToken);

        if (user is null || user.PasswordHash is null)
        {
            return Result<LoginResultDto>.Failure("invalid_credentials");
        }

        if (user.IsLocked)
        {
            return Result<LoginResultDto>.Failure("account_locked");
        }

        if (!user.IsActive)
        {
            return Result<LoginResultDto>.Failure("account_inactive");
        }

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            await RecordFailedAttemptAsync(user, cancellationToken);
            return Result<LoginResultDto>.Failure("invalid_credentials");
        }

        user.FailedAttempts = 0;
        user.LastLogin = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        PermissionResolution resolution = await _permissionResolver.ResolveAsync(user.Id, cancellationToken);
        (AccessTokenResult accessToken, AuthenticatedUserDto userDto) =
            _sessionFactory.BuildSession(user, resolution, user.MustChangePassword);

        var rawRefreshToken = await _refreshTokenService.IssueAsync(user.Id, request.IpAddress, cancellationToken);

        return Result<LoginResultDto>.Success(
            new LoginResultDto(accessToken.AccessToken, accessToken.ExpiresAtUtc, rawRefreshToken, userDto));
    }

    private async Task RecordFailedAttemptAsync(User user, CancellationToken cancellationToken)
    {
        user.FailedAttempts += 1;
        if (user.FailedAttempts >= LockoutThreshold)
        {
            user.IsLocked = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
