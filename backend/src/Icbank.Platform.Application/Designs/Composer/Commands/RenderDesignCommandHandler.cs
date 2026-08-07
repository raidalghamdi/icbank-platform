using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Handles <see cref="RenderDesignCommand"/>. Ports the Node source's template/background/logo lookup pipeline, delegating the actual rasterization to <see cref="IDesignComposer"/>.</summary>
public sealed class RenderDesignCommandHandler : IRequestHandler<RenderDesignCommand, Result<RenderDesignResultDto>>
{
    private const string ComposedImageContentType = "image/png";
    private const string StorageFolderPrefix = "designs/generated/";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IObjectStorageReader _storageReader;
    private readonly IObjectStorageWriter _storageWriter;
    private readonly IDesignComposer _composer;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="RenderDesignCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="storageReader">The object-storage read port.</param>
    /// <param name="storageWriter">The object-storage write port.</param>
    /// <param name="composer">The design composer port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public RenderDesignCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IObjectStorageReader storageReader,
        IObjectStorageWriter storageWriter,
        IDesignComposer composer,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _storageReader = storageReader;
        _storageWriter = storageWriter;
        _composer = composer;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<RenderDesignResultDto>> Handle(RenderDesignCommand request, CancellationToken cancellationToken)
    {
        DesignTemplate? template = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.DesignTemplates.Where(t => t.Id == request.TemplateId), cancellationToken);
        if (template is null)
        {
            return Result<RenderDesignResultDto>.Failure("القالب غير موجود");
        }

        var backgroundBytes = await LoadBackgroundAsync(request.BackgroundUrl, cancellationToken);
        List<BrandLogo> selectedLogos = await LoadSelectedLogosAsync(request.SelectedLogoIds, cancellationToken);

        var composeInput = new ComposeDesignInput(
            template,
            backgroundBytes,
            request.TitleText ?? string.Empty,
            request.BodyText ?? string.Empty,
            request.TitleFontSize,
            request.BodyFontSize,
            request.FontFamily,
            selectedLogos,
            request.Department);
        var composed = await _composer.ComposeAsync(composeInput, cancellationToken);
        var objectPath = await _storageWriter.SaveAsync(StorageFolderPrefix, composed, ComposedImageContentType, cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.render", "DesignTemplate", request.TemplateId.ToString(System.Globalization.CultureInfo.InvariantCulture), before: null, after: new { objectPath }, cancellationToken);

        return Result<RenderDesignResultDto>.Success(new RenderDesignResultDto(objectPath));
    }

    private async Task<byte[]> LoadBackgroundAsync(string? backgroundUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(backgroundUrl))
        {
            return Array.Empty<byte>();
        }

        StoredObject? stored = await _storageReader.OpenAsync(backgroundUrl, cancellationToken);
        return stored?.Content ?? Array.Empty<byte>();
    }

    private async Task<List<BrandLogo>> LoadSelectedLogosAsync(IReadOnlyList<int>? selectedLogoIds, CancellationToken cancellationToken)
    {
        if (selectedLogoIds is null || selectedLogoIds.Count == 0)
        {
            return new List<BrandLogo>();
        }

        List<BrandLogo> rows = await _queryExecutor.ToListAsync(_dbContext.BrandLogos.Where(l => selectedLogoIds.Contains(l.Id)), cancellationToken);
        var byId = rows.ToDictionary(l => l.Id);
        return selectedLogoIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
    }
}
