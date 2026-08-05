using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Handles <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IRefreshTokenService _refreshTokenService;

    /// <summary>Initializes a new instance of the <see cref="LogoutCommandHandler"/> class.</summary>
    /// <param name="refreshTokenService">The refresh-token revocation port.</param>
    public LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId is int userId)
        {
            await _refreshTokenService.RevokeAllForUserAsync(userId, cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}
