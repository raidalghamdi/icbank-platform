using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>Handles <see cref="UpdateAiYearActivationCommand"/>.</summary>
public sealed class UpdateAiYearActivationCommandHandler : IRequestHandler<UpdateAiYearActivationCommand, Result<AiYearActivationDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="UpdateAiYearActivationCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public UpdateAiYearActivationCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<AiYearActivationDto>> Handle(UpdateAiYearActivationCommand request, CancellationToken cancellationToken)
    {
        AiYearActivation? activation = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.AiYearActivations.Where(a => a.Id == request.ActivationId), cancellationToken);
        if (activation is null)
        {
            return Result<AiYearActivationDto>.Failure("التفعيل غير موجود");
        }

        ApplyScalarFields(activation, request);
        await ReplaceChannelsAsync(activation.Id, request.Channels, cancellationToken);
        await ReplaceMediaAsync(activation.Id, request.Media, cancellationToken);
        await ReplaceMetricsAsync(activation.Id, request.Metrics, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "ai_year_activation.update",
            "AiYearActivation",
            activation.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { activation.Title },
            cancellationToken);

        return await FetchDtoAsync(activation.Id, cancellationToken);
    }

    private static void ApplyScalarFields(AiYearActivation activation, UpdateAiYearActivationCommand request)
    {
        ApplyTextFields(activation, request);
        ApplyMetricFields(activation, request);
    }

    private static void ApplyTextFields(AiYearActivation activation, UpdateAiYearActivationCommand request)
    {
        if (request.Title is not null)
        {
            activation.Title = request.Title;
        }

        if (request.Month.HasValue)
        {
            activation.Month = request.Month.Value;
        }

        if (request.ActivationDate is not null)
        {
            activation.ActivationDate = request.ActivationDate;
        }

        if (request.Type is not null)
        {
            activation.Type = request.Type;
        }

        if (request.Description is not null)
        {
            activation.Description = request.Description;
        }

        if (request.Tags is not null)
        {
            activation.Tags = request.Tags.ToList();
        }
    }

    private static void ApplyMetricFields(AiYearActivation activation, UpdateAiYearActivationCommand request)
    {
        if (request.Status is not null && Enum.TryParse(request.Status, ignoreCase: true, out AiYearActivationStatus status))
        {
            activation.Status = status;
        }

        if (request.Reach.HasValue)
        {
            activation.Reach = request.Reach;
        }

        if (request.Engagement.HasValue)
        {
            activation.Engagement = request.Engagement;
        }

        if (request.Notes is not null)
        {
            activation.Notes = request.Notes;
        }
    }

    private async Task ReplaceChannelsAsync(int activationId, IReadOnlyList<string>? channels, CancellationToken cancellationToken)
    {
        if (channels is null)
        {
            return;
        }

        List<AiYearActivationChannel> existing = await _queryExecutor.ToListAsync(_dbContext.AiYearActivationChannels.Where(c => c.ActivationId == activationId), cancellationToken);
        foreach (AiYearActivationChannel? row in existing)
        {
            _dbContext.Remove(row);
        }

        foreach (var channel in channels)
        {
            _dbContext.Add(new AiYearActivationChannel { ActivationId = activationId, Channel = channel });
        }
    }

    private async Task ReplaceMediaAsync(int activationId, IReadOnlyList<CreateAiYearActivationMediaItem>? media, CancellationToken cancellationToken)
    {
        if (media is null)
        {
            return;
        }

        List<AiYearMedia> existing = await _queryExecutor.ToListAsync(_dbContext.AiYearMedia.Where(m => m.ActivationId == activationId), cancellationToken);
        foreach (AiYearMedia? row in existing)
        {
            _dbContext.Remove(row);
        }

        for (var i = 0; i < media.Count; i++)
        {
            CreateAiYearActivationMediaItem item = media[i];
            _dbContext.Add(new AiYearMedia
            {
                ActivationId = activationId,
                ObjectPath = item.ObjectPath,
                FileName = item.FileName,
                ContentType = item.ContentType,
                SortOrder = item.SortOrder ?? i,
            });
        }
    }

    private async Task ReplaceMetricsAsync(int activationId, IReadOnlyList<CreateAiYearActivationMetricItem>? metrics, CancellationToken cancellationToken)
    {
        if (metrics is null)
        {
            return;
        }

        List<AiYearMetric> existing = await _queryExecutor.ToListAsync(_dbContext.AiYearMetrics.Where(m => m.ActivationId == activationId), cancellationToken);
        foreach (AiYearMetric? row in existing)
        {
            _dbContext.Remove(row);
        }

        foreach (CreateAiYearActivationMetricItem metric in metrics)
        {
            _dbContext.Add(new AiYearMetric { ActivationId = activationId, MetricKey = metric.MetricKey, MetricValue = metric.MetricValue });
        }
    }

    private async Task<Result<AiYearActivationDto>> FetchDtoAsync(int activationId, CancellationToken cancellationToken)
    {
        AiYearActivation activation = (await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.AiYearActivations.Where(a => a.Id == activationId), cancellationToken))!;
        List<AiYearActivationChannel> channels = await _queryExecutor.ToListAsync(_dbContext.AiYearActivationChannels.Where(c => c.ActivationId == activationId), cancellationToken);
        List<AiYearMedia> media = await _queryExecutor.ToListAsync(
            _dbContext.AiYearMedia.Where(m => m.ActivationId == activationId).OrderBy(m => m.SortOrder), cancellationToken);
        List<AiYearMetric> metrics = await _queryExecutor.ToListAsync(_dbContext.AiYearMetrics.Where(m => m.ActivationId == activationId), cancellationToken);

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
