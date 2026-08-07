using System.Net;

namespace Icbank.Platform.UnitTests.Infrastructure.News;

/// <summary>
/// A canned-response <see cref="HttpMessageHandler"/> for testing the news providers without a
/// network call, and for asserting the exact URL each provider builds.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;
    private readonly Exception? _throwOnSend;

    /// <summary>Initializes a new instance of the <see cref="StubHttpMessageHandler"/> class.</summary>
    /// <param name="statusCode">The status code to return.</param>
    /// <param name="body">The response body to return.</param>
    /// <param name="throwOnSend">When set, thrown instead of responding, to simulate a dead upstream.</param>
    public StubHttpMessageHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK, string body = "", Exception? throwOnSend = null)
    {
        _statusCode = statusCode;
        _body = body;
        _throwOnSend = throwOnSend;
    }

    /// <summary>
    /// Gets the URL of the last request that reached this handler, percent-encoded as it goes on the
    /// wire. This deliberately reads <see cref="Uri.AbsoluteUri"/> rather than
    /// <see cref="Uri.ToString"/>, because the latter decodes the escapes and so would not show what
    /// the provider actually transmits for an Arabic search term.
    /// </summary>
    public string? LastRequestUri { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri?.AbsoluteUri;

        if (_throwOnSend is not null)
        {
            return Task.FromException<HttpResponseMessage>(_throwOnSend);
        }

        return Task.FromResult(new HttpResponseMessage(_statusCode) { Content = new StringContent(_body) });
    }
}
