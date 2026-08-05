using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>
/// Handles <see cref="ListAiYearActivationsQuery"/>. Closes DEFECT-LOG.md DATA-06: the Node
/// source issued one query pair per activation row; this handler batches all
/// media/metrics/channels for the current page into single <c>IN</c>-style queries.
/// </summary>
public sealed class ListAiYearActivationsQueryHandler : IRequestHandler<ListAiYearActivationsQuery, Result<PagedResult<AiYearActivationDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListAiYearActivationsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListAiYearActivationsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AiYearActivationDto>>> Handle(ListAiYearActivationsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<AiYearActivation> query = _dbContext.AiYearActivations;
        query = ApplyFilters(query, request);
        query = query.OrderByDescending(a => a.Month).ThenByDescending(a => a.CreatedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(a => a.Id), cancellationToken);
        var total = allIds.Count;
        List<AiYearActivation> pageActivations = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);
        var pageIds = pageActivations.Select(a => a.Id).ToList();

        List<AiYearMedia> media = await _queryExecutor.ToListAsync(_dbContext.AiYearMedia.Where(m => pageIds.Contains(m.ActivationId)), cancellationToken);
        List<AiYearMetric> metrics = await _queryExecutor.ToListAsync(_dbContext.AiYearMetrics.Where(m => pageIds.Contains(m.ActivationId)), cancellationToken);
        List<AiYearActivationChannel> channels = await _queryExecutor.ToListAsync(_dbContext.AiYearActivationChannels.Where(c => pageIds.Contains(c.ActivationId)), cancellationToken);

        var items = pageActivations.Select(a => ToDto(
            a,
            channels.Where(c => c.ActivationId == a.Id).Select(c => c.Channel).ToList(),
            media.Where(m => m.ActivationId == a.Id).OrderBy(m => m.SortOrder).ToList(),
            metrics.Where(m => m.ActivationId == a.Id).ToList())).ToList();

        return Result<PagedResult<AiYearActivationDto>>.Success(new PagedResult<AiYearActivationDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static IQueryable<AiYearActivation> ApplyFilters(IQueryable<AiYearActivation> query, ListAiYearActivationsQuery request)
    {
        if (request.Month.HasValue)
        {
            query = query.Where(a => a.Month == request.Month.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(a => a.Type == request.Type);
        }

        if (!string.IsNullOrWhiteSpace(request.Channel))
        {
            var channel = request.Channel;
            query = query.Where(a => a.Channels.Any(c => c.Channel == channel));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var pattern = request.SearchText.Trim();
            query = query.Where(a => a.Title.Contains(pattern) || (a.Description != null && a.Description.Contains(pattern)));
        }

        return query;
    }

    private static AiYearActivationDto ToDto(
        AiYearActivation activation, IReadOnlyList<string> channels, IReadOnlyList<AiYearMedia> media, IReadOnlyList<AiYearMetric> metrics) => new(
        activation.Id,
        activation.Title,
        activation.Month,
        activation.Year,
        activation.ActivationDate,
        activation.Type,
        channels,
        activation.Description,
        activation.Tags,
        activation.Status.ToString(),
        activation.Reach,
        activation.Engagement,
        activation.Notes,
        media.Select(m => new AiYearMediaDto(m.Id, m.ObjectPath, m.FileName, m.ContentType, m.SortOrder)).ToList(),
        metrics.Select(m => new AiYearMetricDto(m.Id, m.MetricKey, m.MetricValue)).ToList());
}
