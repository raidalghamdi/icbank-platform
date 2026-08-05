using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="ApproveGeneratedOutputCommand"/>.</summary>
public sealed class ApproveGeneratedOutputCommandHandler : IRequestHandler<ApproveGeneratedOutputCommand, Result<GeneratedOutputDto>>
{
    private const int ArchiveTitleMaxLength = 80;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="ApproveGeneratedOutputCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public ApproveGeneratedOutputCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<GeneratedOutputDto>> Handle(ApproveGeneratedOutputCommand request, CancellationToken cancellationToken)
    {
        GeneratedOutput? output = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.GeneratedOutputs.Where(o => o.Id == request.OutputId), cancellationToken);
        if (output is null)
        {
            return Result<GeneratedOutputDto>.Failure("output غير موجود");
        }

        output.Selected = true;

        var modelLabel = ResolveModelLabel(output.ModelName);
        var title = output.Topic.Length > ArchiveTitleMaxLength ? output.Topic[..ArchiveTitleMaxLength] : output.Topic;
        var archiveEntry = new ArchiveEntry
        {
            Title = title,
            BodyText = output.OutputText,
            SourceFile = $"معتمد · {modelLabel}",
            CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        _dbContext.Add(archiveEntry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "week_start_output.approve",
            "GeneratedOutput",
            output.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { ArchivedEntryId = archiveEntry.Id },
            cancellationToken);

        return Result<GeneratedOutputDto>.Success(ToDto(output));
    }

    private static string ResolveModelLabel(string modelName) => modelName switch
    {
        "claude" => "Claude Sonnet",
        "openai" => "GPT-4o",
        _ => "Gemini 2.5 Pro",
    };

    private static GeneratedOutputDto ToDto(GeneratedOutput output) =>
        new(output.Id, output.Topic, output.ModelName, output.OutputText, output.Selected, output.CreatedAt);
}
