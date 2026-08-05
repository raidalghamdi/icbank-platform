using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="UpdatePromptFrameworkCommandHandler"/> partial-update semantics and the not-found failure path.</summary>
public sealed class UpdatePromptFrameworkCommandHandlerTests
{
    private const int ActorUserId = 21;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly UpdatePromptFrameworkCommandHandler _handler;

    public UpdatePromptFrameworkCommandHandlerTests()
    {
        _handler = new UpdatePromptFrameworkCommandHandler(_dbContext, _queryExecutor, _auditLogService);
    }

    [Fact]
    public async Task Handle_ExistingFramework_UpdatesOnlySuppliedFields()
    {
        var framework = new PromptFramework { Id = 3, NameAr = "قديم", PromptText = "نص قديم", ExampleInput = "مثال قديم" };
        _dbContext.PromptFrameworks.Returns(new[] { framework }.AsQueryable());
        var command = new UpdatePromptFrameworkCommand(ActorUserId, 3, "جديد", null, null, null, null, null, null, null, null);

        Result<PromptFrameworkDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NameAr.Should().Be("جديد");
        result.Value.PromptText.Should().Be("نص قديم");
        result.Value.ExampleInput.Should().Be("مثال قديم");
    }

    [Fact]
    public async Task Handle_VariablesSupplied_ReplacesVariableList()
    {
        var framework = new PromptFramework { Id = 4, NameAr = "n", PromptText = "p" };
        _dbContext.PromptFrameworks.Returns(new[] { framework }.AsQueryable());
        PromptVariableItem[] variables = { new("k", "l", null, null) };
        var command = new UpdatePromptFrameworkCommand(ActorUserId, 4, null, null, null, null, variables, null, null, null, null);

        Result<PromptFrameworkDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Variables.Should().ContainSingle(v => v.Key == "k");
    }

    [Fact]
    public async Task Handle_MissingFramework_ReturnsFailureAndDoesNotAudit()
    {
        _dbContext.PromptFrameworks.Returns(Array.Empty<PromptFramework>().AsQueryable());
        var command = new UpdatePromptFrameworkCommand(ActorUserId, 404, "x", null, null, null, null, null, null, null, null);

        Result<PromptFrameworkDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _auditLogService.DidNotReceive().RecordAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
