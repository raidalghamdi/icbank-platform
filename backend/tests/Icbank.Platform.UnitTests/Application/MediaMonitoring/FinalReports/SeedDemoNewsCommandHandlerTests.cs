using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.Gac;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>
/// Verifies <see cref="SeedDemoNewsCommandHandler"/> ports the Node source's fixed 6-news/6-post
/// demo fixture set (BUSINESS-RULES.md §5 seed-demo helper) and writes an audit entry.
/// </summary>
public sealed class SeedDemoNewsCommandHandlerTests
{
    private const int ActorUserId = 5;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly SeedDemoNewsCommandHandler _handler;

    public SeedDemoNewsCommandHandlerTests()
    {
        _dateTimeProvider.RiyadhNow.Returns(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.FromHours(3)));
        _handler = new SeedDemoNewsCommandHandler(_dbContext, _auditLogService, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Always_Inserts6NewsAnd6Posts()
    {
        var addedNews = new List<GacNewsItem>();
        var addedPosts = new List<GacSocialPost>();
        _dbContext.When(context => context.Add(Arg.Any<GacNewsItem>())).Do(callInfo => addedNews.Add(callInfo.Arg<GacNewsItem>()));
        _dbContext.When(context => context.Add(Arg.Any<GacSocialPost>())).Do(callInfo => addedPosts.Add(callInfo.Arg<GacSocialPost>()));

        Result<SeedDemoNewsResultDto> result = await _handler.Handle(new SeedDemoNewsCommand(ActorUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SeededNews.Should().Be(6);
        result.Value.SeededPosts.Should().Be(6);
        addedNews.Should().HaveCount(6);
        addedPosts.Should().HaveCount(6);
    }

    [Fact]
    public async Task Handle_Always_PostsDatedWithinLastSevenDaysOfRiyadhClock()
    {
        var addedPosts = new List<GacSocialPost>();
        _dbContext.When(context => context.Add(Arg.Any<GacSocialPost>())).Do(callInfo => addedPosts.Add(callInfo.Arg<GacSocialPost>()));

        await _handler.Handle(new SeedDemoNewsCommand(ActorUserId), CancellationToken.None);

        addedPosts.Should().OnlyContain(post => post.PostedAt >= _dateTimeProvider.RiyadhNow.AddDays(-7) && post.PostedAt <= _dateTimeProvider.RiyadhNow);
    }

    [Fact]
    public async Task Handle_Always_WritesAuditEntryWithSeedCounts()
    {
        await _handler.Handle(new SeedDemoNewsCommand(ActorUserId), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "final_media_report.seed_demo", "GacNewsItem", "seed-demo", Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Always_ReturnsArabicConfirmationMessage()
    {
        Result<SeedDemoNewsResultDto> result = await _handler.Handle(new SeedDemoNewsCommand(ActorUserId), CancellationToken.None);

        result.Value!.Message.Should().Contain("6").And.Contain("زراعة");
    }

    [Fact]
    public async Task Handle_Always_CallsSaveChangesExactlyOnce()
    {
        await _handler.Handle(new SeedDemoNewsCommand(ActorUserId), CancellationToken.None);

        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
