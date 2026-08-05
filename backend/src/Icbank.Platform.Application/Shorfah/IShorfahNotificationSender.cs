namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for dispatching a Shorfah in-app + email notification (BUSINESS-RULES.md §1.7). The Node
/// source called Resend (via <c>sendNotification()</c> in <c>lib/notify.ts</c>) for the email leg
/// and always wrote the in-app row regardless of email outcome. This port's default
/// implementation preserves that same "in-app write always happens, email is best-effort"
/// contract -- see <c>NullShorfahEmailSender</c> for the deferred real dispatch.
/// </summary>
public interface IShorfahNotificationSender
{
    /// <summary>Attempts to send the email leg of a notification. The in-app row is always persisted by the caller regardless of this result.</summary>
    /// <param name="recipientEmail">The recipient's email address, or <c>null</c> if the user has none on file.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlBody">The fully HTML-encoded email body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the email was actually dispatched.</returns>
    Task<bool> SendEmailAsync(string? recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
