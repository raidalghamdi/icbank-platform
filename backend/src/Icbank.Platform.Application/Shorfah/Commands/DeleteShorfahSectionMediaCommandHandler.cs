using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="DeleteShorfahSectionMediaCommand"/>. Ports <c>shorfah.ts:615-619</c> with the AMBIGUOUS-API-4 gap closed.</summary>
public sealed class DeleteShorfahSectionMediaCommandHandler : IRequestHandler<DeleteShorfahSectionMediaCommand, Result<bool>>
{
    /// <summary>The sentinel error returned when the caller lacks any qualifying tier on the owning section.</summary>
    public const string ForbiddenError = "غير مصرح";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IShorfahSectionAccessService _accessService;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteShorfahSectionMediaCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="accessService">The per-section permission-tier port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeleteShorfahSectionMediaCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IShorfahSectionAccessService accessService, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _accessService = accessService;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteShorfahSectionMediaCommand request, CancellationToken cancellationToken)
    {
        ShorfahSectionMedia? media = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSectionMedia.Where(m => m.Id == request.MediaId), cancellationToken);
        if (media is null)
        {
            return Result<bool>.Failure("الوسائط غير موجودة");
        }

        var allowed = await IsAllowedAsync(request.ActorUserId, media.SectionId, cancellationToken);
        if (!allowed)
        {
            return Result<bool>.Failure(ForbiddenError);
        }

        _dbContext.Remove(media);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_media.delete",
            "ShorfahSectionMedia",
            ShorfahMappers.IdString(request.MediaId),
            before: new { media.SectionId, media.MediaUrl },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }

    private async Task<bool> IsAllowedAsync(int actorUserId, int sectionId, CancellationToken cancellationToken)
    {
        if (await _accessService.IsAdminAsync(actorUserId, cancellationToken))
        {
            return true;
        }

        return await _accessService.CanAccessSectionAsync(actorUserId, sectionId, ShorfahSectionAccessTier.Contribute, cancellationToken)
            || await _accessService.CanAccessSectionAsync(actorUserId, sectionId, ShorfahSectionAccessTier.Review, cancellationToken)
            || await _accessService.CanAccessSectionAsync(actorUserId, sectionId, ShorfahSectionAccessTier.Approve, cancellationToken);
    }
}
