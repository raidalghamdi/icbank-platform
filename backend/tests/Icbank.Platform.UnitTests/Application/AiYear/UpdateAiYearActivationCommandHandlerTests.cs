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
/// Verifies <see cref="UpdateAiYearActivationCommandHandler"/>'s partial-update semantics (only
/// non-null fields are applied), its delete-then-insert replacement of child collections when
/// supplied, the not-found short-circuit, and that a single <see cref="IApplicationDbContext.SaveChangesAsync"/>
/// call wraps the whole operation before the audit-log write (API-SURFACE.md §13).
/// </summary>
public sealed class UpdateAiYearActivationCommandHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly UpdateAiYearActivationCommandHandler _handler;

    public UpdateAiYearActivationCommandHandlerTests()
    {
        _handler = new UpdateAiYearActivationCommandHandler(_dbContext, _queryExecutor, _auditLogService);
    }

    [Fact]
    public async Task Handle_ActivationNotFound_ReturnsArabicNotFoundFailure()
    {
        _dbContext.AiYearActivations.Returns(Array.Empty<AiYearActivation>().AsQueryable());
        var command = new UpdateAiYearActivationCommand(1, 99, "New Title", null, null, null, null, null, null, null, null, null, null, null, null);

        Result<AiYearActivationDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("التفعيل غير موجود");
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyTitleSupplied_UpdatesTitleAndLeavesOtherFieldsUnchanged()
    {
        var activation = new AiYearActivation { Id = 1, Title = "Old", Month = 4, Type = "Campaign", Reach = 50 };
        SetActivations(activation);
        var command = new UpdateAiYearActivationCommand(1, 1, "New Title", null, null, null, null, null, null, null, null, null, null, null, null);

        Result<AiYearActivationDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("New Title");
        result.Value.Month.Should().Be(4, "an omitted field must be left untouched by a partial update");
        result.Value.Reach.Should().Be(50);
    }

    [Fact]
    public async Task Handle_StatusFieldWithUnrecognisedValue_LeavesStatusUnchanged()
    {
        var activation = new AiYearActivation { Id = 1, Title = "T", Status = AiYearActivationStatus.Draft };
        SetActivations(activation);
        var command = new UpdateAiYearActivationCommand(1, 1, null, null, null, null, null, null, "bogus", null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        activation.Status.Should().Be(AiYearActivationStatus.Draft);
    }

    [Fact]
    public async Task Handle_NullChannels_DoesNotTouchExistingChannels()
    {
        var activation = new AiYearActivation { Id = 1, Title = "T" };
        SetActivations(activation);
        _dbContext.AiYearActivationChannels.Returns(new[] { new AiYearActivationChannel { Id = 5, ActivationId = 1, Channel = "twitter" } }.AsQueryable());
        var command = new UpdateAiYearActivationCommand(1, 1, null, null, null, null, null, null, null, null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.DidNotReceive().Remove(Arg.Any<AiYearActivationChannel>());
        _dbContext.DidNotReceive().Add(Arg.Any<AiYearActivationChannel>());
    }

    [Fact]
    public async Task Handle_ChannelsSupplied_RemovesExistingThenAddsReplacements()
    {
        var activation = new AiYearActivation { Id = 1, Title = "T" };
        SetActivations(activation);
        var oldChannel = new AiYearActivationChannel { Id = 5, ActivationId = 1, Channel = "twitter" };
        _dbContext.AiYearActivationChannels.Returns(new[] { oldChannel }.AsQueryable());
        var newChannels = new[] { "instagram", "linkedin" };
        var command = new UpdateAiYearActivationCommand(1, 1, null, null, null, null, null, null, null, null, null, null, newChannels, null, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Remove(oldChannel);
        _dbContext.Received(1).Add(Arg.Is<AiYearActivationChannel>(c => c.Channel == "instagram"));
        _dbContext.Received(1).Add(Arg.Is<AiYearActivationChannel>(c => c.Channel == "linkedin"));
    }

    [Fact]
    public async Task Handle_MediaSupplied_ReplacesExistingMediaWithDefaultedSortOrder()
    {
        var activation = new AiYearActivation { Id = 1, Title = "T" };
        SetActivations(activation);
        var oldMedia = new AiYearMedia { Id = 7, ActivationId = 1, ObjectPath = "old.jpg" };
        _dbContext.AiYearMedia.Returns(new[] { oldMedia }.AsQueryable());
        CreateAiYearActivationMediaItem[] newMedia = new[] { new CreateAiYearActivationMediaItem("/objects/ai-year/2026/1/1/new.jpg", "new.jpg", "image/jpeg", null) };
        var command = new UpdateAiYearActivationCommand(1, 1, null, null, null, null, null, null, null, null, null, null, null, newMedia, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Remove(oldMedia);
        _dbContext.Received(1).Add(Arg.Is<AiYearMedia>(m => m.ActivationId == 1 && m.ObjectPath == "/objects/ai-year/2026/1/1/new.jpg" && m.SortOrder == 0));
    }

    [Fact]
    public async Task Handle_MetricsSupplied_ReplacesExistingMetrics()
    {
        var activation = new AiYearActivation { Id = 1, Title = "T" };
        SetActivations(activation);
        var oldMetric = new AiYearMetric { Id = 9, ActivationId = 1, MetricKey = "reach", MetricValue = "10" };
        _dbContext.AiYearMetrics.Returns(new[] { oldMetric }.AsQueryable());
        CreateAiYearActivationMetricItem[] newMetrics = new[] { new CreateAiYearActivationMetricItem("engagement", "200") };
        var command = new UpdateAiYearActivationCommand(1, 1, null, null, null, null, null, null, null, null, null, null, null, null, newMetrics);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Remove(oldMetric);
        _dbContext.Received(1).Add(Arg.Is<AiYearMetric>(m => m.MetricKey == "engagement" && m.MetricValue == "200"));
    }

    [Fact]
    public async Task Handle_ValidUpdate_WritesAuditLogAfterSaveChanges()
    {
        var activation = new AiYearActivation { Id = 1, Title = "T" };
        SetActivations(activation);
        var command = new UpdateAiYearActivationCommand(4, 1, "Updated", null, null, null, null, null, null, null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        Received.InOrder(() =>
        {
            _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>());
            _auditLogService.RecordAsync(
                4,
                "ai_year_activation.update",
                "AiYearActivation",
                Arg.Any<string>(),
                Arg.Is<object?>(before => before == null),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>());
        });
    }

    private void SetActivations(AiYearActivation activation)
    {
        _dbContext.AiYearActivations.Returns(new[] { activation }.AsQueryable());
        _dbContext.AiYearActivationChannels.Returns(Array.Empty<AiYearActivationChannel>().AsQueryable());
        _dbContext.AiYearMedia.Returns(Array.Empty<AiYearMedia>().AsQueryable());
        _dbContext.AiYearMetrics.Returns(Array.Empty<AiYearMetric>().AsQueryable());
    }
}
