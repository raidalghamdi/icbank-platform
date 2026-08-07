using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>
/// Handles <see cref="SeedGacLogosCommand"/>. Ports the Node source's base64-decode-then-upload
/// pipeline, idempotent on <c>logoName</c> exactly as designs.ts:193-220 defines it.
/// </summary>
public sealed class SeedGacLogosCommandHandler : IRequestHandler<SeedGacLogosCommand, Result<SeedGacLogosResultDto>>
{
    private const string LogoPngContentType = "image/png";
    private const string StorageFolderPrefix = "designs/logos/gac/";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IGacLogoSeedCatalog _seedCatalog;
    private readonly IObjectStorageWriter _storageWriter;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SeedGacLogosCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="seedCatalog">The GAC logo seed-data port.</param>
    /// <param name="storageWriter">The object-storage write port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public SeedGacLogosCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IGacLogoSeedCatalog seedCatalog, IObjectStorageWriter storageWriter, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _seedCatalog = seedCatalog;
        _storageWriter = storageWriter;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<SeedGacLogosResultDto>> Handle(SeedGacLogosCommand request, CancellationToken cancellationToken)
    {
        var inserted = new List<BrandLogoDto>();
        var skipped = new List<string>();

        foreach (GacLogoSeedDefinition asset in _seedCatalog.GetLogos())
        {
            var exists = await _queryExecutor.AnyAsync(_dbContext.BrandLogos.Where(l => l.LogoName == asset.LogoName), cancellationToken);
            if (exists)
            {
                skipped.Add($"exists: {asset.LogoName}");
                continue;
            }

            var buffer = Convert.FromBase64String(asset.ContentBase64);
            var objectPath = await _storageWriter.SaveAsync(StorageFolderPrefix, buffer, LogoPngContentType, cancellationToken);
            var entity = new BrandLogo { LogoName = asset.LogoName, FileUrl = objectPath, Transparent = asset.Transparent, DefaultWidth = asset.DefaultWidth };
            _dbContext.Add(entity);
            inserted.Add(BrandAssetMapper.ToDto(entity));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.logo.seed_gac", "BrandLogo", "seed-gac", before: null, after: new { Count = inserted.Count }, cancellationToken);

        return Result<SeedGacLogosResultDto>.Success(new SeedGacLogosResultDto(inserted.Count, skipped, inserted));
    }
}
