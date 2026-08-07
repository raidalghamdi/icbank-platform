using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>
/// Verifies <see cref="GenerateFinalMediaReportCommandHandler"/> ports BUSINESS-RULES.md §5.3's
/// <c>NO_SOURCE_DATA</c> guard exactly: zero posts and zero news in range skips the AI call and
/// returns a detailed diagnostic instead.
/// </summary>
public sealed class GenerateFinalMediaReportCommandHandlerTests
{
    private const int ActorUserId = 12;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IFinalReportSectionGenerator _sectionGenerator = Substitute.For<IFinalReportSectionGenerator>();
    private readonly GenerateFinalMediaReportCommandHandler _handler;

    public GenerateFinalMediaReportCommandHandlerTests()
    {
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());
        _dbContext.GacNewsItems.Returns(Array.Empty<GacNewsItem>().AsQueryable());
        _handler = new GenerateFinalMediaReportCommandHandler(_dbContext, _queryExecutor, _auditLogService, _sectionGenerator);
    }

    [Fact]
    public async Task Handle_NoSourceDataInRange_ReturnsNoSourceDataDiagnosticAndSkipsAiCall()
    {
        var command = new GenerateFinalMediaReportCommand(
            ActorUserId, "يوليو 2026", "عام", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, null);

        Result<GenerateFinalMediaReportResultDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NoSourceData.Should().NotBeNull();
        result.Value.NoSourceData!.Code.Should().Be("NO_SOURCE_DATA");
        result.Value.Draft.Should().BeNull();
        await _sectionGenerator.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SourceDataExists_CallsSectionGeneratorAndReturnsDraft()
    {
        _dbContext.GacNewsItems.Returns(new[]
        {
            new GacNewsItem { TitleAr = "خبر", PublishedAt = DateTimeOffset.UtcNow.AddDays(-1) },
        }.AsQueryable());
        _sectionGenerator.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FinalReportSections { ExecutiveSummary = "ملخص" });
        var command = new GenerateFinalMediaReportCommand(
            ActorUserId, "يوليو 2026", "عام", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, null);

        Result<GenerateFinalMediaReportResultDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Draft.Should().NotBeNull();
        result.Value.Draft!.ExecutiveSummary.Should().Be("ملخص");
        result.Value.NoSourceData.Should().BeNull();
        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "final_media_report.generate", "FinalMediaReport", Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoSourceData_DiagnosticReportsHistoricalDateRangeAcrossAllData()
    {
        _dbContext.GacNewsItems.Returns(new[]
        {
            new GacNewsItem { TitleAr = "قديم", PublishedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) },
        }.AsQueryable());
        var command = new GenerateFinalMediaReportCommand(
            ActorUserId, "يوليو 2026", "عام", DateTimeOffset.UtcNow.AddYears(1), DateTimeOffset.UtcNow.AddYears(1).AddDays(1), null);

        Result<GenerateFinalMediaReportResultDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.NoSourceData!.TotalNewsAvailable.Should().Be(1);
        result.Value.NoSourceData.EarliestAvailableDate.Should().Be(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }
}
