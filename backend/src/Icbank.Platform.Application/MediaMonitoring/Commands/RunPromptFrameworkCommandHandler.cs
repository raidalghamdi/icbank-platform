using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="RunPromptFrameworkCommand"/>.</summary>
public sealed class RunPromptFrameworkCommandHandler : IRequestHandler<RunPromptFrameworkCommand, Result<RunPromptFrameworkResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly IPromptExecutionEngine _executionEngine;

    /// <summary>Initializes a new instance of the <see cref="RunPromptFrameworkCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="executionEngine">The AI prompt execution port.</param>
    public RunPromptFrameworkCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService, IPromptExecutionEngine executionEngine)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _executionEngine = executionEngine;
    }

    /// <inheritdoc />
    public async Task<Result<RunPromptFrameworkResultDto>> Handle(RunPromptFrameworkCommand request, CancellationToken cancellationToken)
    {
        PromptFramework? framework = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.PromptFrameworks.Where(f => f.Id == request.FrameworkId), cancellationToken);
        if (framework is null)
        {
            return Result<RunPromptFrameworkResultDto>.Failure("القالب غير موجود");
        }

        var promptSent = PromptVariableSubstitutor.Substitute(framework.PromptText, request.Variables);
        var output = await _executionEngine.ExecuteAsync(promptSent, cancellationToken);

        framework.UsageCount += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "prompt_framework.run",
            "PromptFramework",
            request.FrameworkId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { framework.UsageCount },
            cancellationToken);

        return Result<RunPromptFrameworkResultDto>.Success(new RunPromptFrameworkResultDto(output, promptSent));
    }
}
