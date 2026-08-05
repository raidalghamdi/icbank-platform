using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Handles <see cref="CreateBrandLogoCommand"/>.</summary>
public sealed class CreateBrandLogoCommandHandler : IRequestHandler<CreateBrandLogoCommand, Result<BrandLogoDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="CreateBrandLogoCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public CreateBrandLogoCommandHandler(IApplicationDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<BrandLogoDto>> Handle(CreateBrandLogoCommand request, CancellationToken cancellationToken)
    {
        var entity = new BrandLogo { LogoName = request.LogoName, FileUrl = request.FileUrl, Transparent = request.Transparent, DefaultWidth = request.DefaultWidth };
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.logo.create", "BrandLogo", entity.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), before: null, after: new { entity.LogoName }, cancellationToken);

        return Result<BrandLogoDto>.Success(BrandAssetMapper.ToDto(entity));
    }
}
