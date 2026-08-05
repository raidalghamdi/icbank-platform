using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>
/// Handles <see cref="DeleteBrandLogoCommand"/>. The Node source best-effort deletes the
/// underlying storage object first (swallowing any error); this port does the same via
/// <see cref="Icbank.Platform.Application.Storage.IObjectStorageWriter"/>'s absence of a delete
/// method being a deliberate omission -- storage cleanup is not ported (no storage-delete port
/// exists yet, matching the identical WAVE1-PORT-NOTES.md deferral for weekend-place images).
/// </summary>
public sealed class DeleteBrandLogoCommandHandler : IRequestHandler<DeleteBrandLogoCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteBrandLogoCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public DeleteBrandLogoCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteBrandLogoCommand request, CancellationToken cancellationToken)
    {
        BrandLogo? entity = await _queryExecutor.SingleOrDefaultAsync(_dbContext.BrandLogos.Where(l => l.Id == request.LogoId), cancellationToken);
        if (entity is null)
        {
            return Result<bool>.Failure("الشعار غير موجود");
        }

        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.logo.delete", "BrandLogo", request.LogoId.ToString(System.Globalization.CultureInfo.InvariantCulture), before: new { entity.LogoName }, after: null, cancellationToken);

        return Result<bool>.Success(true);
    }
}
