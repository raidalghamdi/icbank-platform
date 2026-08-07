using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>
/// Handles <see cref="ReseedDesignTemplateSetCommand"/>. Ports BUSINESS-RULES.md §7.1's
/// idempotent-by-name, always-overwrite rule verbatim: look up each seed definition by
/// <c>TemplateNameAr</c>; if found, overwrite its layout fields so code fixes to the seed data
/// propagate on re-run; if not found, insert fresh.
/// </summary>
public sealed class ReseedDesignTemplateSetCommandHandler : IRequestHandler<ReseedDesignTemplateSetCommand, Result<ReseedDesignTemplateSetResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDesignTemplateSeedCatalog _seedCatalog;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="ReseedDesignTemplateSetCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="seedCatalog">The template seed-data port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public ReseedDesignTemplateSetCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDesignTemplateSeedCatalog seedCatalog, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _seedCatalog = seedCatalog;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ReseedDesignTemplateSetResultDto>> Handle(ReseedDesignTemplateSetCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<DesignTemplateSeedDefinition> definitions = _seedCatalog.GetSeedSet(request.SeedSet);
        var results = new List<DesignTemplateDto>();
        var notes = new List<string>();

        foreach (DesignTemplateSeedDefinition definition in definitions)
        {
            DesignTemplate? existing = await _queryExecutor.SingleOrDefaultAsync(
                _dbContext.DesignTemplates.Where(t => t.TemplateNameAr == definition.TemplateNameAr), cancellationToken);

            if (existing is not null)
            {
                ApplyOverwrite(existing, definition);
                notes.Add($"updated: {definition.TemplateNameAr}");
                results.Add(DesignTemplateMapper.ToDto(existing));
            }
            else
            {
                DesignTemplate created = BuildEntity(definition);
                _dbContext.Add(created);
                results.Add(DesignTemplateMapper.ToDto(created));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.template.reseed", "DesignTemplate", request.SeedSet.ToString(), before: null, after: new { Count = results.Count }, cancellationToken);

        return Result<ReseedDesignTemplateSetResultDto>.Success(new ReseedDesignTemplateSetResultDto(results.Count, results, notes));
    }

    private static void ApplyOverwrite(DesignTemplate existing, DesignTemplateSeedDefinition definition)
    {
        existing.Category = definition.Category;
        existing.CanvasWidth = definition.CanvasWidth;
        existing.CanvasHeight = definition.CanvasHeight;
        existing.BackgroundPanelConfig = definition.BackgroundPanelConfig;
        existing.TextSlots = definition.TextSlots;
        existing.LogoSlots = definition.LogoSlots;
        existing.PromptHint = definition.PromptHint;
        existing.Extras = definition.Extras;
    }

    private static DesignTemplate BuildEntity(DesignTemplateSeedDefinition definition) => new()
    {
        TemplateNameAr = definition.TemplateNameAr,
        Category = definition.Category,
        CanvasWidth = definition.CanvasWidth,
        CanvasHeight = definition.CanvasHeight,
        BackgroundPanelConfig = definition.BackgroundPanelConfig,
        TextSlots = definition.TextSlots,
        LogoSlots = definition.LogoSlots,
        PromptHint = definition.PromptHint,
        Extras = definition.Extras,
    };
}
