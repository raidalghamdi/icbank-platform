using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Shorfah;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>
/// Proves the Shorfah issue state machine (BUSINESS-RULES.md §1.1) enforces exactly the legal
/// transitions and rejects everything else -- the task's explicit instruction to "enforce illegal
/// transitions with a clear error rather than silently allowing them".
/// </summary>
public sealed class ShorfahIssueStateMachineTests
{
    [Theory]
    [InlineData(ShorfahIssueStatus.Collecting, true)]
    [InlineData(ShorfahIssueStatus.InReview, true)]
    [InlineData(ShorfahIssueStatus.Published, false)]
    public void CanStartReview_MatchesNodeSourceRule(ShorfahIssueStatus current, bool expected)
    {
        ShorfahIssueStateMachine.CanStartReview(current).Should().Be(expected, "shorfah.ts:248-258 blocks start-review only when already published");
    }

    [Theory]
    [InlineData(ShorfahIssueStatus.Collecting, ShorfahIssueStatus.InReview, true)]
    [InlineData(ShorfahIssueStatus.InReview, ShorfahIssueStatus.Published, true)]
    [InlineData(ShorfahIssueStatus.Collecting, ShorfahIssueStatus.Published, false)]
    [InlineData(ShorfahIssueStatus.Published, ShorfahIssueStatus.Collecting, false)]
    [InlineData(ShorfahIssueStatus.Published, ShorfahIssueStatus.InReview, false)]
    [InlineData(ShorfahIssueStatus.InReview, ShorfahIssueStatus.Collecting, false)]
    public void CanTransitionTo_OnlyAllowsForwardSingleSteps(ShorfahIssueStatus current, ShorfahIssueStatus target, bool expected)
    {
        ShorfahIssueStateMachine.CanTransitionTo(current, target).Should().Be(expected);
    }

    [Theory]
    [InlineData(ShorfahIssueStatus.Collecting)]
    [InlineData(ShorfahIssueStatus.InReview)]
    [InlineData(ShorfahIssueStatus.Published)]
    public void CanTransitionTo_SameStatus_IsAlwaysAllowedAsNoOp(ShorfahIssueStatus status)
    {
        ShorfahIssueStateMachine.CanTransitionTo(status, status).Should().BeTrue();
    }
}
