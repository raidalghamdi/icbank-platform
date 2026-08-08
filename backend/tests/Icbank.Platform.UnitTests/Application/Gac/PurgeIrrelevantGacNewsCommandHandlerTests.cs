using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Gac.Commands;
using Icbank.Platform.Domain.Gac;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Gac;

/// <summary>
/// Verifies <see cref="PurgeIrrelevantGacNewsCommandHandler"/> deletes only the stored rows that
/// fail the relevance filter, and does not touch the database when everything already qualifies.
/// </summary>
public sealed class PurgeIrrelevantGacNewsCommandHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly PurgeIrrelevantGacNewsCommandHandler _handler;

    public PurgeIrrelevantGacNewsCommandHandlerTests()
    {
        _handler = new PurgeIrrelevantGacNewsCommandHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_MixedStore_RemovesOnlyTheIrrelevantRows()
    {
        GacNewsItem keep = Item("هيئة المنافسة تعتمد قرارات التركز الاقتصادي");
        GacNewsItem drop = Item("حكام دوري روشن يواصلون معسكرهم في إسبانيا");
        Given(keep, drop);

        Result<PurgeIrrelevantGacNewsResult> result = await _handler.Handle(new PurgeIrrelevantGacNewsCommand(), CancellationToken.None);

        result.Value!.Examined.Should().Be(2);
        result.Value.Removed.Should().Be(1);
        _dbContext.Received(1).Remove(drop);
        _dbContext.DidNotReceive().Remove(keep);
        await _dbContext.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_EverythingRelevant_DoesNotSave()
    {
        Given(Item("الهيئة العامة للمنافسة تعدل نظام العقوبات"));

        Result<PurgeIrrelevantGacNewsResult> result = await _handler.Handle(new PurgeIrrelevantGacNewsCommand(), CancellationToken.None);

        result.Value!.Removed.Should().Be(0);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyStore_ReportsZeroes()
    {
        Given();

        Result<PurgeIrrelevantGacNewsResult> result = await _handler.Handle(new PurgeIrrelevantGacNewsCommand(), CancellationToken.None);

        result.Value!.Examined.Should().Be(0);
        result.Value.Removed.Should().Be(0);
    }

    private static GacNewsItem Item(string title) => new() { TitleAr = title };

    private void Given(params GacNewsItem[] items)
        => _dbContext.GacNewsItems.Returns(items.AsQueryable());
}
