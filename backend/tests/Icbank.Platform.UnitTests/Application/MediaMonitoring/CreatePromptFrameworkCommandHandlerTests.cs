using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="CreatePromptFrameworkCommandHandler"/> defaults and audit-write behaviour.</summary>
public sealed class CreatePromptFrameworkCommandHandlerTests
{
    private const int ActorUserId = 11;

    private static readonly string[] Tags = { "media", "gac" };

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly CreatePromptFrameworkCommandHandler _handler;

    public CreatePromptFrameworkCommandHandlerTests()
    {
        _handler = new CreatePromptFrameworkCommandHandler(_dbContext, _auditLogService);
    }

    [Fact]
    public async Task Handle_WellFormedCommand_ReturnsSuccessDtoWithMappedFields()
    {
        PromptVariableItem[] variables = { new("topic", "الموضوع", "string", true) };
        var command = new CreatePromptFrameworkCommand(
            ActorUserId, "اسم القالب", "Template Name", "وصف", "MediaReport", "Template", "نص {{topic}}", variables, "مثال", "ناتج", Tags, "gemini-2.5-flash");

        Result<PromptFrameworkDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NameAr.Should().Be("اسم القالب");
        result.Value.Category.Should().Be(nameof(PromptFrameworkCategory.MediaReport));
        result.Value.Kind.Should().Be(nameof(PromptFrameworkKind.Template));
        result.Value.Variables.Should().ContainSingle(v => v.Key == "topic");
        _dbContext.Received(1).Add(Arg.Any<PromptFramework>());
    }

    [Fact]
    public async Task Handle_OmittedCategoryAndKind_DefaultsToContentCreationAndFramework()
    {
        var command = new CreatePromptFrameworkCommand(ActorUserId, "اسم", null, null, null, null, "نص", null, null, null, null, null);

        Result<PromptFrameworkDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Category.Should().Be(nameof(PromptFrameworkCategory.ContentCreation));
        result.Value.Kind.Should().Be(nameof(PromptFrameworkKind.Framework));
    }

    [Fact]
    public async Task Handle_NewFramework_WritesAuditEntry()
    {
        var command = new CreatePromptFrameworkCommand(ActorUserId, "اسم", null, null, null, null, "نص", null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "prompt_framework.create", "PromptFramework", Arg.Any<string>(), Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
