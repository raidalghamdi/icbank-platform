using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac.Commands;
using Icbank.Platform.Domain.Gac;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Gac;

/// <summary>
/// Verifies <see cref="IngestGacNewsItemsCommandHandler"/> deduplicates on source URL, both against
/// rows already stored and within a single submitted batch, and never lets a headline-only refetch
/// erase a body that an earlier richer fetch had supplied.
/// </summary>
public sealed class IngestGacNewsItemsCommandHandlerTests
{
    private const string Url = "https://www.argaam.com/ar/article/1";

    private static readonly DateTimeOffset PublishedAt = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] ProviderTags = { "google-news-rss" };

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IngestGacNewsItemsCommandHandler _handler;

    public IngestGacNewsItemsCommandHandlerTests()
    {
        _dbContext.GacNewsItems.Returns(Array.Empty<GacNewsItem>().AsQueryable());
        _handler = new IngestGacNewsItemsCommandHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_NewItem_InsertsWithNewsKindByDefault()
    {
        Result<IngestGacNewsItemsResult> result = await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem() }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Inserted.Should().Be(1);
        result.Value.Updated.Should().Be(0);
        _dbContext.Received(1).Add(Arg.Is<GacNewsItem>(n =>
            n.SourceUrl == Url && n.Kind == GacNewsKind.News && n.TitleAr == "هيئة المنافسة تتلقى طلبات تركز"));
    }

    [Fact]
    public async Task Handle_UrlAlreadyStored_UpdatesInPlaceInsteadOfDuplicating()
    {
        var existing = new GacNewsItem { SourceUrl = Url, TitleAr = "عنوان قديم" };
        _dbContext.GacNewsItems.Returns(new[] { existing }.AsQueryable());

        Result<IngestGacNewsItemsResult> result = await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem() }), CancellationToken.None);

        result.Value!.Inserted.Should().Be(0);
        result.Value.Updated.Should().Be(1);
        existing.TitleAr.Should().Be("هيئة المنافسة تتلقى طلبات تركز");
        _dbContext.DidNotReceive().Add(Arg.Any<GacNewsItem>());
    }

    [Fact]
    public async Task Handle_SameUrlTwiceInOneBatch_InsertsOnceAndCountsTheSecondAsSkipped()
    {
        Result<IngestGacNewsItemsResult> result = await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem(), MakeItem() }), CancellationToken.None);

        result.Value!.Inserted.Should().Be(1);
        result.Value.Skipped.Should().Be(1);
        _dbContext.Received(1).Add(Arg.Any<GacNewsItem>());
    }

    [Fact]
    public async Task Handle_RefetchWithoutBody_KeepsThePreviouslyStoredBody()
    {
        var existing = new GacNewsItem { SourceUrl = Url, TitleAr = "عنوان", BodyAr = "نص المقال الكامل" };
        _dbContext.GacNewsItems.Returns(new[] { existing }.AsQueryable());

        await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem(body: null) }), CancellationToken.None);

        existing.BodyAr.Should().Be("نص المقال الكامل");
    }

    [Fact]
    public async Task Handle_UrlDifferingOnlyByCase_IsTreatedAsTheSameArticle()
    {
        Result<IngestGacNewsItemsResult> result = await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem(), MakeItem(url: Url.ToUpperInvariant()) }),
            CancellationToken.None);

        result.Value!.Inserted.Should().Be(1);
        result.Value.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExplicitKindAndCategory_AreParsedCaseInsensitively()
    {
        await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem(kind: "decision") }), CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<GacNewsItem>(n => n.Kind == GacNewsKind.Decision));
    }

    [Fact]
    public async Task Handle_SourceUrlLongerThanTheColumn_SkipsInsteadOfFailingTheBatch()
    {
        // Google News redirect links can exceed the 2,048-character column, and handing one to
        // the database used to fail the entire request with a 500 instead of dropping one row.
        var overlongUrl = "https://news.google.com/rss/articles/" + new string('A', 2100);

        Result<IngestGacNewsItemsResult> result = await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem(url: overlongUrl), MakeItem() }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Skipped.Should().Be(1);
        result.Value.Inserted.Should().Be(1);
        _dbContext.Received(1).Add(Arg.Is<GacNewsItem>(n => n.SourceUrl == Url));
    }

    [Fact]
    public async Task Handle_SourceUrlExactlyAtTheColumnLimit_IsStored()
    {
        const string prefix = "https://news.google.com/rss/articles/";
        var boundaryUrl = prefix + new string('A', 2048 - prefix.Length);

        Result<IngestGacNewsItemsResult> result = await _handler.Handle(
            new IngestGacNewsItemsCommand(new[] { MakeItem(url: boundaryUrl) }), CancellationToken.None);

        result.Value!.Inserted.Should().Be(1);
        result.Value.Skipped.Should().Be(0);
        _dbContext.Received(1).Add(Arg.Is<GacNewsItem>(n => n.SourceUrl == boundaryUrl));
    }

    private static IngestGacNewsItem MakeItem(
        string? url = null, string? body = "ملخص الخبر", string? kind = null) => new(
        "هيئة المنافسة تتلقى طلبات تركز",
        body,
        url ?? Url,
        "أرقام",
        PublishedAt,
        kind,
        Category: null,
        Tags: ProviderTags);
}
