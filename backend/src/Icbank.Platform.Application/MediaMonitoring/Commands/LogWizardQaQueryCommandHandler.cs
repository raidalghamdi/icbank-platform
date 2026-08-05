using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="LogWizardQaQueryCommand"/>. Persists one <see cref="ReportsQaQuery"/> audit row per call, matching the Node source's <c>POST /qa-queries</c> behaviour.</summary>
public sealed class LogWizardQaQueryCommandHandler : IRequestHandler<LogWizardQaQueryCommand, Result<int>>
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="LogWizardQaQueryCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    public LogWizardQaQueryCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(LogWizardQaQueryCommand request, CancellationToken cancellationToken)
    {
        var entry = new ReportsQaQuery
        {
            UserId = request.ActorUserId,
            QueryType = QaQueryType.Wizard,
            WizardAnswers = new WizardAnswers
            {
                Period = request.Period,
                Audience = request.Audience,
                Sources = request.Sources?.ToList() ?? new List<string>(),
                FocusTopics = request.FocusTopics,
                Language = request.Language,
                Recipients = request.Recipients,
                Mode = request.Mode,
            },
            ResultSummary = "تم تسجيل إجابات المعالج",
        };

        _dbContext.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(entry.Id);
    }
}
