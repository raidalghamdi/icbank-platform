using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>
/// Verifies <see cref="GenerateMediaReportCommandHandler"/> ports BUSINESS-RULES.md §5.1: the
/// no-AI-call-on-empty-input guard, the 7/30-day default range resolution, and that a real audit
/// entry is written on every successful generation.
/// </summary>
public sealed class GenerateMediaReportCommandHandlerTests
{
    private const int ActorUserId = 42;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IMediaReportNarrativeGenerator _narrativeGenerator = Substitute.For<IMediaReportNarrativeGenerator>();
    private readonly GenerateMediaReportCommandHandler _handler;

    public GenerateMediaReportCommandHandlerTests()
    {
        _dateTimeProvider.RiyadhNow.Returns(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.FromHours(3)));
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());
        _dbContext.GacNewsItems.Returns(Array.Empty<GacNewsItem>().AsQueryable());
        _handler = new GenerateMediaReportCommandHandler(_dbContext, _queryExecutor, _auditLogService, _dateTimeProvider, _narrativeGenerator);
    }

    [Fact]
    public async Task Handle_NoSourceItemsInRange_SkipsAiCallAndProducesCannedMessage()
    {
        var command = new GenerateMediaReportCommand(ActorUserId, "manager", "weekly", null, null, null, null);

        Result<MediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentMd.Should().Contain("لا توجد بيانات");
        result.Value.ExecutiveSummary.Should().BeNull();
        await _narrativeGenerator.DidNotReceive().GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SourceItemsExist_CallsNarrativeGeneratorAndPersistsResult()
    {
        _dbContext.GacNewsItems.Returns(new[]
        {
            new GacNewsItem { TitleAr = "قرار جديد", PublishedAt = new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero) },
        }.AsQueryable());
        _narrativeGenerator.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MediaReportNarrative("## محتوى", "ملخص", "إيجابي"));
        var command = new GenerateMediaReportCommand(ActorUserId, "executive", "weekly", null, null, null, null);

        Result<MediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.ContentMd.Should().Be("## محتوى");
        result.Value.ExecutiveSummary.Should().Be("ملخص");
        result.Value.OverallTone.Should().Be("إيجابي");
        result.Value.Audience.Should().Be(nameof(MediaReportAudience.Executive));
        _dbContext.Received(1).Add(Arg.Any<MediaReport>());
        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "media_report.generate", "MediaReport", Arg.Any<string>(), Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnrecognisedAudience_FallsBackToManager()
    {
        var command = new GenerateMediaReportCommand(ActorUserId, "not-a-tier", "weekly", null, null, null, null);

        Result<MediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Audience.Should().Be(nameof(MediaReportAudience.Manager));
    }

    [Fact]
    public async Task Handle_MonthlyType_UsesThirtyDayDefaultRange()
    {
        var command = new GenerateMediaReportCommand(ActorUserId, "manager", "monthly", null, null, null, null);

        Result<MediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        (result.Value!.DateTo - result.Value.DateFrom).Days.Should().Be(30);
    }

    [Fact]
    public async Task Handle_ExplicitDateRange_OverridesDefaultResolution()
    {
        var dateFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var command = new GenerateMediaReportCommand(ActorUserId, "manager", "weekly", dateFrom, dateTo, null, null);

        Result<MediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.DateFrom.Should().Be(dateFrom);
        result.Value.DateTo.Should().Be(dateTo);
    }

    [Fact]
    public async Task Handle_CustomTitleProvided_UsesCustomTitleOverDefault()
    {
        var command = new GenerateMediaReportCommand(ActorUserId, "manager", "weekly", null, null, null, "عنوان مخصص");

        Result<MediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Title.Should().Be("عنوان مخصص");
    }
}
