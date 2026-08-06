using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Locks the AI Year create/update exchange to the nested browser request plus explicit
/// <c>ok</c> response envelope used by the original frontend.
/// </summary>
public sealed class AiYearPayloadContractTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private static readonly string[] UpdatedChannels = { "linkedin", "x" };

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ActivationCreateAndUpdate_AcceptNestedBrowserPayloadAndReturnOkEnvelopes()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        await GrantAiYearPermissionsAsync(dbContext);

        using HttpClient client = await LoginAsAdminAsync(seeded.Admin.Email);
        var legacyCreatePayload = new
        {
            activation = new
            {
                title = "حملة العقد",
                month = 8,
                year = 2026,
                activationDate = "2026-08-06",
                type = "حملة",
                channels = new[] { "linkedin" },
                status = "published",
            },
            media = Array.Empty<object>(),
            metrics = Array.Empty<object>(),
        };

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/ai-year/activations", UriKind.Relative),
            legacyCreatePayload);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        JsonElement createPayload = createDocument.RootElement;
        createPayload.GetProperty("ok").GetBoolean().Should().BeTrue();
        var activationId = createPayload.GetProperty("id").GetInt32();
        createPayload.GetProperty("activation").GetProperty("title").GetString().Should().Be("حملة العقد");

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/ai-year/activations/{activationId}", UriKind.Relative),
            new
            {
                activation = new { title = "حملة العقد المحدثة", channels = UpdatedChannels },
                media = Array.Empty<object>(),
            });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var updateDocument = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        JsonElement updatePayload = updateDocument.RootElement;
        updatePayload.GetProperty("ok").GetBoolean().Should().BeTrue();
        updatePayload.GetProperty("activation").GetProperty("title").GetString().Should().Be("حملة العقد المحدثة");
    }

    private static async Task GrantAiYearPermissionsAsync(AppDbContext dbContext)
    {
        Role adminRole = await dbContext.Roles.SingleAsync(role => role.Name == "admin");
        Page aiYearPage = await AuthTestDataBuilder.EnsurePageAsync(dbContext, "ai_year");
        Permission viewPermission = await AuthTestDataBuilder.EnsurePermissionAsync(dbContext, "view");
        Permission createPermission = await AuthTestDataBuilder.EnsurePermissionAsync(dbContext, "create");
        Permission editPermission = await AuthTestDataBuilder.EnsurePermissionAsync(dbContext, "edit");

        dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = adminRole.Id,
            PageId = aiYearPage.Id,
            PermissionId = viewPermission.Id,
            CreatedBy = "test",
        });
        dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = adminRole.Id,
            PageId = aiYearPage.Id,
            PermissionId = createPermission.Id,
            CreatedBy = "test",
        });
        dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = adminRole.Id,
            PageId = aiYearPage.Id,
            PermissionId = editPermission.Id,
            CreatedBy = "test",
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task<HttpClient> LoginAsAdminAsync(string email)
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { email, password = SharedPassword });
        loginResponse.EnsureSuccessStatusCode();

        using var loginDocument = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = loginDocument.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("The login response did not include an access token.");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
