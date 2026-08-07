using System.Diagnostics;

namespace Icbank.Platform.Infrastructure.Http;

/// <summary>
/// Outbound <see cref="DelegatingHandler"/> that copies the current W3C trace id onto every
/// downstream HTTP call as <c>X-Correlation-Id</c> (R-BE-052), so distributed traces stay
/// joinable end-to-end.
/// </summary>
public sealed class CorrelationPropagationHandler : DelegatingHandler
{
    private const string CorrelationHeaderName = "X-Correlation-Id";

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (traceId is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, traceId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
