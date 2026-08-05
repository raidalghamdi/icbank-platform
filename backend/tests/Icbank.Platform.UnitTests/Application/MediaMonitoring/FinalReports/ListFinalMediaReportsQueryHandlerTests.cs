using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Queries;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="ListFinalMediaReportsQueryHandler"/> filtering, pagination envelope, and boundary behaviour.</summary>
public sealed class ListFinalMediaReportsQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly ListFinalMediaReportsQueryHandler _handler;

    public ListFinalMediaReportsQueryHandlerTests()
    {
        _handler = new ListFinalMediaReportsQueryHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllReportsWithPaginationEnvelope()
    {
        _dbContext.FinalMediaReports.Returns(new[] { FinalMediaReportTestData.BuildEntity(1), FinalMediaReportTestData.BuildEntity(2) }.AsQueryable());

        Result<PagedResult<FinalMediaReportDto>> result = await _handler.Handle(
            new ListFinalMediaReportsQuery(new PagedQuery { Page = 1, PageSize = 25 }, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Handle_YearFilter_ReturnsOnlyMatchingYear()
    {
        FinalMediaReport report2025 = FinalMediaReportTestData.BuildEntity(1);
        report2025.IssueDate = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        FinalMediaReport report2026 = FinalMediaReportTestData.BuildEntity(2);
        report2026.IssueDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        _dbContext.FinalMediaReports.Returns(new[] { report2025, report2026 }.AsQueryable());

        Result<PagedResult<FinalMediaReportDto>> result = await _handler.Handle(
            new ListFinalMediaReportsQuery(new PagedQuery { Page = 1, PageSize = 25 }, null, 2026), CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(item => item.Id == 2);
    }

    [Fact]
    public async Task Handle_ReportTypeFilter_ReturnsOnlyMatchingType()
    {
        FinalMediaReport weekly = FinalMediaReportTestData.BuildEntity(1);
        weekly.ReportType = MediaReportType.Weekly;
        FinalMediaReport monthly = FinalMediaReportTestData.BuildEntity(2);
        monthly.ReportType = MediaReportType.Monthly;
        _dbContext.FinalMediaReports.Returns(new[] { weekly, monthly }.AsQueryable());

        Result<PagedResult<FinalMediaReportDto>> result = await _handler.Handle(
            new ListFinalMediaReportsQuery(new PagedQuery { Page = 1, PageSize = 25 }, "Monthly", null), CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(item => item.Id == 2);
    }

    [Fact]
    public async Task Handle_EmptySet_ReturnsEmptyItemsWithZeroTotal()
    {
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());

        Result<PagedResult<FinalMediaReportDto>> result = await _handler.Handle(
            new ListFinalMediaReportsQuery(new PagedQuery { Page = 1, PageSize = 25 }, null, null), CancellationToken.None);

        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PageBeyondAvailableData_ReturnsEmptyPageWithCorrectTotal()
    {
        _dbContext.FinalMediaReports.Returns(new[] { FinalMediaReportTestData.BuildEntity(1) }.AsQueryable());

        Result<PagedResult<FinalMediaReportDto>> result = await _handler.Handle(
            new ListFinalMediaReportsQuery(new PagedQuery { Page = 5, PageSize = 25 }, null, null), CancellationToken.None);

        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(1);
    }
}
