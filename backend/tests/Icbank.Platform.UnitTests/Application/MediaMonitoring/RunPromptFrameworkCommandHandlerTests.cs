using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>
/// Verifies <see cref="RunPromptFrameworkCommandHandler"/> substitutes variables before calling
/// the AI port, increments the usage counter, and writes an audit entry (closes DEFECT-LOG.md
/// SEC-02 for the previously-unauthenticated <c>POST /prompts/:id/run</c>).
/// </summary>
public sealed class RunPromptFrameworkCommandHandlerTests
{
    private const int ActorUserId = 41;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IPromptExecutionEngine _executionEngine = Substitute.For<IPromptExecutionEngine>();
    private readonly RunPromptFrameworkCommandHandler _handler;

    public RunPromptFrameworkCommandHandlerTests()
    {
        _handler = new RunPromptFrameworkCommandHandler(_dbContext, _queryExecutor, _auditLogService, _executionEngine);
    }

    [Fact]
    public async Task Handle_ExistingFramework_SubstitutesVariablesAndIncrementsUsageCount()
    {
        var framework = new PromptFramework { Id = 6, NameAr = "n", PromptText = "مرحبا {{name}}", UsageCount = 2 };
        _dbContext.PromptFrameworks.Returns(new[] { framework }.AsQueryable());
        _executionEngine.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("ناتج الذكاء الاصطناعي");
        var variables = new Dictionary<string, string> { ["name"] = "أحمد" };

        Result<RunPromptFrameworkResultDto> result = await _handler.Handle(new RunPromptFrameworkCommand(ActorUserId, 6, variables), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PromptSent.Should().Be("مرحبا أحمد");
        result.Value.Output.Should().Be("ناتج الذكاء الاصطناعي");
        framework.UsageCount.Should().Be(3);
        await _executionEngine.Received(1).ExecuteAsync("مرحبا أحمد", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingFramework_ReturnsFailureAndDoesNotCallAiPort()
    {
        _dbContext.PromptFrameworks.Returns(Array.Empty<PromptFramework>().AsQueryable());

        Result<RunPromptFrameworkResultDto> result = await _handler.Handle(
            new RunPromptFrameworkCommand(ActorUserId, 404, new Dictionary<string, string>()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _executionEngine.DidNotReceive().ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SuccessfulRun_WritesAuditEntry()
    {
        var framework = new PromptFramework { Id = 6, NameAr = "n", PromptText = "نص" };
        _dbContext.PromptFrameworks.Returns(new[] { framework }.AsQueryable());
        _executionEngine.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("ناتج");

        await _handler.Handle(new RunPromptFrameworkCommand(ActorUserId, 6, new Dictionary<string, string>()), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "prompt_framework.run", "PromptFramework", "6", Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
