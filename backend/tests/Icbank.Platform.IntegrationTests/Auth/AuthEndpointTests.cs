using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Icbank.Platform.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
    public async Task MustChangePassword_TokenIsRefusedByProtectedEndpointUntilPasswordChanges()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        seeded.SuperAdmin.MustChangePassword = true;
        await dbContext.SaveChangesAsync();
        using HttpClient client = _factory.CreateClient();

        var temporaryToken = await LoginAndGetAccessTokenAsync(client, seeded.SuperAdmin.Email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", temporaryToken);

        HttpResponseMessage blockedDashboard = await client.GetAsync(new Uri("/api/v1/dashboard/summary", UriKind.Relative));
        blockedDashboard.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await blockedDashboard.Content.ReadAsStringAsync()).Should().Contain("must_change_password");

        HttpResponseMessage profile = await client.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative));
        profile.StatusCode.Should().Be(HttpStatusCode.OK, "the forced-password flow needs the profile endpoint");

        HttpResponseMessage blockedLogout = await client.PostAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), content: null);
        blockedLogout.StatusCode.Should().Be(HttpStatusCode.Forbidden, "only /auth/me and /auth/change-password may bypass the temporary-password gate");

        const string replacementPassword = "NewStr0ng!Passw0rd#2026";
        HttpResponseMessage change = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/change-password", UriKind.Relative),
            new { currentPassword = SharedPassword, newPassword = replacementPassword });
        change.StatusCode.Should().Be(HttpStatusCode.OK);

        // The temporary token remains deliberately restricted; only a newly issued token can
        // access application data after the server has observed the completed password change.
        HttpResponseMessage stillBlocked = await client.GetAsync(new Uri("/api/v1/dashboard/summary", UriKind.Relative));
        stillBlocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        client.DefaultRequestHeaders.Authorization = null;
        var unrestrictedToken = await LoginAndGetAccessTokenAsync(client, seeded.SuperAdmin.Email, replacementPassword);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", unrestrictedToken);
        HttpResponseMessage dashboard = await client.GetAsync(new Uri("/api/v1/dashboard/summary", UriKind.Relative));
        dashboard.StatusCode.Should().Be(HttpStatusCode.OK);

        using AppDbContext assertionDbContext = CreateDbContext();
        User changedUser = await assertionDbContext.Users.SingleAsync(user => user.Id == seeded.SuperAdmin.Id);
        changedUser.MustChangePassword.Should().BeFalse();
        (await assertionDbContext.AuditLogEntries.Where(entry => entry.Action == "user.password.change").ToListAsync())
            .Should().ContainSingle(entry => entry.ActorUserId == seeded.SuperAdmin.Id);
    }

    [Fact]
    public async Task LoginRateLimit_AllowsTwentyAttemptsThenRejectsTheTwentyFirst()
    {
        using HttpClient client = _factory.CreateClient();

        for (var attempt = 0; attempt < 20; attempt++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(
                new Uri("/api/v1/auth/login", UriKind.Relative),
                new { email = "unknown-rate-limit@test.local", password = "wrong-password" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        HttpResponseMessage rejected = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { email = "unknown-rate-limit@test.local", password = "wrong-password" });
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task SeededAdministrator_ReconcilesSuperAdminRoleAndPassesSuperAdminPolicy()
    {
        using AppDbContext dbContext = CreateDbContext();
        Role adminRole = new() { Name = "admin", NameAr = "مدير", CreatedBy = "test" };
        Role superAdminRole = new() { Name = "super_admin", NameAr = "مدير النظام", CreatedBy = "test" };
        User configuredAdministrator = new()
        {
            Email = "ccteam234@gmail.com",
            Name = "Configured administrator",
            PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(null!, SharedPassword),
            IsActive = true,
            CreatedBy = "test",
        };
        dbContext.AddRange(adminRole, superAdminRole, configuredAdministrator);
        await dbContext.SaveChangesAsync();
        dbContext.UserRoles.Add(new UserRole { UserId = configuredAdministrator.Id, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync();

        var seeder = new DatabaseSeeder(
            dbContext,
            _factory.Services.GetRequiredService<IHostEnvironment>(),
            Options.Create(new SeedOptions { InitialSuperAdminEmail = configuredAdministrator.Email }),
            NullLogger<DatabaseSeeder>.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        List<string> assignedRoles = await dbContext.UserRoles
            .Where(assignment => assignment.UserId == configuredAdministrator.Id)
            .Join(dbContext.Roles, assignment => assignment.RoleId, role => role.Id, (_, role) => role.Name)
            .ToListAsync();
        assignedRoles.Should().Contain("admin");
        assignedRoles.Should().Contain("super_admin");

        using HttpClient client = _factory.CreateClient();
        var temporaryToken = await LoginAndGetAccessTokenAsync(client, configuredAdministrator.Email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", temporaryToken);
        HttpResponseMessage change = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/change-password", UriKind.Relative),
            new { currentPassword = SharedPassword, newPassword = "SeededStr0ng!Passw0rd#2026" });
        change.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = null;
        var superAdminToken = await LoginAndGetAccessTokenAsync(client, configuredAdministrator.Email, "SeededStr0ng!Passw0rd#2026");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", superAdminToken);
        HttpResponseMessage superAdminRoute = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/roles", UriKind.Relative),
            new { name = "seeded_super_admin_proof", nameAr = "إثبات المدير" });
        superAdminRoute.StatusCode.Should().Be(HttpStatusCode.Created, "the real super-admin policy handler must accept the reconciled seeded account");
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

        // Account lockout remains independent from the more generous request limiter: after five
        // incorrect passwords the persisted account state must be locked even though a normal
        // browser burst still has additional request capacity.
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

    private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email, string? password = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { email, password = password ?? SharedPassword });
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
