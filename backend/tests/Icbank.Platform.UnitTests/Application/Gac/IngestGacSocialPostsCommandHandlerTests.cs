using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac.Commands;
using Icbank.Platform.Domain.Gac;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Gac;

/// <summary>
/// Verifies <see cref="IngestGacSocialPostsCommandHandler"/> upserts on <c>(platform,
/// external_id)</c> as documented on <see cref="IngestGacSocialPostsCommand"/>, correctly
/// counting inserted vs. updated rows and falling back to <see cref="GacSocialMediaType.None"/>
/// for an unparseable media type (API-SURFACE.md §12).
/// </summary>
public sealed class IngestGacSocialPostsCommandHandlerTests
{
    private static readonly DateTimeOffset PostedAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IngestGacSocialPostsCommandHandler _handler;

    public IngestGacSocialPostsCommandHandlerTests()
    {
        _handler = new IngestGacSocialPostsCommandHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_NewPost_InsertsAndReturnsInsertedCountOfOne()
    {
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());
        var command = new IngestGacSocialPostsCommand(new[] { MakeItem() });

        Result<IngestGacSocialPostsResult> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Inserted.Should().Be(1);
        result.Value.Updated.Should().Be(0);
        _dbContext.Received(1).Add(Arg.Is<GacSocialPost>(p => p.ExternalId == "ext-1" && p.Platform == GacSocialPlatform.Twitter));
    }

    [Fact]
    public async Task Handle_ExistingPostSamePlatformAndExternalId_UpdatesInPlaceAndCountsAsUpdated()
    {
        var existing = new GacSocialPost { Platform = GacSocialPlatform.Twitter, ExternalId = "ext-1", ContentAr = "old", Account = "old-account" };
        _dbContext.GacSocialPosts.Returns(new[] { existing }.AsQueryable());
        var command = new IngestGacSocialPostsCommand(new[] { MakeItem() });

        Result<IngestGacSocialPostsResult> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Inserted.Should().Be(0);
        result.Value.Updated.Should().Be(1);
        existing.ContentAr.Should().Be("محتوى");
        existing.Account.Should().Be("GACOMPKSA");
        _dbContext.DidNotReceive().Add(Arg.Any<GacSocialPost>());
    }

    [Fact]
    public async Task Handle_UpdateWithNullPostUrlAndAccount_PreservesExistingValues()
    {
        var existing = new GacSocialPost { Platform = GacSocialPlatform.Twitter, ExternalId = "ext-1", PostUrl = "https://keep-me", Account = "keep-account" };
        _dbContext.GacSocialPosts.Returns(new[] { existing }.AsQueryable());
        var item = new IngestGacSocialPostItem("Twitter", "ext-1", "ar", "en", null, null, null, PostedAt, null);
        var command = new IngestGacSocialPostsCommand(new[] { item });

        await _handler.Handle(command, CancellationToken.None);

        existing.PostUrl.Should().Be("https://keep-me");
        existing.Account.Should().Be("keep-account");
    }

    [Fact]
    public async Task Handle_UnparseableMediaType_FallsBackToNone()
    {
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());
        var command = new IngestGacSocialPostsCommand(new[] { MakeItem(mediaType: "not-a-type") });

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<GacSocialPost>(p => p.MediaType == GacSocialMediaType.None));
    }

    [Fact]
    public async Task Handle_SameExternalIdDifferentPlatform_TreatedAsSeparateInserts()
    {
        var existing = new GacSocialPost { Platform = GacSocialPlatform.LinkedIn, ExternalId = "ext-1" };
        _dbContext.GacSocialPosts.Returns(new[] { existing }.AsQueryable());
        var command = new IngestGacSocialPostsCommand(new[] { MakeItem(platform: "Twitter") });

        Result<IngestGacSocialPostsResult> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Inserted.Should().Be(1);
        result.Value.Updated.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MixedBatch_CountsInsertsAndUpdatesIndependently()
    {
        var existing = new GacSocialPost { Platform = GacSocialPlatform.Twitter, ExternalId = "ext-1" };
        _dbContext.GacSocialPosts.Returns(new[] { existing }.AsQueryable());
        var command = new IngestGacSocialPostsCommand(new[] { MakeItem(externalId: "ext-1"), MakeItem(externalId: "ext-2") });

        Result<IngestGacSocialPostsResult> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Inserted.Should().Be(1);
        result.Value.Updated.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidBatch_CallsSaveChangesExactlyOnce()
    {
        _dbContext.GacSocialPosts.Returns(Array.Empty<GacSocialPost>().AsQueryable());
        var command = new IngestGacSocialPostsCommand(new[] { MakeItem(externalId: "a"), MakeItem(externalId: "b") });

        await _handler.Handle(command, CancellationToken.None);

        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static IngestGacSocialPostItem MakeItem(
        string platform = "Twitter", string externalId = "ext-1", string? mediaType = "Image", string? account = "GACOMPKSA") =>
        new(platform, externalId, "محتوى", "content", "https://x.com/1", "https://img", mediaType, PostedAt, account);
}
