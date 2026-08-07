using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>
/// Verifies <see cref="RunQuickAiToolCommandHandler"/> resolves the fixed 7-tool prompt set and
/// rejects unknown tool keys without an AI call (closes DEFECT-LOG.md SEC-02 for the previously-
/// unauthenticated <c>POST /ai/quick</c>).
/// </summary>
public sealed class RunQuickAiToolCommandHandlerTests
{
    private const int ActorUserId = 51;

    private readonly IPromptExecutionEngine _executionEngine = Substitute.For<IPromptExecutionEngine>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly RunQuickAiToolCommandHandler _handler;

    public RunQuickAiToolCommandHandlerTests()
    {
        _handler = new RunQuickAiToolCommandHandler(_executionEngine, _auditLogService);
    }

    [Fact]
    public async Task Handle_KnownTool_CallsExecutionEngineWithBuiltPromptAndReturnsOutput()
    {
        _executionEngine.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("ناتج");

        Result<RunQuickAiToolResultDto> result = await _handler.Handle(
            new RunQuickAiToolCommand(ActorUserId, "summary", "نص طويل", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Output.Should().Be("ناتج");
        result.Value.Tool.Should().Be("summary");
        await _executionEngine.Received(1).ExecuteAsync(Arg.Is<string>(p => p.Contains("نص طويل")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownTool_ReturnsFailureWithoutCallingExecutionEngine()
    {
        Result<RunQuickAiToolResultDto> result = await _handler.Handle(
            new RunQuickAiToolCommand(ActorUserId, "not-a-tool", "input", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _executionEngine.DidNotReceive().ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SuccessfulRun_WritesAuditEntry()
    {
        _executionEngine.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("ناتج");

        await _handler.Handle(new RunQuickAiToolCommand(ActorUserId, "generate", "input", null, null), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "ai_quick.run", "AiQuickTool", "generate", Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
