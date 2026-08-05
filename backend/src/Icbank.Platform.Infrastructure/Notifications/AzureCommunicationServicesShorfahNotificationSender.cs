using Azure;
using Azure.Communication.Email;
using Icbank.Platform.Application.Shorfah;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Icbank.Platform.Infrastructure.Notifications;

/// <summary>
/// <see cref="IShorfahNotificationSender"/> implementation backed by Azure Communication Services
/// Email. Selected when <c>Notifications:Provider</c> is <c>AzureCommunicationServices</c>; the
/// existing <c>NullShorfahNotificationSender</c> remains the default so the in-app notification
/// row (always written by the caller regardless of this port's result, per BUSINESS-RULES.md
/// §1.7) keeps working with no cloud dependency in local development and the test suite.
/// </summary>
public sealed class AzureCommunicationServicesShorfahNotificationSender : IShorfahNotificationSender
{
    private const int MaxRetryAttempts = 3;
    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex => ex.Status is >= 500 or 429),
        })
        .Build();

    private readonly EmailClient _emailClient;
    private readonly AzureCommunicationServicesOptions _options;

    /// <summary>Initializes a new instance of the <see cref="AzureCommunicationServicesShorfahNotificationSender"/> class.</summary>
    /// <param name="emailClient">The managed-identity-authenticated ACS Email client.</param>
    /// <param name="options">The bound ACS configuration.</param>
    public AzureCommunicationServicesShorfahNotificationSender(EmailClient emailClient, IOptions<AzureCommunicationServicesOptions> options)
    {
        _emailClient = emailClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(string? recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return false;
        }

        var message = new EmailMessage(
            _options.SenderAddress,
            new EmailRecipients(new[] { new EmailAddress(recipientEmail) }),
            new EmailContent(subject) { Html = htmlBody });

        try
        {
            EmailSendOperation operation = await RetryPipeline.ExecuteAsync(
                async token => await _emailClient.SendAsync(WaitUntil.Completed, message, token),
                cancellationToken);

            return operation.Value.Status == EmailSendStatus.Succeeded;
        }
        catch (RequestFailedException)
        {
            // Why: matches this port's existing "best-effort, never throws" contract -- the
            // in-app notification row is always written by the caller regardless of this result.
            return false;
        }
    }
}
