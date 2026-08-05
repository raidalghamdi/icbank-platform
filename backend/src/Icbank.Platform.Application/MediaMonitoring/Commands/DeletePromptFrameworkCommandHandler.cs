using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="DeletePromptFrameworkCommand"/>. Hard delete, matching the Node source (a lookup-style reference row).</summary>
public sealed class DeletePromptFrameworkCommandHandler : IRequestHandler<DeletePromptFrameworkCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeletePromptFrameworkCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeletePromptFrameworkCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeletePromptFrameworkCommand request, CancellationToken cancellationToken)
    {
        PromptFramework? framework = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.PromptFrameworks.Where(f => f.Id == request.FrameworkId), cancellationToken);
        if (framework is null)
        {
            return Result<bool>.Failure("القالب غير موجود");
        }

        _dbContext.Remove(framework);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "prompt_framework.delete",
            "PromptFramework",
            request.FrameworkId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { framework.NameAr },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
