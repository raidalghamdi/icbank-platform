using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Application.Designs.Composer.Commands;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.Composer;

/// <summary>Verifies <see cref="RenderDesignCommandHandler"/> resolves the template/logos, delegates to the composer, and persists the result.</summary>
public sealed class RenderDesignCommandHandlerTests
{
    private const int ActorUserId = 11;

    private static readonly int[] LogoSelectionOrder = { 6, 5, 999 };

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IObjectStorageReader _storageReader = Substitute.For<IObjectStorageReader>();
    private readonly IObjectStorageWriter _storageWriter = Substitute.For<IObjectStorageWriter>();
    private readonly IDesignComposer _composer = Substitute.For<IDesignComposer>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly RenderDesignCommandHandler _handler;

    public RenderDesignCommandHandlerTests()
    {
        _handler = new RenderDesignCommandHandler(_dbContext, _queryExecutor, _storageReader, _storageWriter, _composer, _auditLogService);
    }

    [Fact]
    public async Task Handle_MissingTemplate_ReturnsFailureAndNeverComposes()
    {
        _dbContext.DesignTemplates.Returns(Array.Empty<DesignTemplate>().AsQueryable());

        Result<RenderDesignResultDto> result = await _handler.Handle(
            new RenderDesignCommand(ActorUserId, 404, "title", "body", null, null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _composer.DidNotReceive().ComposeAsync(Arg.Any<ComposeDesignInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidTemplateNoBackgroundNoLogos_ComposesAndSaves()
    {
        var template = new DesignTemplate { Id = 1, TemplateNameAr = "t", Category = "c" };
        _dbContext.DesignTemplates.Returns(new[] { template }.AsQueryable());
        _dbContext.BrandLogos.Returns(Array.Empty<BrandLogo>().AsQueryable());
        _composer.ComposeAsync(Arg.Any<ComposeDesignInput>(), Arg.Any<CancellationToken>()).Returns(new byte[] { 9 });
        _storageWriter.SaveAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("designs/generated/x.png");

        Result<RenderDesignResultDto> result = await _handler.Handle(
            new RenderDesignCommand(ActorUserId, 1, "title", "body", null, null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Url.Should().Be("designs/generated/x.png");
        await _storageReader.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SelectedLogoIds_LoadsOnlyMatchingLogosInRequestedOrder()
    {
        var template = new DesignTemplate { Id = 1, TemplateNameAr = "t", Category = "c" };
        var logoA = new BrandLogo { Id = 5, LogoName = "a", FileUrl = "urlA" };
        var logoB = new BrandLogo { Id = 6, LogoName = "b", FileUrl = "urlB" };
        _dbContext.DesignTemplates.Returns(new[] { template }.AsQueryable());
        _dbContext.BrandLogos.Returns(new[] { logoA, logoB }.AsQueryable());
        _composer.ComposeAsync(Arg.Any<ComposeDesignInput>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(System.Text.Encoding.UTF8.GetBytes(callInfo.Arg<ComposeDesignInput>().SelectedLogos.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        _storageWriter.SaveAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("path");

        await _handler.Handle(new RenderDesignCommand(ActorUserId, 1, null, null, null, LogoSelectionOrder, null, null, null, null), CancellationToken.None);

        await _composer.Received(1).ComposeAsync(Arg.Is<ComposeDesignInput>(i => i.SelectedLogos.Count == 2 && i.SelectedLogos[0].Id == 6), Arg.Any<CancellationToken>());
    }
}
