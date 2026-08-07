using Icbank.Platform.Application.Shorfah;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Deterministic, non-dispatching default <see cref="IShorfahNotificationSender"/>
/// implementation. The Node source called Resend via <c>sendNotification()</c>; wiring a real
/// transactional-email provider is deferred for Wave 4a (see WAVE4A-PORT-NOTES.md), following the
/// exact same deferral pattern as Wave 3a's <c>NullReportEmailSender</c>. This implementation
/// never actually dispatches email, so every downstream concern (in-app notification
/// persistence, authorization, audit logging, SLA-clock stamping) is fully exercisable
/// end-to-end without incurring real email-provider cost or credentials.
/// </summary>
public sealed class NullShorfahNotificationSender : IShorfahNotificationSender
{
    /// <inheritdoc />
    public Task<bool> SendEmailAsync(string? recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
