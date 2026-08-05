using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Handles <see cref="DeleteBrandFontCommand"/>. Storage cleanup not ported, matching the identical deferral documented on <see cref="DeleteBrandLogoCommandHandler"/>.</summary>
public sealed class DeleteBrandFontCommandHandler : IRequestHandler<DeleteBrandFontCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteBrandFontCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public DeleteBrandFontCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteBrandFontCommand request, CancellationToken cancellationToken)
    {
        BrandFont? entity = await _queryExecutor.SingleOrDefaultAsync(_dbContext.BrandFonts.Where(f => f.Id == request.FontId), cancellationToken);
        if (entity is null)
        {
            return Result<bool>.Failure("الخط غير موجود");
        }

        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.font.delete", "BrandFont", request.FontId.ToString(System.Globalization.CultureInfo.InvariantCulture), before: new { entity.FontName }, after: null, cancellationToken);

        return Result<bool>.Success(true);
    }
}
