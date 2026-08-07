using Azure;
using Azure.Communication.Email;
using Icbank.Platform.Application.MediaMonitoring;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Icbank.Platform.Infrastructure.Notifications;

/// <summary>
/// <see cref="IReportEmailSender"/> implementation backed by Azure Communication Services Email.
/// Selected when <c>Notifications:Provider</c> is <c>AzureCommunicationServices</c>; the existing
/// <c>NullReportEmailSender</c> honest no-op remains the default so local development and the
/// test suite need no cloud dependency. Wraps every send attempt in the same standard
/// retry-with-exponential-backoff policy used elsewhere for outbound calls (R-BE-095), because a
/// transient ACS throttling/network failure must not surface as a permanent "not sent" to the
/// end user.
/// </summary>
public sealed class AzureCommunicationServicesReportEmailSender : IReportEmailSender
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

    /// <summary>Initializes a new instance of the <see cref="AzureCommunicationServicesReportEmailSender"/> class.</summary>
    /// <param name="emailClient">The managed-identity-authenticated ACS Email client.</param>
    /// <param name="options">The bound ACS configuration.</param>
    public AzureCommunicationServicesReportEmailSender(EmailClient emailClient, IOptions<AzureCommunicationServicesOptions> options)
    {
        _emailClient = emailClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<ReportEmailResult> SendAsync(IReadOnlyList<string> recipients, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (recipients.Count == 0)
        {
            return new ReportEmailResult(Sent: false, ProviderMessage: "No recipients were supplied.");
        }

        var message = new EmailMessage(
            _options.SenderAddress,
            new EmailRecipients(recipients.Select(address => new EmailAddress(address))),
            new EmailContent(subject) { Html = htmlBody });

        try
        {
            EmailSendOperation operation = await RetryPipeline.ExecuteAsync(
                async token => await _emailClient.SendAsync(WaitUntil.Completed, message, token),
                cancellationToken);

            return new ReportEmailResult(
                Sent: operation.Value.Status == EmailSendStatus.Succeeded,
                ProviderMessage: $"Azure Communication Services: {operation.Value.Status}");
        }
        catch (RequestFailedException ex)
        {
            // Why: an ACS send failure is reported back as an honest "not sent" result, matching
            // the port's existing no-throw contract -- callers already handle Sent:false, and a
            // thrown exception here would be a behavioural change none of them expect.
            return new ReportEmailResult(Sent: false, ProviderMessage: $"Azure Communication Services error {ex.ErrorCode}: {ex.Message}");
        }
    }
}
