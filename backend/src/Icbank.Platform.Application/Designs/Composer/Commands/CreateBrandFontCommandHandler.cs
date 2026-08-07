using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Handles <see cref="CreateBrandFontCommand"/>.</summary>
public sealed class CreateBrandFontCommandHandler : IRequestHandler<CreateBrandFontCommand, Result<BrandFontDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="CreateBrandFontCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public CreateBrandFontCommandHandler(IApplicationDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<BrandFontDto>> Handle(CreateBrandFontCommand request, CancellationToken cancellationToken)
    {
        var entity = new BrandFont { FontName = request.FontName, FontFileUrl = request.FontFileUrl, IsDefault = false };
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.font.create", "BrandFont", entity.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), before: null, after: new { entity.FontName }, cancellationToken);

        return Result<BrandFontDto>.Success(BrandAssetMapper.ToDto(entity));
    }
}
