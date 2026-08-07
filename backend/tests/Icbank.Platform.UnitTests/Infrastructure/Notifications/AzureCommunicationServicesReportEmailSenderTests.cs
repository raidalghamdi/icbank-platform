using Azure;
using Azure.Communication.Email;
using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Infrastructure.Notifications;

/// <summary>
/// Verifies <see cref="AzureCommunicationServicesReportEmailSender"/>'s Polly retry policy
/// actually retries transient (5xx/429) <see cref="RequestFailedException"/> failures and gives
/// up cleanly (reporting <c>Sent: false</c>, never throwing) once attempts are exhausted, against
/// a substituted <see cref="EmailClient"/> -- no live Azure Communication Services resource.
/// </summary>
public sealed class AzureCommunicationServicesReportEmailSenderTests
{
    private static readonly IReadOnlyList<string> Recipients = new[] { "ops@icbank.example" };

    private readonly EmailClient _emailClient = Substitute.For<EmailClient>();
    private readonly AzureCommunicationServicesReportEmailSender _sender;

    public AzureCommunicationServicesReportEmailSenderTests()
    {
        IOptions<AzureCommunicationServicesOptions> options = Options.Create(new AzureCommunicationServicesOptions
        {
            Endpoint = "https://icbank-test-acs.communication.azure.com",
            SenderAddress = "DoNotReply@icbank.example",
        });
        _sender = new AzureCommunicationServicesReportEmailSender(_emailClient, options);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(429)]
    public async Task SendAsync_TransientFailureThenSuccess_RetriesAndEventuallySucceeds(int transientStatus)
    {
        var callCount = 0;
        Task<EmailSendOperation> Send(NSubstitute.Core.CallInfo call)
        {
            callCount++;
            return callCount == 1
                ? throw new RequestFailedException(status: transientStatus, message: "transient")
                : Task.FromResult(SucceededOperation());
        }

        _emailClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Send(callInfo));

        ReportEmailResult result = await _sender.SendAsync(Recipients, "subject", "<p>body</p>", CancellationToken.None);

        result.Sent.Should().BeTrue();
        callCount.Should().Be(2, "the first (5xx/429) failure must be retried, not surfaced immediately");
    }

    [Fact]
    public async Task SendAsync_NonTransientFailure_DoesNotRetryAndReturnsNotSent()
    {
        // Why: ShouldHandle only matches Status is >= 500 or 429 -- a 400 (bad request, e.g. an
        // invalid sender/recipient address) is permanent and retrying it would just waste time
        // and delay an honest failure the caller needs to see.
        var callCount = 0;
        Task<EmailSendOperation> Send(NSubstitute.Core.CallInfo call)
        {
            callCount++;
            throw new RequestFailedException(status: 400, message: "bad request");
        }

        _emailClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Send(callInfo));

        ReportEmailResult result = await _sender.SendAsync(Recipients, "subject", "<p>body</p>", CancellationToken.None);

        result.Sent.Should().BeFalse();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_PersistentTransientFailure_GivesUpAfterMaxAttemptsAndReturnsNotSent()
    {
        var callCount = 0;
        Task<EmailSendOperation> Send(NSubstitute.Core.CallInfo call)
        {
            callCount++;
            throw new RequestFailedException(status: 503, message: "still down");
        }

        _emailClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Send(callInfo));

        ReportEmailResult result = await _sender.SendAsync(Recipients, "subject", "<p>body</p>", CancellationToken.None);

        result.Sent.Should().BeFalse();
        result.ProviderMessage.Should().Contain("still down");

        // Why: MaxRetryAttempts = 3 means 1 initial attempt + 3 retries = 4 calls total. Asserting
        // the exact count is what proves the policy gives up instead of retrying forever.
        callCount.Should().Be(4);
    }

    [Fact]
    public async Task SendAsync_FirstAttemptSucceeds_DoesNotRetry()
    {
        var callCount = 0;
        Task<EmailSendOperation> Send(NSubstitute.Core.CallInfo call)
        {
            callCount++;
            return Task.FromResult(SucceededOperation());
        }

        _emailClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Send(callInfo));

        ReportEmailResult result = await _sender.SendAsync(Recipients, "subject", "<p>body</p>", CancellationToken.None);

        result.Sent.Should().BeTrue();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_NoRecipients_ReturnsNotSentWithoutCallingTheClient()
    {
        ReportEmailResult result = await _sender.SendAsync(Array.Empty<string>(), "subject", "<p>body</p>", CancellationToken.None);

        result.Sent.Should().BeFalse();
        await _emailClient.DidNotReceive().SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    private static EmailSendOperation SucceededOperation()
    {
        EmailSendOperation op = Substitute.For<EmailSendOperation>();
        op.Value.Returns(EmailModelFactory.EmailSendResult("op-1", EmailSendStatus.Succeeded));
        return op;
    }
}
