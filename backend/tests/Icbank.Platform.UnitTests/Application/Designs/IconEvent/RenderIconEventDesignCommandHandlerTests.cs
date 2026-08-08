using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Designs;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.IconEvent;

/// <summary>
/// Verifies <see cref="RenderIconEventDesignCommandHandler"/> is rate limited, persists the
/// rendered bytes via the storage port, and always writes an audit record -- the task's
/// "external-cost abuse vector" requirement for image-generation endpoints.
/// </summary>
public sealed class RenderIconEventDesignCommandHandlerTests
{
    private const int ActorUserId = 9;

    private static readonly byte[] RenderedBytes = { 1, 2, 3 };

    private readonly IIconEventImageRenderer _imageRenderer = Substitute.For<IIconEventImageRenderer>();
    private readonly IObjectStorageWriter _storageWriter = Substitute.For<IObjectStorageWriter>();
    private readonly IDesignGenerationRateLimiter _rateLimiter = Substitute.For<IDesignGenerationRateLimiter>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly RenderIconEventDesignCommandHandler _handler;

    public RenderIconEventDesignCommandHandlerTests()
    {
        _rateLimiter.TryConsume(Arg.Any<int>()).Returns(true);
        _imageRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<IconEventSizePreset>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(RenderedBytes);
        _storageWriter.SaveAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("designs/icon-event/abc.png");
        _handler = new RenderIconEventDesignCommandHandler(_imageRenderer, _storageWriter, _rateLimiter, _auditLogService);
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ReturnsFailureAndNeverRenders()
    {
        _rateLimiter.TryConsume(ActorUserId).Returns(false);

        Result<RenderIconEventDesignResultDto> result = await _handler.Handle(
            new RenderIconEventDesignCommand(ActorUserId, "<html></html>", "desktop-hd", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _imageRenderer.DidNotReceive().RenderAsync(Arg.Any<string>(), Arg.Any<IconEventSizePreset>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DefaultQuality_UsesHdScaleFactor()
    {
        Result<RenderIconEventDesignResultDto> result = await _handler.Handle(
            new RenderIconEventDesignCommand(ActorUserId, "<html></html>", "desktop-hd", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Quality.Should().Be("hd (3x)");
        result.Value.Width.Should().Be(1440 * 3);
    }

    [Fact]
    public async Task Handle_UltraQuality_UsesQuadrupleScaleFactor()
    {
        Result<RenderIconEventDesignResultDto> result = await _handler.Handle(
            new RenderIconEventDesignCommand(ActorUserId, "<html></html>", "web-standard", "ultra"), CancellationToken.None);

        result.Value!.Quality.Should().Be("ultra (4x)");
        result.Value.Width.Should().Be(1067 * 4);
    }

    [Fact]
    public async Task Handle_Success_PersistsBytesAndReturnsObjectPath()
    {
        Result<RenderIconEventDesignResultDto> result = await _handler.Handle(
            new RenderIconEventDesignCommand(ActorUserId, "<html></html>", "desktop-hd", null), CancellationToken.None);

        result.Value!.Url.Should().Be("designs/icon-event/abc.png");
        await _storageWriter.Received(1).SaveAsync("designs/icon-event/", RenderedBytes, "image/png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_WritesAuditEntry()
    {
        await _handler.Handle(new RenderIconEventDesignCommand(ActorUserId, "<html></html>", "desktop-hd", null), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "design.icon_event.render", "IconEventDesign", "designs/icon-event/abc.png", Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }
}
