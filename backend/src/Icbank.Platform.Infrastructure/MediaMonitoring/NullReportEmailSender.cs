using Icbank.Platform.Application.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Deterministic, non-dispatching default <see cref="IReportEmailSender"/> implementation. The
/// Node source called Resend and silently no-op'd (<c>sent:false</c>) whenever
/// <c>RESEND_API_KEY</c> was unset, rather than throwing; wiring a real email provider is
/// deferred for Wave 3a (see WAVE3A-PORT-NOTES.md) -- this implementation preserves that exact
/// honest-no-op contract unconditionally, so the send-email endpoint is fully exercisable
/// end-to-end (persistence, authorization, audit log) without ever dispatching a real message or
/// incurring provider cost.
/// </summary>
public sealed class NullReportEmailSender : IReportEmailSender
{
    private const string NoProviderConfiguredMessage = "لم يتم إرسال البريد — مزوّد البريد الإلكتروني غير مُفعّل بعد.";

    /// <inheritdoc />
    public Task<ReportEmailResult> SendAsync(IReadOnlyList<string> recipients, string subject, string htmlBody, CancellationToken cancellationToken) =>
        Task.FromResult(new ReportEmailResult(Sent: false, ProviderMessage: NoProviderConfiguredMessage));
}
