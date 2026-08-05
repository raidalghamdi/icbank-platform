using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs;
using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Application.Designs.Composer.Commands;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.Composer;

/// <summary>
/// Verifies <see cref="GenerateBackgroundsCommandHandler"/> is rate limited, tolerates partial
/// generation failure (BUSINESS-RULES.md §7.3's <c>Promise.allSettled</c> semantics), and always
/// writes an audit record on success.
/// </summary>
public sealed class GenerateBackgroundsCommandHandlerTests
{
    private const int ActorUserId = 21;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IBackgroundImageGenerator _imageGenerator = Substitute.For<IBackgroundImageGenerator>();
    private readonly IObjectStorageWriter _storageWriter = Substitute.For<IObjectStorageWriter>();
    private readonly IDesignGenerationRateLimiter _rateLimiter = Substitute.For<IDesignGenerationRateLimiter>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly GenerateBackgroundsCommandHandler _handler;

    public GenerateBackgroundsCommandHandlerTests()
    {
        _dbContext.DesignTemplates.Returns(Array.Empty<DesignTemplate>().AsQueryable());
        _rateLimiter.TryConsume(Arg.Any<int>()).Returns(true);
        _storageWriter.SaveAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("designs/backgrounds/x.png");
        _handler = new GenerateBackgroundsCommandHandler(_dbContext, _queryExecutor, _imageGenerator, _storageWriter, _rateLimiter, _auditLogService);
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ReturnsFailureAndNeverGenerates()
    {
        _rateLimiter.TryConsume(ActorUserId).Returns(false);

        Result<GenerateBackgroundsResultDto> result = await _handler.Handle(
            new GenerateBackgroundsCommand(ActorUserId, "prompt", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _imageGenerator.DidNotReceive().GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllFourGenerationsSucceed_ReturnsFourImages()
    {
        _imageGenerator.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedBackgroundImage(new byte[] { 1 }, "image/png"));

        Result<GenerateBackgroundsResultDto> result = await _handler.Handle(
            new GenerateBackgroundsCommand(ActorUserId, "prompt", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Images.Should().HaveCount(4);
        result.Value.Images.Should().OnlyContain(i => i.Source == "gemini");
    }

    [Fact]
    public async Task Handle_AllGenerationsFail_ReturnsFailure()
    {
        _imageGenerator.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GeneratedBackgroundImage>(new InvalidOperationException("QUOTA_EXCEEDED")));

        Result<GenerateBackgroundsResultDto> result = await _handler.Handle(
            new GenerateBackgroundsCommand(ActorUserId, "prompt", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Success_WritesAuditEntryWithSuccessCount()
    {
        _imageGenerator.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedBackgroundImage(new byte[] { 1 }, "image/png"));

        await _handler.Handle(new GenerateBackgroundsCommand(ActorUserId, "prompt", null), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "design.generate_backgrounds", "DesignTemplate", "none", Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }
}
