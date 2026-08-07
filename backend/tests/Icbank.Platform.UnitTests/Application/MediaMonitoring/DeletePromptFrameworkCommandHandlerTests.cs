using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="DeletePromptFrameworkCommandHandler"/> hard-deletes and writes an audit entry on success, and fails cleanly on a missing id.</summary>
public sealed class DeletePromptFrameworkCommandHandlerTests
{
    private const int ActorUserId = 31;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly DeletePromptFrameworkCommandHandler _handler;

    public DeletePromptFrameworkCommandHandlerTests()
    {
        _handler = new DeletePromptFrameworkCommandHandler(_dbContext, _queryExecutor, _auditLogService);
    }

    [Fact]
    public async Task Handle_ExistingFramework_RemovesAndWritesAuditEntry()
    {
        var framework = new PromptFramework { Id = 8, NameAr = "n", PromptText = "p" };
        _dbContext.PromptFrameworks.Returns(new[] { framework }.AsQueryable());

        Result<bool> result = await _handler.Handle(new DeletePromptFrameworkCommand(ActorUserId, 8), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.Received(1).Remove(framework);
        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "prompt_framework.delete", "PromptFramework", "8", Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingFramework_ReturnsFailure()
    {
        _dbContext.PromptFrameworks.Returns(Array.Empty<PromptFramework>().AsQueryable());

        Result<bool> result = await _handler.Handle(new DeletePromptFrameworkCommand(ActorUserId, 404), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("القالب غير موجود");
    }
}
