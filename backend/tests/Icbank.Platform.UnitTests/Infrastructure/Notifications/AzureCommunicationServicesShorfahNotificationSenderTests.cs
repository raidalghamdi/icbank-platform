using Azure;
using Azure.Communication.Email;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Infrastructure.Notifications;

/// <summary>
/// Verifies <see cref="AzureCommunicationServicesShorfahNotificationSender"/>'s Polly retry
/// policy against a substituted <see cref="EmailClient"/>, mirroring
/// <see cref="AzureCommunicationServicesReportEmailSenderTests"/> for the Shorfah notification
/// port -- both ports share the identical retry policy shape but are separate classes, so each
/// needs its own coverage rather than assuming the other's tests exercise it.
/// </summary>
public sealed class AzureCommunicationServicesShorfahNotificationSenderTests
{
    private readonly EmailClient _emailClient = Substitute.For<EmailClient>();
    private readonly AzureCommunicationServicesShorfahNotificationSender _sender;

    public AzureCommunicationServicesShorfahNotificationSenderTests()
    {
        IOptions<AzureCommunicationServicesOptions> options = Options.Create(new AzureCommunicationServicesOptions
        {
            Endpoint = "https://icbank-test-acs.communication.azure.com",
            SenderAddress = "DoNotReply@icbank.example",
        });
        _sender = new AzureCommunicationServicesShorfahNotificationSender(_emailClient, options);
    }

    [Fact]
    public async Task SendEmailAsync_TransientFailureThenSuccess_RetriesAndReturnsTrue()
    {
        var callCount = 0;
        Task<EmailSendOperation> Send(NSubstitute.Core.CallInfo call)
        {
            callCount++;
            return callCount == 1
                ? throw new RequestFailedException(status: 429, message: "throttled")
                : Task.FromResult(SucceededOperation());
        }

        _emailClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Send(callInfo));

        var result = await _sender.SendEmailAsync("reader@icbank.example", "subject", "<p>body</p>", CancellationToken.None);

        result.Should().BeTrue();
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task SendEmailAsync_PersistentTransientFailure_GivesUpAndReturnsFalseWithoutThrowing()
    {
        Task<EmailSendOperation> Send(NSubstitute.Core.CallInfo call) =>
            throw new RequestFailedException(status: 500, message: "down");

        _emailClient.SendAsync(WaitUntil.Completed, Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Send(callInfo));

        var result = false;
        Func<Task> act = async () => result = await _sender.SendEmailAsync("reader@icbank.example", "subject", "<p>body</p>", CancellationToken.None);

        await act.Should().NotThrowAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_NullRecipient_ReturnsFalseWithoutCallingTheClient()
    {
        var result = await _sender.SendEmailAsync(null, "subject", "<p>body</p>", CancellationToken.None);

        result.Should().BeFalse();
        await _emailClient.DidNotReceive().SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_BlankRecipient_ReturnsFalseWithoutCallingTheClient()
    {
        var result = await _sender.SendEmailAsync("   ", "subject", "<p>body</p>", CancellationToken.None);

        result.Should().BeFalse();
        await _emailClient.DidNotReceive().SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    private static EmailSendOperation SucceededOperation()
    {
        EmailSendOperation op = Substitute.For<EmailSendOperation>();
        op.Value.Returns(EmailModelFactory.EmailSendResult("op-1", EmailSendStatus.Succeeded));
        return op;
    }
}
