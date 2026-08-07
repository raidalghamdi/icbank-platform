using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>
/// Handles <see cref="SsoCallbackCommand"/> (BUSINESS-RULES.md §11.3: lookup by Azure OID, then
/// by email — linking the OID onto an existing password account if found — else auto-provision a
/// new user defaulting to <c>viewer</c>; enforce the optional domain restriction).
/// </summary>
public sealed class SsoCallbackCommandHandler : IRequestHandler<SsoCallbackCommand, Result<SsoCallbackResultDto>>
{
    private const string DefaultSsoRoleName = "viewer";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAzureAdClient _azureAdClient;
    private readonly ISsoStateStore _stateStore;
    private readonly ISsoOptionsProvider _options;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly AuthSessionFactory _sessionFactory;

    /// <summary>Initializes a new instance of the <see cref="SsoCallbackCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="azureAdClient">The Azure AD PKCE client port.</param>
    /// <param name="stateStore">The server-side PKCE state store.</param>
    /// <param name="options">The Azure AD SSO configuration.</param>
    /// <param name="refreshTokenService">The refresh-token issuance port.</param>
    /// <param name="permissionResolver">The effective-permission resolution port.</param>
    /// <param name="sessionFactory">Builds the access token + DTO pair shared by all auth endpoints.</param>
    public SsoCallbackCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IAzureAdClient azureAdClient,
        ISsoStateStore stateStore,
        ISsoOptionsProvider options,
        IRefreshTokenService refreshTokenService,
        IPermissionResolver permissionResolver,
        AuthSessionFactory sessionFactory)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _azureAdClient = azureAdClient;
        _stateStore = stateStore;
        _options = options;
        _refreshTokenService = refreshTokenService;
        _permissionResolver = permissionResolver;
        _sessionFactory = sessionFactory;
    }

    /// <inheritdoc />
    public async Task<Result<SsoCallbackResultDto>> Handle(SsoCallbackCommand request, CancellationToken cancellationToken)
    {
        (string CodeVerifier, string RedirectTarget)? state = await _stateStore.ConsumeAsync(request.State, cancellationToken);
        if (state is null)
        {
            return Result<SsoCallbackResultDto>.Failure("invalid_or_expired_state");
        }

        AzureAdUserInfo azureUser = await _azureAdClient.ExchangeCodeAsync(request.Code, state.Value.CodeVerifier, cancellationToken);

        if (!IsDomainAllowed(azureUser.Email))
        {
            return Result<SsoCallbackResultDto>.Failure("domain_not_allowed");
        }

        User user = await FindOrProvisionUserAsync(azureUser, cancellationToken);

        if (!user.IsActive || user.IsLocked)
        {
            return Result<SsoCallbackResultDto>.Failure("account_unavailable");
        }

        PermissionResolution resolution = await _permissionResolver.ResolveAsync(user.Id, cancellationToken);
        (AccessTokenResult accessToken, AuthenticatedUserDto userDto) =
            _sessionFactory.BuildSession(user, resolution, user.MustChangePassword);
        var rawRefreshToken = await _refreshTokenService.IssueAsync(user.Id, request.IpAddress, cancellationToken);

        var login = new LoginResultDto(accessToken.AccessToken, accessToken.ExpiresAtUtc, rawRefreshToken, userDto);
        return Result<SsoCallbackResultDto>.Success(new SsoCallbackResultDto(login, state.Value.RedirectTarget));
    }

    private bool IsDomainAllowed(string email)
    {
        if (string.IsNullOrWhiteSpace(_options.AllowedDomain))
        {
            return true;
        }

        return email.EndsWith("@" + _options.AllowedDomain, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<User> FindOrProvisionUserAsync(AzureAdUserInfo azureUser, CancellationToken cancellationToken)
    {
        User? user = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.AzureOid == azureUser.AzureObjectId), cancellationToken);

        var normalizedEmail = azureUser.Email.Trim().ToLowerInvariant();
        user ??= await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Users.Where(u => u.Email == normalizedEmail), cancellationToken);

        if (user is not null)
        {
            user.AzureOid ??= azureUser.AzureObjectId;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }

        return await ProvisionNewUserAsync(azureUser, normalizedEmail, cancellationToken);
    }

    private async Task<User> ProvisionNewUserAsync(AzureAdUserInfo azureUser, string normalizedEmail, CancellationToken cancellationToken)
    {
        var newUser = new User
        {
            Email = normalizedEmail,
            Name = azureUser.Name,
            AzureOid = azureUser.AzureObjectId,
            IsActive = true,
        };
        _dbContext.Add(newUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        Role? defaultRole = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Roles.Where(r => r.Name == DefaultSsoRoleName), cancellationToken);
        if (defaultRole is not null)
        {
            _dbContext.Add(new UserRole { UserId = newUser.Id, RoleId = defaultRole.Id, AssignedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return newUser;
    }
}
