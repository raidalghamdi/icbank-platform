using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="CreatePromptFrameworkCommand"/>.</summary>
public sealed class CreatePromptFrameworkCommandHandler : IRequestHandler<CreatePromptFrameworkCommand, Result<PromptFrameworkDto>>
{
    private const string DefaultCategory = "ContentCreation";
    private const string DefaultKind = "Framework";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="CreatePromptFrameworkCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public CreatePromptFrameworkCommandHandler(IApplicationDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<PromptFrameworkDto>> Handle(CreatePromptFrameworkCommand request, CancellationToken cancellationToken)
    {
        var framework = new PromptFramework
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            DescriptionAr = request.DescriptionAr,
            Category = Enum.TryParse(request.Category, ignoreCase: true, out PromptFrameworkCategory category) ? category : Enum.Parse<PromptFrameworkCategory>(DefaultCategory),
            Kind = Enum.TryParse(request.Kind, ignoreCase: true, out PromptFrameworkKind kind) ? kind : Enum.Parse<PromptFrameworkKind>(DefaultKind),
            PromptText = request.PromptText,
            Variables = (request.Variables ?? Array.Empty<PromptVariableItem>())
                .Select(v => new PromptVariable { Key = v.Key, Label = v.Label, Type = v.Type, Required = v.Required }).ToList(),
            ExampleInput = request.ExampleInput,
            ExampleOutput = request.ExampleOutput,
            Tags = request.Tags?.ToList() ?? new List<string>(),
            RecommendedModel = request.RecommendedModel,
            CreatedByUserId = request.ActorUserId,
            Status = PromptFrameworkStatus.Active,
        };

        _dbContext.Add(framework);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "prompt_framework.create",
            "PromptFramework",
            framework.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { framework.NameAr },
            cancellationToken);

        return Result<PromptFrameworkDto>.Success(PromptFrameworkMapper.ToDto(framework));
    }
}
