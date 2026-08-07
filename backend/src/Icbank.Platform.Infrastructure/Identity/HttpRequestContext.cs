using System.Diagnostics;
using Icbank.Platform.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Resolves the current request's correlation id and caller IP from the ambient
/// <see cref="HttpContext"/>, mirroring the correlation-id middleware's own trace-id resolution
/// (DOTNET-CONVENTIONS.md §3.3) so audit rows and response headers always agree.
/// </summary>
public sealed class HttpRequestContext : IRequestContext
{
    private const string SystemCorrelationId = "system";
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of the <see cref="HttpRequestContext"/> class.</summary>
    /// <param name="httpContextAccessor">Accessor for the current request's <see cref="HttpContext"/>.</param>
    public HttpRequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string CorrelationId =>
        Activity.Current?.TraceId.ToString() ?? _httpContextAccessor.HttpContext?.TraceIdentifier ?? SystemCorrelationId;

    /// <inheritdoc />
    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
