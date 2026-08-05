using System.Security.Claims;
using Icbank.Platform.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Infrastructure implementation of <see cref="ICurrentUserService"/> that resolves the caller's
/// identity from the ambient <see cref="HttpContext"/>. Falls back to a system marker for
/// background jobs or unauthenticated requests, so the audit interceptor always has a value to
/// write (R-BE-022).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private const string SystemUserId = "system";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of the <see cref="CurrentUserService"/> class.</summary>
    /// <param name="httpContextAccessor">Accessor for the current request's <see cref="HttpContext"/>.</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? SystemUserId;
}
