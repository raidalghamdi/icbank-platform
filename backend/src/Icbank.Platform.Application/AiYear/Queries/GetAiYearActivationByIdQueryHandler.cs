using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Handles <see cref="GetAiYearActivationByIdQuery"/>.</summary>
public sealed class GetAiYearActivationByIdQueryHandler : IRequestHandler<GetAiYearActivationByIdQuery, Result<AiYearActivationDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetAiYearActivationByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetAiYearActivationByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<AiYearActivationDto>> Handle(GetAiYearActivationByIdQuery request, CancellationToken cancellationToken)
    {
        AiYearActivation? activation = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.AiYearActivations.Where(a => a.Id == request.ActivationId), cancellationToken);
        if (activation is null)
        {
            return Result<AiYearActivationDto>.Failure("التفعيل غير موجود");
        }

        List<AiYearMedia> media = await _queryExecutor.ToListAsync(
            _dbContext.AiYearMedia.Where(m => m.ActivationId == activation.Id).OrderBy(m => m.SortOrder), cancellationToken);
        List<AiYearMetric> metrics = await _queryExecutor.ToListAsync(_dbContext.AiYearMetrics.Where(m => m.ActivationId == activation.Id), cancellationToken);
        List<AiYearActivationChannel> channels = await _queryExecutor.ToListAsync(_dbContext.AiYearActivationChannels.Where(c => c.ActivationId == activation.Id), cancellationToken);

        var dto = new AiYearActivationDto(
            activation.Id,
            activation.Title,
            activation.Month,
            activation.Year,
            activation.ActivationDate,
            activation.Type,
            channels.Select(c => c.Channel).ToList(),
            activation.Description,
            activation.Tags,
            activation.Status.ToString(),
            activation.Reach,
            activation.Engagement,
            activation.Notes,
            media.Select(m => new AiYearMediaDto(m.Id, m.ObjectPath, m.FileName, m.ContentType, m.SortOrder)).ToList(),
            metrics.Select(m => new AiYearMetricDto(m.Id, m.MetricKey, m.MetricValue)).ToList());

        return Result<AiYearActivationDto>.Success(dto);
    }
}
