using FluentAssertions;
using Icbank.Platform.Application.AiYear;
using Icbank.Platform.Application.AiYear.Commands;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.AiYear;

/// <summary>
/// Verifies <see cref="CreateAiYearActivationCommandHandler"/> ports the "validated before any DB
/// write" ordering exactly: the activation plus its channel/media/metric children are added within
/// a single <see cref="IApplicationDbContext.SaveChangesAsync"/> call and an audit-log entry is
/// written afterwards (API-SURFACE.md §13).
/// </summary>
public sealed class CreateAiYearActivationCommandHandlerTests
{
    private static readonly string[] TwitterChannel = { "twitter" };
    private static readonly string[] MultiChannel = { "twitter", "instagram" };
    private static readonly string[] Tags = { "ai", "2026" };

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly CreateAiYearActivationCommandHandler _handler;

    public CreateAiYearActivationCommandHandlerTests()
    {
        _handler = new CreateAiYearActivationCommandHandler(_dbContext, _auditLogService);
    }

    [Fact]
    public async Task Handle_WellFormedCommand_ReturnsSuccessDtoWithMappedFields()
    {
        var command = new CreateAiYearActivationCommand(
            7, "Launch Day", 5, 2026, "2026-05-01", "Campaign", TwitterChannel, "desc", Tags, "Published", 100, 50, "note", null, null);

        Result<AiYearActivationDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Launch Day");
        result.Value.Month.Should().Be(5);
        result.Value.Year.Should().Be(2026);
        result.Value.Status.Should().Be("Published");
        result.Value.Channels.Should().BeEquivalentTo(TwitterChannel);
        result.Value.Tags.Should().BeEquivalentTo(Tags);
        result.Value.Reach.Should().Be(100);
        result.Value.Engagement.Should().Be(50);
    }

    [Fact]
    public async Task Handle_OmittedYearAndStatus_DefaultsToCurrentYearAndPublished()
    {
        var command = new CreateAiYearActivationCommand(
            7, "No Year", 3, null, null, "Campaign", TwitterChannel, null, null, null, null, null, null, null, null);

        Result<AiYearActivationDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Year.Should().Be(2026);
        result.Value.Status.Should().Be("Published");
    }

    [Fact]
    public async Task Handle_UnrecognisedStatus_FallsBackToPublished()
    {
        var command = new CreateAiYearActivationCommand(
            7, "Bad Status", 3, 2026, null, "Campaign", TwitterChannel, null, null, "not-a-status", null, null, null, null, null);

        Result<AiYearActivationDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Status.Should().Be("Published");
    }

    [Fact]
    public async Task Handle_MultipleChannels_AddsOneChannelEntityPerChannel()
    {
        var command = new CreateAiYearActivationCommand(
            7, "Multi", 3, 2026, null, "Campaign", MultiChannel, null, null, null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<AiYearActivation>(a => a.Title == "Multi"));
        _dbContext.Received(1).Add(Arg.Is<AiYearActivationChannel>(c => c.Channel == "twitter"));
        _dbContext.Received(1).Add(Arg.Is<AiYearActivationChannel>(c => c.Channel == "instagram"));
    }

    [Fact]
    public async Task Handle_MediaProvided_AddsMediaEntityWithDefaultedSortOrder()
    {
        CreateAiYearActivationMediaItem[] media = new[] { new CreateAiYearActivationMediaItem("/objects/ai-year/2026/5/1/photo.jpg", "photo.jpg", "image/jpeg", null) };
        var command = new CreateAiYearActivationCommand(
            7, "With Media", 5, 2026, null, "Campaign", TwitterChannel, null, null, null, null, null, null, media, null);

        Result<AiYearActivationDto> result = await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<AiYearMedia>(m => m.ObjectPath == "/objects/ai-year/2026/5/1/photo.jpg" && m.SortOrder == 0));
        result.Value!.Media.Should().ContainSingle(m => m.ObjectPath == "/objects/ai-year/2026/5/1/photo.jpg" && m.SortOrder == 0);
    }

    [Fact]
    public async Task Handle_MetricsProvided_AddsMetricEntities()
    {
        CreateAiYearActivationMetricItem[] metrics = new[] { new CreateAiYearActivationMetricItem("impressions", "1000") };
        var command = new CreateAiYearActivationCommand(
            7, "With Metrics", 5, 2026, null, "Campaign", TwitterChannel, null, null, null, null, null, null, null, metrics);

        Result<AiYearActivationDto> result = await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<AiYearMetric>(m => m.MetricKey == "impressions" && m.MetricValue == "1000"));
        result.Value!.Metrics.Should().ContainSingle(m => m.MetricKey == "impressions" && m.MetricValue == "1000");
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsBeforeWritingAuditLog()
    {
        var command = new CreateAiYearActivationCommand(
            42, "Audit Check", 5, 2026, null, "Campaign", TwitterChannel, null, null, null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        Received.InOrder(() =>
        {
            _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>());
            _auditLogService.RecordAsync(
                42,
                "ai_year_activation.create",
                "AiYearActivation",
                Arg.Any<string>(),
                Arg.Is<object?>(before => before == null),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChangesExactlyOnce()
    {
        var command = new CreateAiYearActivationCommand(
            7, "Single Save", 5, 2026, null, "Campaign", TwitterChannel, null, null, null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
