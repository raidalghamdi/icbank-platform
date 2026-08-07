using System.Text.Json;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>
/// Handles <see cref="GetWk2DataQuery"/>. Ports BUSINESS-RULES.md §2.4's curation rule verbatim:
/// curated <c>weekend_places</c> rows always win over the draft's AI-generated places when both
/// exist; every other section (deals/podcasts/matches/movies/summary) comes exclusively from the
/// latest published draft's content JSON.
/// </summary>
public sealed class GetWk2DataQueryHandler : IRequestHandler<GetWk2DataQuery, Result<Wk2DataDto>>
{
    private const string CityRiyadh = "الرياض";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetWk2DataQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetWk2DataQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<Wk2DataDto>> Handle(GetWk2DataQuery request, CancellationToken cancellationToken)
    {
        List<WeekendPlace> activePlaces = await _queryExecutor.ToListAsync(
            _dbContext.WeekendPlaces.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ThenBy(p => p.CreatedAt),
            cancellationToken);

        List<WeekendDraft> publishedDrafts = await _queryExecutor.ToListAsync(
            _dbContext.WeekendDrafts.Where(d => d.Status == WeekendDraftStatus.Published), cancellationToken);
        WeekendDraft? latestDraft = publishedDrafts.OrderByDescending(d => d.PublishedAt).FirstOrDefault();

        JsonElement content = latestDraft is null
            ? default
            : JsonDocument.Parse(latestDraft.ContentJson).RootElement;

        IReadOnlyList<JsonElement> curatedOrDraftPlaces = activePlaces.Count > 0
            ? activePlaces.Select(ToPlaceJson).ToList()
            : ExtractArray(content, "places");

        var dto = new Wk2DataDto(
            curatedOrDraftPlaces,
            ExtractArray(content, "deals"),
            ExtractArray(content, "podcasts"),
            ExtractArray(content, "aiTools"),
            ExtractArray(content, "matches"),
            ExtractArray(content, "movies"),
            ExtractString(content, "summary"),
            latestDraft?.PublishedAt,
            latestDraft?.WeekendDate,
            CityRiyadh);

        return Result<Wk2DataDto>.Success(dto);
    }

    private static JsonElement ToPlaceJson(WeekendPlace place)
    {
        var payload = new
        {
            title = place.Name,
            body = place.Description,
            maps_query = string.IsNullOrEmpty(place.MapsQuery) ? place.Name : place.MapsQuery,
            imageUrl = place.ImageUrl,
            city = place.City,
            id = place.Id,
        };
        return JsonSerializer.SerializeToElement(payload);
    }

    private static IReadOnlyList<JsonElement> ExtractArray(JsonElement content, string propertyName)
    {
        if (content.ValueKind == JsonValueKind.Object &&
            content.TryGetProperty(propertyName, out JsonElement value) &&
            value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().ToList();
        }

        return Array.Empty<JsonElement>();
    }

    private static string? ExtractString(JsonElement content, string propertyName)
    {
        if (content.ValueKind == JsonValueKind.Object &&
            content.TryGetProperty(propertyName, out JsonElement value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }
}
