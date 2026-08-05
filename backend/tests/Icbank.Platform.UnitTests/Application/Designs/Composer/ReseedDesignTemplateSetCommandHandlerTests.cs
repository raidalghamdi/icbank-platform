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
/// Verifies <see cref="ReseedDesignTemplateSetCommandHandler"/> reproduces BUSINESS-RULES.md
/// §7.1's idempotent-by-name, always-overwrite rule exactly.
/// </summary>
public sealed class ReseedDesignTemplateSetCommandHandlerTests
{
    private const int ActorUserId = 5;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IDesignTemplateSeedCatalog _seedCatalog = Substitute.For<IDesignTemplateSeedCatalog>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly ReseedDesignTemplateSetCommandHandler _handler;

    public ReseedDesignTemplateSetCommandHandlerTests()
    {
        _handler = new ReseedDesignTemplateSetCommandHandler(_dbContext, _queryExecutor, _seedCatalog, _auditLogService);
    }

    [Fact]
    public async Task Handle_TemplateNameDoesNotExist_InsertsNewRow()
    {
        _dbContext.DesignTemplates.Returns(Array.Empty<DesignTemplate>().AsQueryable());
        _seedCatalog.GetSeedSet(DesignTemplateSeedSet.Presentation).Returns(new List<DesignTemplateSeedDefinition>
        {
            new("قالب جديد", "presentation", 1920, 1440, null, new List<TextSlot>(), new List<LogoSlot>(), null, null),
        });

        Result<ReseedDesignTemplateSetResultDto> result = await _handler.Handle(
            new ReseedDesignTemplateSetCommand(ActorUserId, DesignTemplateSeedSet.Presentation), CancellationToken.None);

        result.Value!.Count.Should().Be(1);
        result.Value.Notes.Should().BeEmpty();
        _dbContext.Received(1).Add(Arg.Is<DesignTemplate>(t => t.TemplateNameAr == "قالب جديد"));
    }

    [Fact]
    public async Task Handle_TemplateNameAlreadyExists_OverwritesLayoutFieldsAndRecordsNote()
    {
        var existing = new DesignTemplate { TemplateNameAr = "قالب موجود", Category = "old", CanvasWidth = 100, CanvasHeight = 100 };
        _dbContext.DesignTemplates.Returns(new[] { existing }.AsQueryable());
        _seedCatalog.GetSeedSet(DesignTemplateSeedSet.SocialV2).Returns(new List<DesignTemplateSeedDefinition>
        {
            new("قالب موجود", "social", 1080, 1080, null, new List<TextSlot>(), new List<LogoSlot>(), "hint", null),
        });

        Result<ReseedDesignTemplateSetResultDto> result = await _handler.Handle(
            new ReseedDesignTemplateSetCommand(ActorUserId, DesignTemplateSeedSet.SocialV2), CancellationToken.None);

        existing.Category.Should().Be("social");
        existing.CanvasWidth.Should().Be(1080);
        existing.PromptHint.Should().Be("hint");
        result.Value!.Notes.Should().ContainSingle(n => n == "updated: قالب موجود");
        _dbContext.DidNotReceive().Add(Arg.Any<DesignTemplate>());
    }

    [Fact]
    public async Task Handle_Always_WritesAuditEntry()
    {
        _dbContext.DesignTemplates.Returns(Array.Empty<DesignTemplate>().AsQueryable());
        _seedCatalog.GetSeedSet(Arg.Any<DesignTemplateSeedSet>()).Returns(new List<DesignTemplateSeedDefinition>());

        await _handler.Handle(new ReseedDesignTemplateSetCommand(ActorUserId, DesignTemplateSeedSet.Year2026), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "design.template.reseed", "DesignTemplate", "Year2026", Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }
}
