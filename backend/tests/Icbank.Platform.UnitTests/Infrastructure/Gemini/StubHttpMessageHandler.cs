using System.Net;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// An in-memory <see cref="HttpMessageHandler"/> double that never touches a real socket — used
/// only to verify <see cref="Icbank.Platform.Infrastructure.Gemini.HttpGeminiTransport"/>'s JSON
/// parsing against literal response bodies shaped like the real <c>v1beta generateContent</c> API.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    /// <summary>Initializes a new instance of the <see cref="StubHttpMessageHandler"/> class.</summary>
    /// <param name="responseBody">The literal JSON body to return.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    public StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    /// <summary>Gets the most recently sent request's captured body, for assertions on request construction.</summary>
    public string? LastRequestBody { get; private set; }

    /// <summary>Gets the most recently sent request, for header/URL assertions.</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new HttpResponseMessage(_statusCode) { Content = new StringContent(_responseBody) };
    }
}
