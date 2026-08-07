namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for emailing a report's rendered HTML to a recipient list
/// (<c>POST /final-media-reports/:id/send-email</c>). The Node source used Resend and silently
/// no-op'd (<c>sent:false</c>) when no API key was configured, rather than erroring -- this port
/// preserves that honest-no-op contract via <see cref="ReportEmailResult.Sent"/> rather than
/// throwing. Wave 3a ships a placeholder that never actually dispatches email; wiring a real
/// provider is deferred, see WAVE3A-PORT-NOTES.md.
/// </summary>
public interface IReportEmailSender
{
    /// <summary>Attempts to send the given HTML document to the given recipients.</summary>
    /// <param name="recipients">The recipient email addresses.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlBody">The fully HTML-encoded report document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The send result.</returns>
    Task<ReportEmailResult> SendAsync(IReadOnlyList<string> recipients, string subject, string htmlBody, CancellationToken cancellationToken);
}

/// <summary>Result of a <see cref="IReportEmailSender.SendAsync"/> call.</summary>
/// <param name="Sent">Whether the email was actually dispatched (false when no provider is configured -- an honest no-op, not a fabricated success).</param>
/// <param name="ProviderMessage">A human-readable status message from the provider (or the reason no send occurred).</param>
public sealed record ReportEmailResult(bool Sent, string ProviderMessage);
