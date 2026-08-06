using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="IssueShorfahIssueDownloadTokenCommand"/>. Deliberately does not re-check
/// <c>IResourceAuthorizationService</c> here: the controller action this handler is wired to
/// already runs behind <c>[Authorize(Policy = "shorfah:view")]</c> and the same
/// <c>EnsureIssueExistsAsync</c> guard every other issue-id endpoint on
/// <c>ShorfahIssuesController</c> uses, so by the time MediatR reaches this handler the caller has
/// already cleared both the policy and the resource-existence check. Minting is intentionally
/// cheap and side-effect-free beyond writing the token row -- the security-relevant check is
/// re-run independently at redemption time by <c>ShorfahIssuesController</c>'s token-redemption
/// actions, which call <c>IResourceAuthorizationService.AuthorizeShorfahIssueResourceAsync</c>
/// again before serving any content; minting a token is never treated as proof of authorization
/// by itself.
/// </summary>
public sealed class IssueShorfahIssueDownloadTokenCommandHandler : IRequestHandler<IssueShorfahIssueDownloadTokenCommand, Result<string>>
{
    private readonly IDownloadTokenService _downloadTokenService;

    /// <summary>Initializes a new instance of the <see cref="IssueShorfahIssueDownloadTokenCommandHandler"/> class.</summary>
    /// <param name="downloadTokenService">The single-use download-token port.</param>
    public IssueShorfahIssueDownloadTokenCommandHandler(IDownloadTokenService downloadTokenService)
    {
        _downloadTokenService = downloadTokenService;
    }

    /// <inheritdoc />
    public async Task<Result<string>> Handle(IssueShorfahIssueDownloadTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _downloadTokenService.IssueAsync(
            DownloadResourceType.ShorfahIssuePdf, request.IssueId, request.ActorUserId, cancellationToken);
        return Result<string>.Success(token);
    }
}
