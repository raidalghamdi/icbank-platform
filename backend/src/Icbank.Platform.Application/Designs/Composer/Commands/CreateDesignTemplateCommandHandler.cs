using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Handles <see cref="CreateDesignTemplateCommand"/>.</summary>
public sealed class CreateDesignTemplateCommandHandler : IRequestHandler<CreateDesignTemplateCommand, Result<DesignTemplateDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="CreateDesignTemplateCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public CreateDesignTemplateCommandHandler(IApplicationDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<DesignTemplateDto>> Handle(CreateDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = new DesignTemplate
        {
            TemplateNameAr = request.TemplateNameAr,
            Category = request.Category,
            CanvasWidth = request.CanvasWidth,
            CanvasHeight = request.CanvasHeight,
            BackgroundPanelConfig = request.BackgroundPanelConfig,
            TextSlots = request.TextSlots ?? new List<TextSlot>(),
            LogoSlots = request.LogoSlots ?? new List<LogoSlot>(),
            PromptHint = request.PromptHint,
        };

        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.template.create", "DesignTemplate", entity.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), before: null, after: new { entity.TemplateNameAr }, cancellationToken);

        return Result<DesignTemplateDto>.Success(DesignTemplateMapper.ToDto(entity));
    }
}
