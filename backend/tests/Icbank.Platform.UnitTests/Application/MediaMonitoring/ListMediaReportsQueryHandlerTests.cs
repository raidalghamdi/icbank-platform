using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Queries;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="ListMediaReportsQueryHandler"/> only returns published reports and applies the mandated pagination envelope (R-BE-033).</summary>
public sealed class ListMediaReportsQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly ListMediaReportsQueryHandler _handler;

    public ListMediaReportsQueryHandlerTests()
    {
        _handler = new ListMediaReportsQueryHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_DraftReportsExist_ExcludesThemFromResults()
    {
        _dbContext.MediaReports.Returns(new[]
        {
            MakeReport(1, MediaReportStatus.Published),
            MakeReport(2, MediaReportStatus.Draft),
        }.AsQueryable());
        var query = new ListMediaReportsQuery(new PagedQuery(), null, null);

        Result<PagedResult<MediaReportDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(r => r.Id == 1);
        result.Value.Total.Should().Be(1);
    }

    [Fact]
    public async Task Handle_AudienceFilter_ReturnsOnlyMatchingAudience()
    {
        MediaReport executive = MakeReport(1, MediaReportStatus.Published);
        executive.Audience = MediaReportAudience.Executive;
        MediaReport manager = MakeReport(2, MediaReportStatus.Published);
        manager.Audience = MediaReportAudience.Manager;
        _dbContext.MediaReports.Returns(new[] { executive, manager }.AsQueryable());
        var query = new ListMediaReportsQuery(new PagedQuery(), "Executive", null);

        Result<PagedResult<MediaReportDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(r => r.Id == 1);
    }

    [Fact]
    public async Task Handle_ReportTypeFilter_ReturnsOnlyMatchingType()
    {
        MediaReport weekly = MakeReport(1, MediaReportStatus.Published);
        weekly.ReportType = MediaReportType.Weekly;
        MediaReport monthly = MakeReport(2, MediaReportStatus.Published);
        monthly.ReportType = MediaReportType.Monthly;
        _dbContext.MediaReports.Returns(new[] { weekly, monthly }.AsQueryable());
        var query = new ListMediaReportsQuery(new PagedQuery(), null, "Monthly");

        Result<PagedResult<MediaReportDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(r => r.Id == 2);
    }

    [Fact]
    public async Task Handle_PageBeyondAvailableData_ReturnsEmptyItemsButCorrectTotal()
    {
        _dbContext.MediaReports.Returns(new[] { MakeReport(1, MediaReportStatus.Published) }.AsQueryable());
        var query = new ListMediaReportsQuery(new PagedQuery { Page = 5, PageSize = 10 }, null, null);

        Result<PagedResult<MediaReportDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(1);
    }

    private static MediaReport MakeReport(int id, MediaReportStatus status) => new()
    {
        Id = id,
        Title = $"Report {id}",
        Status = status,
        ContentMd = "content",
        CreatedAt = DateTime.UtcNow.AddMinutes(-id),
    };
}
