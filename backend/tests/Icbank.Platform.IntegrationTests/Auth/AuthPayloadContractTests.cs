using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Regression tests for the legacy browser's auth payload contract. These deliberately inspect
/// JSON rather than deserialising to a DTO so object-vs-array and property-name regressions fail.
/// </summary>
public sealed class AuthPayloadContractTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private static readonly string[] AdminAndViewerRoles = { "admin", "viewer" };
    private static readonly string[] AllCrudVerbs = { "create", "delete", "edit", "view" };

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    /// <summary>
    /// The deployed frontend indexes permissions by page slug and compares one role string for
    /// admin-only UI. This test would have caught the flat permission-array response that hid the
    /// entire navigation for a fully permissioned admin.
    /// </summary>
    [Fact]
    public async Task CurrentUser_ReturnsLegacyCompatibleGroupedPermissionsAndRole()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        Role viewerRole = await dbContext.Roles.SingleAsync(role => role.Name == "viewer");
        dbContext.UserRoles.Add(new UserRole
        {
            UserId = seeded.Admin.Id,
            RoleId = viewerRole.Id,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        await dbContext.SaveChangesAsync();

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { email = seeded.Admin.Email, password = SharedPassword });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginDocument = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        AssertLegacyUserPayload(loginDocument.RootElement.GetProperty("user"));
        var accessToken = loginDocument.RootElement.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertLegacyUserPayload(document.RootElement);
    }

    /// <summary>
    /// The frontend's map names the exact object keys it will use against <c>permissions</c>.
    /// Pinning it to <see cref="PageSlugs.All"/> makes an accidental API/page-slug rename fail in
    /// CI before a browser receives a permission object that it cannot read.
    /// </summary>
    [Fact]
    public void FrontendPermissionMap_OnlyTargetsSeededApiPageSlugs()
    {
        var frontend = File.ReadAllText(FindFrontendArtifactPath());
        Match map = Regex.Match(
            frontend,
            @"var\s+PAGE_PERM_MAP\s*=\s*\{(?<entries>.*?)\};",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        map.Success.Should().BeTrue("the auth contract test must inspect the actual frontend permission map");

        IEnumerable<string> frontendPageSlugs = Regex.Matches(
                map.Groups["entries"].Value,
                @"'[^']+'\s*:\s*'(?<slug>[^']+)'",
                RegexOptions.CultureInvariant)
            .Select(match => match.Groups["slug"].Value)
            .Distinct(StringComparer.Ordinal);

        frontendPageSlugs.Should().NotBeEmpty();
        frontendPageSlugs.Should().BeSubsetOf(PageSlugs.All);
    }

    private static void AssertLegacyUserPayload(JsonElement payload)
    {
        payload.GetProperty("role").ValueKind.Should().Be(JsonValueKind.String);
        payload.GetProperty("role").GetString().Should().Be("admin");
        payload.GetProperty("roleNames").ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetProperty("roleNames").EnumerateArray().Select(item => item.GetString())
            .Should().BeEquivalentTo(AdminAndViewerRoles);
        payload.GetProperty("isSuperAdmin").GetBoolean().Should().BeFalse();

        JsonElement permissions = payload.GetProperty("permissions");
        permissions.ValueKind.Should().Be(JsonValueKind.Object);
        permissions.GetProperty(PageSlugs.AdminPanel).ValueKind.Should().Be(JsonValueKind.Array);
        permissions.GetProperty(PageSlugs.AdminPanel).EnumerateArray().Select(item => item.GetString())
            .Should().BeEquivalentTo(AllCrudVerbs);
        permissions.GetProperty(PageSlugs.Settings).ValueKind.Should().Be(JsonValueKind.Array);
    }

    private static string FindFrontendArtifactPath()
    {
        var fromCurrentDirectory = FindFrontendArtifactPath(new DirectoryInfo(Directory.GetCurrentDirectory()));
        return fromCurrentDirectory
            ?? FindFrontendArtifactPath(new DirectoryInfo(AppContext.BaseDirectory))
            ?? throw new FileNotFoundException("Could not find artifacts/internal-comms/index.html for the auth contract test.");
    }

    private static string? FindFrontendArtifactPath(DirectoryInfo directory)
    {
        for (DirectoryInfo? current = directory; current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, "artifacts", "internal-comms", "index.html");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
