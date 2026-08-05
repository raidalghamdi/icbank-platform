using FluentAssertions;
using Icbank.Platform.Application.AiYear;
using Icbank.Platform.Application.AiYear.Queries;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.AiYear;

/// <summary>
/// Verifies <see cref="ListAiYearActivationsQueryHandler"/> closes DEFECT-LOG.md DATA-06: media,
/// metrics and channels for the current page are fetched with batched queries and correctly
/// grouped back onto their owning activation, and every filter/pagination boundary behaves as
/// documented in API-SURFACE.md §13.
/// </summary>
public sealed class ListAiYearActivationsQueryHandlerTests
{
    private static readonly int[] MatchingSearchIds = { 1, 2 };

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly ListAiYearActivationsQueryHandler _handler;

    public ListAiYearActivationsQueryHandlerTests()
    {
        _handler = new ListAiYearActivationsQueryHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllOrderedByMonthDescendingThenCreatedAtDescending()
    {
        AiYearActivation a1 = MakeActivation(1, 3, "March");
        AiYearActivation a2 = MakeActivation(2, 8, "August");
        AiYearActivation a3 = MakeActivation(3, 8, "August Later");
        a3.CreatedAt = a2.CreatedAt.AddHours(1);
        _dbContext.AiYearActivations.Returns(new[] { a1, a2, a3 }.AsQueryable());
        SetEmptyChildTables();
        var query = new ListAiYearActivationsQuery(new PagedQuery(), null, null, null, null);

        Result<PagedResult<AiYearActivationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(3);
        result.Value.Items.Select(i => i.Id).Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public async Task Handle_MonthFilter_ReturnsOnlyMatchingMonth()
    {
        _dbContext.AiYearActivations.Returns(new[] { MakeActivation(1, 3, "March"), MakeActivation(2, 8, "August") }.AsQueryable());
        SetEmptyChildTables();
        var query = new ListAiYearActivationsQuery(new PagedQuery(), Month: 3, null, null, null);

        Result<PagedResult<AiYearActivationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(i => i.Id == 1);
        result.Value.Total.Should().Be(1);
    }

    [Fact]
    public async Task Handle_TypeFilter_ReturnsOnlyMatchingType()
    {
        _dbContext.AiYearActivations.Returns(new[] { MakeActivation(1, 3, "A", "Webinar"), MakeActivation(2, 3, "B", "Campaign") }.AsQueryable());
        SetEmptyChildTables();
        var query = new ListAiYearActivationsQuery(new PagedQuery(), null, Type: "Webinar", null, null);

        Result<PagedResult<AiYearActivationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(i => i.Id == 1);
    }

    [Fact]
    public async Task Handle_SearchTextMatchesTitleOrDescription_ReturnsMatches()
    {
        _dbContext.AiYearActivations.Returns(new[]
        {
            MakeActivation(1, 3, "Launch Event", description: "generic"),
            MakeActivation(2, 3, "Other", description: "mentions Launch here"),
            MakeActivation(3, 3, "Unrelated", description: "nothing"),
        }.AsQueryable());
        SetEmptyChildTables();
        var query = new ListAiYearActivationsQuery(new PagedQuery(), null, null, null, SearchText: "Launch");

        Result<PagedResult<AiYearActivationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Select(i => i.Id).Should().BeEquivalentTo(MatchingSearchIds);
    }

    [Fact]
    public async Task Handle_ChannelFilter_ReturnsOnlyActivationsOnThatChannel()
    {
        AiYearActivation matching = MakeActivation(1, 3, "A");
        AiYearActivation other = MakeActivation(2, 3, "B");
        matching.Channels.Add(new AiYearActivationChannel { ActivationId = 1, Activation = matching, Channel = "twitter" });
        other.Channels.Add(new AiYearActivationChannel { ActivationId = 2, Activation = other, Channel = "instagram" });
        _dbContext.AiYearActivations.Returns(new[] { matching, other }.AsQueryable());
        SetEmptyChildTables();
        var query = new ListAiYearActivationsQuery(new PagedQuery(), null, null, Channel: "twitter", null);

        Result<PagedResult<AiYearActivationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(i => i.Id == 1);
    }

    [Fact]
    public async Task Handle_PageBeyondAvailableData_ReturnsEmptyItemsButCorrectTotal()
    {
        _dbContext.AiYearActivations.Returns(new[] { MakeActivation(1, 3, "A") }.AsQueryable());
        SetEmptyChildTables();
        var query = new ListAiYearActivationsQuery(new PagedQuery { Page = 5, PageSize = 10 }, null, null, null, null);

        Result<PagedResult<AiYearActivationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(1);
        result.Value.Page.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ActivationHasMediaMetricsAndChannels_MapsThemGroupedByActivationId()
    {
        AiYearActivation a1 = MakeActivation(1, 3, "A");
        AiYearActivation a2 = MakeActivation(2, 3, "B");
        _dbContext.AiYearActivations.Returns(new[] { a1, a2 }.AsQueryable());
        _dbContext.AiYearMedia.Returns(new[]
        {
            new AiYearMedia { Id = 10, ActivationId = 1, ObjectPath = "p1", SortOrder = 1 },
            new AiYearMedia { Id = 11, ActivationId = 1, ObjectPath = "p0", SortOrder = 0 },
            new AiYearMedia { Id = 12, ActivationId = 2, ObjectPath = "other", SortOrder = 0 },
        }.AsQueryable());
        _dbContext.AiYearMetrics.Returns(new[]
        {
            new AiYearMetric { Id = 20, ActivationId = 1, MetricKey = "reach", MetricValue = "10" },
        }.AsQueryable());
        _dbContext.AiYearActivationChannels.Returns(new[]
        {
            new AiYearActivationChannel { Id = 30, ActivationId = 1, Channel = "twitter" },
        }.AsQueryable());
        var query = new ListAiYearActivationsQuery(new PagedQuery(), null, null, null, null);

        Result<PagedResult<AiYearActivationDto>> result = await _handler.Handle(query, CancellationToken.None);

        AiYearActivationDto dto1 = result.Value!.Items.Single(i => i.Id == 1);
        dto1.Media.Select(m => m.ObjectPath).Should().ContainInOrder("p0", "p1");
        dto1.Metrics.Should().ContainSingle(m => m.MetricKey == "reach");
        dto1.Channels.Should().ContainSingle(c => c == "twitter");

        AiYearActivationDto dto2 = result.Value.Items.Single(i => i.Id == 2);
        dto2.Media.Should().ContainSingle(m => m.ObjectPath == "other");
        dto2.Metrics.Should().BeEmpty();
        dto2.Channels.Should().BeEmpty();
    }

    private static AiYearActivation MakeActivation(int id, int month, string title, string type = "Campaign", string? description = null) => new()
    {
        Id = id,
        Title = title,
        Month = month,
        Year = 2026,
        Type = type,
        Description = description,
        CreatedAt = DateTime.UtcNow.AddMinutes(-id),
    };

    private void SetEmptyChildTables()
    {
        _dbContext.AiYearMedia.Returns(Array.Empty<AiYearMedia>().AsQueryable());
        _dbContext.AiYearMetrics.Returns(Array.Empty<AiYearMetric>().AsQueryable());
        _dbContext.AiYearActivationChannels.Returns(Array.Empty<AiYearActivationChannel>().AsQueryable());
    }
}
