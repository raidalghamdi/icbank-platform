using System.Security.Claims;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Identity;

/// <summary>
/// Verifies <see cref="CurrentUserService"/> resolves the acting identity correctly in every case
/// the audit trail (R-BE-022) depends on: an authenticated request, an anonymous/background
/// context, and the complete absence of an <see cref="HttpContext"/> (e.g. a hosted service).
/// </summary>
public sealed class CurrentUserServiceTests
{
    [Fact]
    public void UserId_AuthenticatedRequestWithNameIdentifierClaim_ReturnsClaimValue()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42")], "TestAuth")),
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new CurrentUserService(accessor);

        sut.UserId.Should().Be("42");
    }

    [Fact]
    public void UserId_AuthenticatedPrincipalWithoutNameIdentifierClaim_FallsBackToSystemMarker()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "someone")], "TestAuth")),
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new CurrentUserService(accessor);

        sut.UserId.Should().Be("system");
    }

    [Fact]
    public void UserId_NoHttpContext_FallsBackToSystemMarker()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var sut = new CurrentUserService(accessor);

        sut.UserId.Should().Be("system");
    }
}
