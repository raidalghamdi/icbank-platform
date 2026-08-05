using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Application.Designs.Composer.Commands;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.Composer;

/// <summary>
/// Verifies <see cref="SetDefaultBrandFontCommandHandler"/> closes DEFECT-LOG.md DATA-01: the
/// unconditional, non-transactional "clear all rows then set one" race is replaced by a scoped
/// clear (only currently-true rows) plus a single atomic <c>SaveChangesAsync</c>.
/// </summary>
public sealed class SetDefaultBrandFontCommandHandlerTests
{
    private const int ActorUserId = 3;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly SetDefaultBrandFontCommandHandler _handler;

    public SetDefaultBrandFontCommandHandlerTests()
    {
        _handler = new SetDefaultBrandFontCommandHandler(_dbContext, _queryExecutor, _auditLogService);
    }

    [Fact]
    public async Task Handle_MissingFont_ReturnsFailure()
    {
        _dbContext.BrandFonts.Returns(Array.Empty<BrandFont>().AsQueryable());

        Result<BrandFontDto> result = await _handler.Handle(new SetDefaultBrandFontCommand(ActorUserId, 404), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AnotherFontIsCurrentlyDefault_ClearsOnlyThatOneAndSetsNewTarget()
    {
        var currentDefault = new BrandFont { Id = 1, FontName = "old", FontFileUrl = "url1", IsDefault = true };
        var target = new BrandFont { Id = 2, FontName = "new", FontFileUrl = "url2", IsDefault = false };
        var unrelated = new BrandFont { Id = 3, FontName = "unrelated", FontFileUrl = "url3", IsDefault = false };
        _dbContext.BrandFonts.Returns(new[] { currentDefault, target, unrelated }.AsQueryable());

        Result<BrandFontDto> result = await _handler.Handle(new SetDefaultBrandFontCommand(ActorUserId, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        currentDefault.IsDefault.Should().BeFalse();
        target.IsDefault.Should().BeTrue();
        unrelated.IsDefault.Should().BeFalse();
        result.Value!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoExistingDefault_SetsTargetWithoutTouchingOtherRows()
    {
        var target = new BrandFont { Id = 2, FontName = "new", FontFileUrl = "url2", IsDefault = false };
        var other = new BrandFont { Id = 3, FontName = "other", FontFileUrl = "url3", IsDefault = false };
        _dbContext.BrandFonts.Returns(new[] { target, other }.AsQueryable());

        await _handler.Handle(new SetDefaultBrandFontCommand(ActorUserId, 2), CancellationToken.None);

        target.IsDefault.Should().BeTrue();
        other.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Success_WritesAuditEntry()
    {
        var target = new BrandFont { Id = 2, FontName = "new", FontFileUrl = "url2", IsDefault = false };
        _dbContext.BrandFonts.Returns(new[] { target }.AsQueryable());

        await _handler.Handle(new SetDefaultBrandFontCommand(ActorUserId, 2), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "design.font.set_default", "BrandFont", "2", Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }
}
