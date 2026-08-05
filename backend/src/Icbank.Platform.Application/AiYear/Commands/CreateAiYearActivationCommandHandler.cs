using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>
/// Handles <see cref="CreateAiYearActivationCommand"/>. Media path validation happens in the
/// FluentValidation pipeline before this handler runs at all (ports the Node source's
/// "validated before any DB write" ordering exactly), and every entity is added within a single
/// <see cref="IApplicationDbContext.SaveChangesAsync"/> call (one implicit transaction).
/// </summary>
public sealed class CreateAiYearActivationCommandHandler : IRequestHandler<CreateAiYearActivationCommand, Result<AiYearActivationDto>>
{
    private const string DefaultStatus = "Published";
    private const int DefaultYear = 2026;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="CreateAiYearActivationCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public CreateAiYearActivationCommandHandler(IApplicationDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<AiYearActivationDto>> Handle(CreateAiYearActivationCommand request, CancellationToken cancellationToken)
    {
        AiYearActivation activation = BuildActivation(request);
        _dbContext.Add(activation);
        AddChildEntities(activation, request);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "ai_year_activation.create",
            "AiYearActivation",
            activation.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { activation.Title },
            cancellationToken);

        return Result<AiYearActivationDto>.Success(ToDto(activation, request));
    }

    private static AiYearActivation BuildActivation(CreateAiYearActivationCommand request)
    {
        AiYearActivationStatus status = Enum.TryParse(request.Status, ignoreCase: true, out AiYearActivationStatus parsedStatus)
            ? parsedStatus
            : Enum.Parse<AiYearActivationStatus>(DefaultStatus);

        return new AiYearActivation
        {
            Title = request.Title,
            Month = request.Month,
            Year = request.Year ?? DefaultYear,
            ActivationDate = request.ActivationDate,
            Type = request.Type,
            Description = request.Description,
            Tags = request.Tags?.ToList() ?? new List<string>(),
            Status = status,
            Reach = request.Reach,
            Engagement = request.Engagement,
            Notes = request.Notes,
            CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    private static AiYearActivationDto ToDto(AiYearActivation activation, CreateAiYearActivationCommand request)
    {
        IReadOnlyList<CreateAiYearActivationMediaItem> mediaItems = request.Media ?? Array.Empty<CreateAiYearActivationMediaItem>();
        IReadOnlyList<CreateAiYearActivationMetricItem> metricItems = request.Metrics ?? Array.Empty<CreateAiYearActivationMetricItem>();

        return new AiYearActivationDto(
            activation.Id,
            activation.Title,
            activation.Month,
            activation.Year,
            activation.ActivationDate,
            activation.Type,
            request.Channels,
            activation.Description,
            activation.Tags,
            activation.Status.ToString(),
            activation.Reach,
            activation.Engagement,
            activation.Notes,
            mediaItems.Select((m, i) => new AiYearMediaDto(0, m.ObjectPath, m.FileName, m.ContentType, m.SortOrder ?? i)).ToList(),
            metricItems.Select(m => new AiYearMetricDto(0, m.MetricKey, m.MetricValue)).ToList());
    }

    private void AddChildEntities(AiYearActivation activation, CreateAiYearActivationCommand request)
    {
        foreach (var channel in request.Channels)
        {
            _dbContext.Add(new AiYearActivationChannel { Activation = activation, Channel = channel });
        }

        IReadOnlyList<CreateAiYearActivationMediaItem> mediaItems = request.Media ?? Array.Empty<CreateAiYearActivationMediaItem>();
        for (var i = 0; i < mediaItems.Count; i++)
        {
            CreateAiYearActivationMediaItem item = mediaItems[i];
            _dbContext.Add(new AiYearMedia
            {
                Activation = activation,
                ObjectPath = item.ObjectPath,
                FileName = item.FileName,
                ContentType = item.ContentType,
                SortOrder = item.SortOrder ?? i,
            });
        }

        foreach (CreateAiYearActivationMetricItem metric in request.Metrics ?? Array.Empty<CreateAiYearActivationMetricItem>())
        {
            _dbContext.Add(new AiYearMetric { Activation = activation, MetricKey = metric.MetricKey, MetricValue = metric.MetricValue });
        }
    }
}
