using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Handles <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly AuthSessionFactory _sessionFactory;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="refreshTokenService">The refresh-token rotation port.</param>
    /// <param name="permissionResolver">The effective-permission resolution port.</param>
    /// <param name="sessionFactory">Builds the access token + DTO pair shared by all auth endpoints.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public RefreshTokenCommandHandler(
        IApplicationDbContext dbContext,
        IRefreshTokenService refreshTokenService,
        IPermissionResolver permissionResolver,
        AuthSessionFactory sessionFactory,
        IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _refreshTokenService = refreshTokenService;
        _permissionResolver = permissionResolver;
        _sessionFactory = sessionFactory;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        (int UserId, string NewRawToken)? rotation =
            await _refreshTokenService.RotateAsync(request.RawRefreshToken, request.IpAddress, cancellationToken);

        if (rotation is null)
        {
            return Result<LoginResultDto>.Failure("invalid_refresh_token");
        }

        User? user = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.Id == rotation.Value.UserId), cancellationToken);
        if (user is null || !user.IsActive || user.IsLocked)
        {
            return Result<LoginResultDto>.Failure("account_unavailable");
        }

        PermissionResolution resolution = await _permissionResolver.ResolveAsync(user.Id, cancellationToken);
        (AccessTokenResult accessToken, AuthenticatedUserDto userDto) =
            _sessionFactory.BuildSession(user, resolution, user.MustChangePassword);

        return Result<LoginResultDto>.Success(
            new LoginResultDto(accessToken.AccessToken, accessToken.ExpiresAtUtc, rotation.Value.NewRawToken, userDto));
    }
}
