using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac.Commands;
using Icbank.Platform.Domain.Gac;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Gac;

/// <summary>
/// Verifies <see cref="SeedGacTwitterSamplesCommandHandler"/> ports the Node source's 5 fixed
/// sample posts verbatim, including idempotency keyed on (platform, externalId) and the
/// inserted/skipped counters reported to the caller and audit log (API-SURFACE.md §12).
/// </summary>
public sealed class SeedGacTwitterSamplesCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] AllSampleExternalIds =
    {
        "tw-gac-2026-07-08-launch", "tw-gac-2026-07-05-report", "tw-gac-2026-07-02-oecd",
        "tw-gac-2026-06-28-workshop", "tw-gac-2026-06-25-decision",
    };

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly SeedGacTwitterSamplesCommandHandler _handler;

    public SeedGacTwitterSamplesCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _handler = new SeedGacTwitterSamplesCommandHandler(_dbContext, _queryExecutor, _dateTimeProvider, _auditLogService);
    }

    [Fact]
    public async Task Handle_NoExistingPosts_InsertsAllFiveSamples()
    {
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());

        Result<SeedGacTwitterSamplesResult> result = await _handler.Handle(new SeedGacTwitterSamplesCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Inserted.Should().Be(5);
        result.Value.Skipped.Should().Be(0);
        result.Value.Total.Should().Be(5);
        _dbContext.Received(5).Add(Arg.Any<GacSocialPost>());
    }

    [Fact]
    public async Task Handle_AllSamplesAlreadyExist_SkipsAllAndInsertsNone()
    {
        GacSocialPost[] existing = AllSampleExternalIds
            .Select(id => new GacSocialPost { Platform = GacSocialPlatform.Twitter, ExternalId = id }).ToArray();
        _dbContext.GacSocialPosts.Returns(existing.AsQueryable());

        Result<SeedGacTwitterSamplesResult> result = await _handler.Handle(new SeedGacTwitterSamplesCommand(1), CancellationToken.None);

        result.Value!.Inserted.Should().Be(0);
        result.Value.Skipped.Should().Be(5);
        _dbContext.DidNotReceive().Add(Arg.Any<GacSocialPost>());
    }

    [Fact]
    public async Task Handle_OneSampleAlreadyExists_InsertsRemainingFourAndSkipsOne()
    {
        GacSocialPost[] existing = new[] { new GacSocialPost { Platform = GacSocialPlatform.Twitter, ExternalId = "tw-gac-2026-07-08-launch" } };
        _dbContext.GacSocialPosts.Returns(existing.AsQueryable());

        Result<SeedGacTwitterSamplesResult> result = await _handler.Handle(new SeedGacTwitterSamplesCommand(1), CancellationToken.None);

        result.Value!.Inserted.Should().Be(4);
        result.Value.Skipped.Should().Be(1);
        _dbContext.Received(4).Add(Arg.Any<GacSocialPost>());
    }

    [Fact]
    public async Task Handle_ExistingSampleOnDifferentPlatform_IsNotTreatedAsDuplicate()
    {
        GacSocialPost[] existing = new[] { new GacSocialPost { Platform = GacSocialPlatform.LinkedIn, ExternalId = "tw-gac-2026-07-08-launch" } };
        _dbContext.GacSocialPosts.Returns(existing.AsQueryable());

        Result<SeedGacTwitterSamplesResult> result = await _handler.Handle(new SeedGacTwitterSamplesCommand(1), CancellationToken.None);

        result.Value!.Inserted.Should().Be(5, "the existing row is for a different platform so it is not a duplicate key match");
    }

    [Fact]
    public async Task Handle_InsertedSample_UsesInjectedClockForPostedAtInsteadOfRealTime()
    {
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());
        GacSocialPost? captured = null;
        _dbContext.When(c => c.Add(Arg.Any<GacSocialPost>())).Do(callInfo =>
        {
            GacSocialPost post = callInfo.Arg<GacSocialPost>();
            captured ??= post;
        });

        await _handler.Handle(new SeedGacTwitterSamplesCommand(1), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.PostedAt.Should().BeOnOrBefore(FixedNow);
        captured.Account.Should().Be("GACOMPKSA");
    }

    [Fact]
    public async Task Handle_ValidSeed_WritesBulkAuditLogEntryAfterSaveChanges()
    {
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());

        await _handler.Handle(new SeedGacTwitterSamplesCommand(7), CancellationToken.None);

        Received.InOrder(() =>
        {
            _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>());
            _auditLogService.RecordAsync(
                7,
                "gac_social_post.seed_twitter",
                "GacSocialPost",
                "bulk",
                Arg.Is<object?>(before => before == null),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>());
        });
    }
}
