using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>
/// Handles <see cref="SetDefaultBrandFontCommand"/>. Closes DEFECT-LOG.md DATA-01
/// (BUSINESS-RULES.md §7.2): the Node source ran an unconditional
/// <c>UPDATE brand_fonts SET is_default=false</c> with no <c>WHERE</c> clause, not wrapped in a
/// transaction -- under concurrent requests this could leave zero fonts marked default, or fail
/// between the clear-all and set-new-default steps. This port instead: (1) confirms the target
/// font exists first (no wasted clear if the id is invalid), (2) clears only rows that are
/// currently <c>true</c> (a scoped, minimal write instead of touching every row), and
/// (3) persists both the clear and the set in a single <c>SaveChangesAsync</c> call, so EF Core's
/// implicit transaction guarantees both changes commit atomically or neither does.
/// </summary>
public sealed class SetDefaultBrandFontCommandHandler : IRequestHandler<SetDefaultBrandFontCommand, Result<BrandFontDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SetDefaultBrandFontCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public SetDefaultBrandFontCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<BrandFontDto>> Handle(SetDefaultBrandFontCommand request, CancellationToken cancellationToken)
    {
        BrandFont? target = await _queryExecutor.SingleOrDefaultAsync(_dbContext.BrandFonts.Where(f => f.Id == request.FontId), cancellationToken);
        if (target is null)
        {
            return Result<BrandFontDto>.Failure("الخط غير موجود");
        }

        List<BrandFont> currentDefaults = await _queryExecutor.ToListAsync(
            _dbContext.BrandFonts.Where(f => f.IsDefault && f.Id != request.FontId), cancellationToken);
        foreach (BrandFont font in currentDefaults)
        {
            font.IsDefault = false;
        }

        target.IsDefault = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.font.set_default", "BrandFont", request.FontId.ToString(System.Globalization.CultureInfo.InvariantCulture), before: null, after: new { target.FontName }, cancellationToken);

        return Result<BrandFontDto>.Success(BrandAssetMapper.ToDto(target));
    }
}
