using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="RunQuickAiToolCommand"/> using the 7 fixed <see cref="QuickAiToolPromptTemplates"/>.</summary>
public sealed class RunQuickAiToolCommandHandler : IRequestHandler<RunQuickAiToolCommand, Result<RunQuickAiToolResultDto>>
{
    private readonly IPromptExecutionEngine _executionEngine;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="RunQuickAiToolCommandHandler"/> class.</summary>
    /// <param name="executionEngine">The AI prompt execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public RunQuickAiToolCommandHandler(IPromptExecutionEngine executionEngine, IAuditLogService auditLogService)
    {
        _executionEngine = executionEngine;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<RunQuickAiToolResultDto>> Handle(RunQuickAiToolCommand request, CancellationToken cancellationToken)
    {
        var prompt = QuickAiToolPromptTemplates.Build(request.Tool, request.Input, request.Tone, request.Count);
        if (prompt is null)
        {
            return Result<RunQuickAiToolResultDto>.Failure("أداة غير معروفة");
        }

        var output = await _executionEngine.ExecuteAsync(prompt, cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "ai_quick.run",
            "AiQuickTool",
            request.Tool,
            before: null,
            after: new { request.Tool },
            cancellationToken);

        return Result<RunQuickAiToolResultDto>.Success(new RunQuickAiToolResultDto(output, request.Tool));
    }
}
