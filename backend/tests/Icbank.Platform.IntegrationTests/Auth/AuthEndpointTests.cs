using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// End-to-end proof of the auth/RBAC pipeline (task requirement 8): unauthenticated requests are
/// rejected, a viewer cannot reach an admin endpoint, an admin cannot escalate to super_admin
/// (SEC-01 regression), an invalid SSO redirect target is rejected, lockout triggers, refresh
/// rotation invalidates the prior token, and the audit log is written on privileged actions.
/// </summary>
public sealed class AuthEndpointTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    // Why: each test method gets its own factory instance (own InMemory database, own rate
    // limiter state) so tests never see each other's seeded users or trip each other's
    // login rate limit — sharing one factory across the whole class caused both problems.
    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetAdminUsers_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/users", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdminUsers_ViewerToken_ReturnsForbidden()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        using HttpClient client = _factory.CreateClient();

        var accessToken = await LoginAndGetAccessTokenAsync(client, seeded.Viewer.Email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/users", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_PlainAdminAttemptingSuperAdminGrant_ReturnsForbidden()
    {
        // Why: this is the explicit SEC-01 regression test the task requires — an admin token
        // must be rejected by the authorization policy before the handler's own defense-in-depth
        // check ever runs.
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        using HttpClient client = _factory.CreateClient();

        var accessToken = await LoginAndGetAccessTokenAsync(client, seeded.Admin.Email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/admin/users/{seeded.Viewer.Id}/roles", UriKind.Relative),
            new { roleId = seeded.SuperAdminRoleId });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_SuperAdminGrantingSuperAdmin_Succeeds()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        using HttpClient client = _factory.CreateClient();

        var accessToken = await LoginAndGetAccessTokenAsync(client, seeded.SuperAdmin.Email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/admin/users/{seeded.Viewer.Id}/roles", UriKind.Relative),
            new { roleId = seeded.SuperAdminRoleId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_FiveConsecutiveFailures_LocksAccount()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        using HttpClient client = _factory.CreateClient();
        const int lockoutThreshold = 5;

        for (var attempt = 0; attempt < lockoutThreshold; attempt++)
        {
            await client.PostAsJsonAsync(
                new Uri("/api/v1/auth/login", UriKind.Relative),
                new { email = seeded.Viewer.Email, password = "wrong-password" });
        }

        // Why: the login rate limiter (5/min/IP, DOTNET-CONVENTIONS.md §3.13) already caps this
        // test's client at exactly the lockout threshold's worth of requests, so the lockout
        // itself is asserted directly against persisted state rather than via a 6th HTTP call
        // that would otherwise be indistinguishable from a 429 rate-limit rejection.
        using AppDbContext assertionDbContext = CreateDbContext();
        User? lockedUser = await assertionDbContext.Users.SingleAsync(u => u.Id == seeded.Viewer.Id);

        lockedUser.IsLocked.Should().BeTrue();
        lockedUser.FailedAttempts.Should().Be(lockoutThreshold);
    }

    [Fact]
    public async Task Refresh_AfterRotation_PriorRefreshTokenNoLongerWorks()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        using HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { email = seeded.Viewer.Email, password = SharedPassword });
        var priorCookie = ExtractRefreshCookie(loginResponse);

        using var firstRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        firstRefreshRequest.Headers.Add("Cookie", priorCookie);
        HttpResponseMessage firstRefreshResponse = await client.SendAsync(firstRefreshRequest);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var secondRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        secondRefreshRequest.Headers.Add("Cookie", priorCookie);
        HttpResponseMessage secondRefreshResponse = await client.SendAsync(secondRefreshRequest);

        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SsoStart_InvalidRedirectTarget_FallsBackToDefaultNotAttackerTarget()
    {
        using AppDbContext dbContext = CreateDbContext();
        await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        using HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync(new Uri(
            "/api/v1/auth/sso/azure/start?redirect=https://evil.example.com/steal", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location?.ToString() ?? string.Empty;

        // Why: closes SEC-11 — the attacker-controlled redirect target must never appear
        // anywhere in the authorization URL Azure AD is instructed to eventually return to.
        location.Should().NotContain("evil.example.com");
    }

    [Fact]
    public async Task AssignRole_SuperAdminGrantingRole_WritesAuditLogEntry()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        using HttpClient client = _factory.CreateClient();

        var accessToken = await LoginAndGetAccessTokenAsync(client, seeded.SuperAdmin.Email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/admin/users/{seeded.Viewer.Id}/roles", UriKind.Relative),
            new { roleId = seeded.SuperAdminRoleId });
        response.EnsureSuccessStatusCode();

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries
            .Where(e => e.Action == "user.role.assign" && e.TargetId == seeded.Viewer.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToListAsync();

        entries.Should().ContainSingle();
        entries[0].ActorUserId.Should().Be(seeded.SuperAdmin.Id);
        entries[0].CorrelationId.Should().NotBeNullOrEmpty();
    }

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        var setCookieHeader = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values)
            ? values.First(v => v.StartsWith("refresh_token=", StringComparison.Ordinal))
            : throw new InvalidOperationException("Login response did not set a refresh_token cookie.");

        return setCookieHeader.Split(';')[0];
    }

    private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { email, password = SharedPassword });
        response.EnsureSuccessStatusCode();

        LoginResponseBody? body = await response.Content.ReadFromJsonAsync<LoginResponseBody>();
        return body?.AccessToken ?? throw new InvalidOperationException("Login response missing accessToken.");
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private sealed record LoginResponseBody(string AccessToken);
}
