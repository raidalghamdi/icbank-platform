using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>
/// Verifies <see cref="LogWizardQaQueryCommandHandler"/> persists a <see cref="ReportsQaQuery"/>
/// row stamped with the authenticated caller's id (closes SEC-02: the Node source trusted a
/// client-supplied identity for this write instead of the authenticated session).
/// </summary>
public sealed class LogWizardQaQueryCommandHandlerTests
{
    private const int ActorUserId = 11;

    private static readonly string[] ExpectedSources = { "أخبار", "لينكدإن" };

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly LogWizardQaQueryCommandHandler _handler;

    public LogWizardQaQueryCommandHandlerTests()
    {
        _handler = new LogWizardQaQueryCommandHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_ValidWizardAnswers_PersistsWizardQueryStampedWithActor()
    {
        var command = new LogWizardQaQueryCommand(
            ActorUserId, "أسبوعي", "تنفيذي", new List<string> { "أخبار", "لينكدإن" }, "منافسة", "ar", "ceo@example.com", "generate");
        ReportsQaQuery? persisted = null;
        _dbContext.When(context => context.Add(Arg.Any<ReportsQaQuery>())).Do(callInfo => persisted = callInfo.Arg<ReportsQaQuery>());

        Result<int> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(ActorUserId);
        persisted.QueryType.Should().Be(QaQueryType.Wizard);
        persisted.WizardAnswers.Should().NotBeNull();
        persisted.WizardAnswers!.Period.Should().Be("أسبوعي");
        persisted.WizardAnswers.Audience.Should().Be("تنفيذي");
        persisted.WizardAnswers.Sources.Should().BeEquivalentTo(ExpectedSources);
        persisted.WizardAnswers.Mode.Should().Be("generate");
    }

    [Fact]
    public async Task Handle_NullOptionalFields_PersistsEmptySourcesListNotNull()
    {
        var command = new LogWizardQaQueryCommand(ActorUserId, null, null, null, null, null, null, null);
        ReportsQaQuery? persisted = null;
        _dbContext.When(context => context.Add(Arg.Any<ReportsQaQuery>())).Do(callInfo => persisted = callInfo.Arg<ReportsQaQuery>());

        Result<int> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persisted!.WizardAnswers!.Sources.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AlwaysCallsSaveChanges()
    {
        var command = new LogWizardQaQueryCommand(ActorUserId, "شهري", null, null, null, null, null, "search");

        await _handler.Handle(command, CancellationToken.None);

        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
