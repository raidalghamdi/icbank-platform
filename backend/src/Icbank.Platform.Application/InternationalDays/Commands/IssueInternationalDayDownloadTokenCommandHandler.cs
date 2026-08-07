using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.InternationalDays;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>
/// Handles <see cref="IssueInternationalDayDownloadTokenCommand"/>. International days have no
/// dedicated <c>IResourceAuthorizationService</c> method (there is no owner/tenant concept beyond
/// existence, same as Shorfah issues, but this resource family predates that port and its
/// existing export handler -- <see cref="Queries.ExportInternationalDayHtmlQueryHandler"/> --
/// already does its own inline existence check rather than going through that service). This
/// handler repeats that same inline existence check before minting so a token can never be minted
/// for a day id that does not exist, keeping the token's guarantee ("was mintable a moment ago by
/// an authorized caller") meaningful. The token-redemption endpoint independently re-runs the
/// identical existence check (via the same underlying query) before serving any content.
/// </summary>
public sealed class IssueInternationalDayDownloadTokenCommandHandler : IRequestHandler<IssueInternationalDayDownloadTokenCommand, Result<string>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDownloadTokenService _downloadTokenService;

    /// <summary>Initializes a new instance of the <see cref="IssueInternationalDayDownloadTokenCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="downloadTokenService">The single-use download-token port.</param>
    public IssueInternationalDayDownloadTokenCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDownloadTokenService downloadTokenService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _downloadTokenService = downloadTokenService;
    }

    /// <inheritdoc />
    public async Task<Result<string>> Handle(IssueInternationalDayDownloadTokenCommand request, CancellationToken cancellationToken)
    {
        InternationalDay? day = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.InternationalDays.Where(d => d.Id == request.DayId), cancellationToken);
        if (day is null)
        {
            return Result<string>.Failure("غير موجود");
        }

        var token = await _downloadTokenService.IssueAsync(
            DownloadResourceType.InternationalDayExport, request.DayId, request.ActorUserId, cancellationToken);
        return Result<string>.Success(token);
    }
}
