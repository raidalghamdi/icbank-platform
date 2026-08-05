using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="UpdatePromptFrameworkCommand"/>.</summary>
public sealed class UpdatePromptFrameworkCommandHandler : IRequestHandler<UpdatePromptFrameworkCommand, Result<PromptFrameworkDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="UpdatePromptFrameworkCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public UpdatePromptFrameworkCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<PromptFrameworkDto>> Handle(UpdatePromptFrameworkCommand request, CancellationToken cancellationToken)
    {
        PromptFramework? framework = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.PromptFrameworks.Where(f => f.Id == request.FrameworkId), cancellationToken);
        if (framework is null)
        {
            return Result<PromptFrameworkDto>.Failure("القالب غير موجود");
        }

        ApplyChanges(framework, request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "prompt_framework.update",
            "PromptFramework",
            request.FrameworkId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { framework.NameAr },
            cancellationToken);

        return Result<PromptFrameworkDto>.Success(PromptFrameworkMapper.ToDto(framework));
    }

    private static void ApplyChanges(PromptFramework framework, UpdatePromptFrameworkCommand request)
    {
        framework.NameAr = request.NameAr ?? framework.NameAr;
        framework.NameEn = request.NameEn ?? framework.NameEn;
        framework.DescriptionAr = request.DescriptionAr ?? framework.DescriptionAr;
        framework.PromptText = request.PromptText ?? framework.PromptText;
        framework.ExampleInput = request.ExampleInput ?? framework.ExampleInput;
        framework.ExampleOutput = request.ExampleOutput ?? framework.ExampleOutput;
        framework.IsApproved = request.IsApproved ?? framework.IsApproved;

        if (request.Variables is not null)
        {
            framework.Variables = request.Variables.Select(v => new PromptVariable { Key = v.Key, Label = v.Label, Type = v.Type, Required = v.Required }).ToList();
        }

        if (request.Tags is not null)
        {
            framework.Tags = request.Tags.ToList();
        }
    }
}
