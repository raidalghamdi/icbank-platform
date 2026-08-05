using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Proves SEC-16 for the per-user Shorfah notification inbox (API-SURFACE.md §19): every route on
/// <c>NotificationsController</c> is scoped to the caller's own rows via
/// <see cref="Icbank.Platform.Application.Common.Interfaces.IResourceAuthorizationService.AuthorizeShorfahNotificationResourceAsync"/>,
/// which checks existence AND ownership in the same predicate so a foreign notification id is
/// indistinguishable from a nonexistent one -- there is no existence oracle. Notifications are
/// per-user data and the audit named this the top remaining defect class for this wave.
/// </summary>
public sealed class ShorfahNotificationsIdorTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListNotifications_OnlyReturnsCallersOwnRows_NeverAnotherUsers()
    {
        (User userA, User userB) = await SeedTwoNotificationViewersAsync();
        await SeedNotificationAsync(userA.Id, "initial", "لمستخدم أ");
        await SeedNotificationAsync(userB.Id, "initial", "لمستخدم ب");
        await SeedNotificationAsync(userB.Id, "reminder_overdue", "تذكير لمستخدم ب");
        HttpClient clientA = await LoginAsync(userA.Email);

        HttpResponseMessage response = await clientA.GetAsync(new Uri("/api/v1/notifications?page=1&pageSize=30", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        NotificationsListPayload? payload = await response.Content.ReadFromJsonAsync<NotificationsListPayload>();
        payload!.Notifications.Should().ContainSingle();
        payload.Notifications.Should().OnlyContain(n => n.Title == "لمستخدم أ", "GET /notifications must never surface another user's rows even though B's rows exist in the same table");
        payload.Total.Should().Be(1);
    }

    [Fact]
    public async Task MarkRead_OnAnotherUsersNotification_ReturnsNotFound_AndLeavesItUnread()
    {
        (User userA, User userB) = await SeedTwoNotificationViewersAsync();
        ShorfahNotification bNotification = await SeedNotificationAsync(userB.Id, "initial", "لمستخدم ب");
        HttpClient clientA = await LoginAsync(userA.Email);

        HttpResponseMessage response = await clientA.PostAsync(new Uri($"/api/v1/notifications/{bNotification.Id}/read", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "user A must never be able to mark user B's notification read, and the response must not distinguish this from an unknown id");
        (await IsReadAsync(bNotification.Id)).Should().BeFalse("A's refused attempt must not have mutated B's row");
    }

    [Fact]
    public async Task MarkRead_ForeignId_And_NonexistentId_ReturnIndistinguishableNotFoundBodies()
    {
        // Why: SEC-16's precise guarantee is that a foreign-owned id and a genuinely nonexistent
        // id produce the same observable response -- otherwise the response itself becomes an
        // existence oracle an attacker can use to enumerate other users' notification ids.
        (User userA, User userB) = await SeedTwoNotificationViewersAsync();
        ShorfahNotification bNotification = await SeedNotificationAsync(userB.Id, "initial", "لمستخدم ب");
        HttpClient clientA = await LoginAsync(userA.Email);

        HttpResponseMessage foreignResponse = await clientA.PostAsync(new Uri($"/api/v1/notifications/{bNotification.Id}/read", UriKind.Relative), content: null);
        HttpResponseMessage nonexistentResponse = await clientA.PostAsync(new Uri("/api/v1/notifications/999999/read", UriKind.Relative), content: null);

        foreignResponse.StatusCode.Should().Be(nonexistentResponse.StatusCode);
        var foreignBody = await foreignResponse.Content.ReadAsStringAsync();
        var nonexistentBody = await nonexistentResponse.Content.ReadAsStringAsync();
        foreignBody.Should().Be(nonexistentBody, "a foreign id must be byte-for-byte indistinguishable from an unknown id");
    }

    [Fact]
    public async Task MarkRead_OwnNotification_Succeeds()
    {
        (User userA, _) = await SeedTwoNotificationViewersAsync();
        ShorfahNotification aNotification = await SeedNotificationAsync(userA.Id, "initial", "لمستخدم أ");
        HttpClient clientA = await LoginAsync(userA.Email);

        HttpResponseMessage response = await clientA.PostAsync(new Uri($"/api/v1/notifications/{aNotification.Id}/read", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsReadAsync(aNotification.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllRead_OnlyAffectsCallersRows_LeavesOtherUsersUnreadCountUnchanged()
    {
        (User userA, User userB) = await SeedTwoNotificationViewersAsync();
        await SeedNotificationAsync(userA.Id, "initial", "A1");
        await SeedNotificationAsync(userA.Id, "reminder_overdue", "A2");
        ShorfahNotification b1 = await SeedNotificationAsync(userB.Id, "initial", "B1");
        ShorfahNotification b2 = await SeedNotificationAsync(userB.Id, "reminder_overdue", "B2");
        HttpClient clientA = await LoginAsync(userA.Email);

        HttpResponseMessage response = await clientA.PostAsync(new Uri("/api/v1/notifications/read-all", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsReadAsync(b1.Id)).Should().BeFalse("B's unread notifications must be untouched by A's read-all call");
        (await IsReadAsync(b2.Id)).Should().BeFalse("B's unread notifications must be untouched by A's read-all call");
        (await UnreadCountAsync(userB.Id)).Should().Be(2, "B's unread count must be exactly unchanged after A calls read-all");
    }

    [Fact]
    public async Task MarkAllRead_OwnUnreadNotifications_AllBecomeRead()
    {
        (User userA, _) = await SeedTwoNotificationViewersAsync();
        ShorfahNotification a1 = await SeedNotificationAsync(userA.Id, "initial", "A1");
        ShorfahNotification a2 = await SeedNotificationAsync(userA.Id, "reminder_overdue", "A2");
        HttpClient clientA = await LoginAsync(userA.Email);

        HttpResponseMessage response = await clientA.PostAsync(new Uri("/api/v1/notifications/read-all", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsReadAsync(a1.Id)).Should().BeTrue();
        (await IsReadAsync(a2.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ListNotifications_Unauthenticated_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/notifications", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkRead_Unauthenticated_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/notifications/1/read", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAllRead_Unauthenticated_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/notifications/read-all", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<bool> IsReadAsync(int notificationId)
    {
        using AppDbContext dbContext = CreateDbContext();
        ShorfahNotification notification = await dbContext.ShorfahNotifications.SingleAsync(n => n.Id == notificationId);
        return notification.IsRead == true;
    }

    private async Task<int> UnreadCountAsync(int userId)
    {
        using AppDbContext dbContext = CreateDbContext();
        return await dbContext.ShorfahNotifications.CountAsync(n => n.UserId == userId && n.IsRead != true);
    }

    private async Task<ShorfahNotification> SeedNotificationAsync(int userId, string type, string title)
    {
        using AppDbContext dbContext = CreateDbContext();
        var notification = new ShorfahNotification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = title,
            IsRead = false,
            CreatedBy = "test",
        };
        dbContext.Add(notification);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return notification;
    }

    /// <summary>
    /// Seeds two independent users, each in their own role carrying only shorfah:view (no
    /// admin_panel, no super_admin, no cross-visibility of one another) -- the minimal grant that
    /// satisfies the controller-level <c>[Authorize(Policy = "shorfah:view")]</c> gate so the IDOR
    /// boundary being tested is the resource-level ownership check inside the handler, not the
    /// coarse RBAC policy.
    /// </summary>
    private async Task<(User UserA, User UserB)> SeedTwoNotificationViewersAsync()
    {
        using AppDbContext dbContext = CreateDbContext();

        Role viewerRole = new() { Name = $"shorfah_notification_viewer_{Guid.NewGuid()}", NameAr = "shorfah_viewer", CreatedBy = "test" };
        dbContext.Add(viewerRole);
        Page shorfahPage = new() { Slug = PageSlugs.Shorfah, NameAr = "shorfah", CreatedBy = "test" };
        Permission viewPermission = new() { Name = "view", NameAr = "view", CreatedBy = "test" };
        dbContext.AddRange(shorfahPage, viewPermission);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.RolePermissions.Add(new RolePermission { RoleId = viewerRole.Id, PageId = shorfahPage.Id, PermissionId = viewPermission.Id, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var hasher = new PasswordHasher<PasswordHasherSubject>();
        var hashedPassword = hasher.HashPassword(new PasswordHasherSubject(), SharedPassword);
        var userA = new User { Email = $"notif-a-{Guid.NewGuid()}@test.local", Name = "Notification Viewer A", PasswordHash = hashedPassword, IsActive = true, CreatedBy = "test" };
        var userB = new User { Email = $"notif-b-{Guid.NewGuid()}@test.local", Name = "Notification Viewer B", PasswordHash = hashedPassword, IsActive = true, CreatedBy = "test" };
        dbContext.AddRange(userA, userB);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.UserRoles.Add(new UserRole { UserId = userA.Id, RoleId = viewerRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        dbContext.UserRoles.Add(new UserRole { UserId = userB.Id, RoleId = viewerRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (userA, userB);
    }

    private async Task<HttpClient> LoginAsync(string email)
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password = SharedPassword });
        loginResponse.EnsureSuccessStatusCode();

        LoginResponsePayload? payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private sealed record LoginResponsePayload(string AccessToken);

    private sealed record NotificationDto(int Id, int? IssueId, int? SectionId, string Type, string Title, string? Body, string? Url, bool? IsRead, DateTime CreatedAt);

    private sealed record NotificationsListPayload(List<NotificationDto> Notifications, int Page, int PageSize, int Total);
}
