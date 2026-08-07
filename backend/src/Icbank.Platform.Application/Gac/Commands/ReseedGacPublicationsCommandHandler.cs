using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Handles <see cref="ReseedGacPublicationsCommand"/>.</summary>
public sealed class ReseedGacPublicationsCommandHandler : IRequestHandler<ReseedGacPublicationsCommand, Result<ReseedGacPublicationsResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="ReseedGacPublicationsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public ReseedGacPublicationsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ReseedGacPublicationsResult>> Handle(ReseedGacPublicationsCommand request, CancellationToken cancellationToken)
    {
        var inserted = 0;
        var skipped = new List<string>();

        foreach (ReseedGacPublicationItem item in request.Publications)
        {
            var exists = await _queryExecutor.AnyAsync(_dbContext.GacPublications.Where(p => p.TitleAr == item.TitleAr), cancellationToken);
            if (exists)
            {
                skipped.Add(item.TitleAr);
                continue;
            }

            _dbContext.Add(ToEntity(item, request.ActorUserId));
            inserted++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "gac_publication.reseed",
            "GacPublication",
            "bulk",
            before: null,
            after: new { inserted, skippedCount = skipped.Count },
            cancellationToken);

        return Result<ReseedGacPublicationsResult>.Success(new ReseedGacPublicationsResult(inserted, skipped));
    }

    private static GacPublication ToEntity(ReseedGacPublicationItem item, int actorUserId) => new()
    {
        TitleAr = item.TitleAr,
        TitleEn = item.TitleEn,
        Category = Enum.Parse<GacPublicationCategory>(item.Category, ignoreCase: true),
        Language = Enum.Parse<GacPublicationLanguage>(item.Language, ignoreCase: true),
        DescriptionAr = item.DescriptionAr,
        DescriptionEn = item.DescriptionEn,
        FileUrl = item.FileUrl,
        FileSizeBytes = item.FileSizeBytes,
        PageCount = item.PageCount,
        Tags = item.Tags?.ToList() ?? new List<string>(),
        SourceDomain = Enum.Parse<GacPublicationSourceDomain>(item.SourceDomain, ignoreCase: true),
        Status = GacPublicationStatus.Published,
        DisplayOrder = item.DisplayOrder,
        CreatedBy = actorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}
