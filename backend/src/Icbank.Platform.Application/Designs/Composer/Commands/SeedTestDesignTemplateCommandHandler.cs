using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>
/// Handles <see cref="SeedTestDesignTemplateCommand"/>. Ports the legacy pixel-based test
/// template exactly as the Node source defines it (designs.ts:54-77), kept for back-compat with
/// an old UI that expects pixel, not percentage, positioning.
/// </summary>
public sealed class SeedTestDesignTemplateCommandHandler : IRequestHandler<SeedTestDesignTemplateCommand, Result<SeedTestDesignTemplateResultDto>>
{
    private const int LegacyCanvasWidth = 1920;
    private const int LegacyCanvasHeight = 1080;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SeedTestDesignTemplateCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public SeedTestDesignTemplateCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<SeedTestDesignTemplateResultDto>> Handle(SeedTestDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        DesignTemplate? existing = await _queryExecutor.SingleOrDefaultAsync(_dbContext.DesignTemplates.Take(1), cancellationToken);
        if (existing is not null)
        {
            return Result<SeedTestDesignTemplateResultDto>.Success(new SeedTestDesignTemplateResultDto(true, DesignTemplateMapper.ToDto(existing)));
        }

        DesignTemplate entity = BuildLegacyTemplate();
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.RecordAsync(request.ActorUserId, "design.template.seed_test", "DesignTemplate", entity.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), before: null, after: null, cancellationToken);

        return Result<SeedTestDesignTemplateResultDto>.Success(new SeedTestDesignTemplateResultDto(false, DesignTemplateMapper.ToDto(entity)));
    }

    private static DesignTemplate BuildLegacyTemplate() => new()
    {
        TemplateNameAr = "قالب تجريبي — إعلان عام",
        Category = "general",
        CanvasWidth = LegacyCanvasWidth,
        CanvasHeight = LegacyCanvasHeight,
        BackgroundPanelConfig = new BackgroundPanelConfig { X = 0, Y = 700, Width = 1920, Height = 380, Color = "#1a3a6b", Opacity = 0.88 },
        TextSlots = new List<TextSlot>
        {
            new() { Key = "title", LabelAr = "العنوان الرئيسي", X = 80, Y = 740, Width = 1760, Height = 120, DefaultFontSize = 72, MaxWords = 10, Alignment = "right", Color = "#ffffff" },
            new() { Key = "body", LabelAr = "النص التفصيلي", X = 80, Y = 880, Width = 1760, Height = 160, DefaultFontSize = 40, MaxWords = 30, Alignment = "right", Color = "#d0dcff" },
        },
        LogoSlots = new List<LogoSlot> { new() { Key = "logo_main", X = 80, Y = 30, Width = 210, Height = 130 } },
    };
}
