using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Icbank.Platform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Persistence;

/// <summary>
/// Verifies <see cref="AuditInterceptor"/> actually stamps the audit columns R-BE-022 mandates on
/// every mutation -- this had zero coverage before the Api/Infrastructure floor was widened, and
/// it is exactly the kind of cross-cutting write-path logic that is easy to silently break (e.g.
/// a refactor that stops registering the interceptor) without a failing test to catch it.
/// </summary>
public sealed class AuditInterceptorTests
{
    private const string ActingUserId = "17";

    [Fact]
    public async Task SaveChangesAsync_NewEntity_StampsCreatedAtAndCreatedByOnly()
    {
        using AppDbContext context = CreateContext(ActingUserId);
        var setting = new SystemSetting { Key = "session_duration_minutes", Value = "60" };
        context.SystemSettings.Add(setting);

        DateTime before = DateTime.UtcNow;
        await context.SaveChangesAsync();
        DateTime after = DateTime.UtcNow;

        setting.CreatedBy.Should().Be(ActingUserId);
        setting.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        setting.UpdatedAt.Should().BeNull();
        setting.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntity_StampsUpdatedAtAndUpdatedByWithoutTouchingCreatedColumns()
    {
        var databaseName = Guid.NewGuid().ToString();
        using AppDbContext creationContext = CreateContext("1", databaseName);
        var setting = new SystemSetting { Key = "azure_ad_tenant_id", Value = "initial" };
        creationContext.SystemSettings.Add(setting);
        await creationContext.SaveChangesAsync();
        DateTime originalCreatedAt = setting.CreatedAt;

        using AppDbContext updateContext = CreateContext(ActingUserId, databaseName);
        SystemSetting tracked = await updateContext.SystemSettings.SingleAsync(s => s.Key == "azure_ad_tenant_id");
        tracked.Value = "updated";
        await updateContext.SaveChangesAsync();

        tracked.UpdatedBy.Should().Be(ActingUserId);
        tracked.UpdatedAt.Should().NotBeNull();
        tracked.CreatedAt.Should().Be(originalCreatedAt, "an update must never rewrite the original creation stamp");
        tracked.CreatedBy.Should().Be("1", "an update must never rewrite the original creator");
    }

    [Fact]
    public async Task SaveChangesAsync_NoCurrentUser_FallsBackToSystemMarkerRatherThanThrowing()
    {
        using AppDbContext context = CreateContext(currentUserId: "system");
        var setting = new SystemSetting { Key = "password_min_length", Value = "12" };
        context.SystemSettings.Add(setting);

        await context.SaveChangesAsync();

        setting.CreatedBy.Should().Be("system");
    }

    private static AppDbContext CreateContext(string currentUserId, string? sharedDatabaseName = null)
    {
        var interceptor = new AuditInterceptor(new FixedCurrentUserService(currentUserId));

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(sharedDatabaseName ?? Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Minimal <see cref="ICurrentUserService"/> test double that always resolves to a fixed identity.</summary>
    private sealed class FixedCurrentUserService : ICurrentUserService
    {
        public FixedCurrentUserService(string userId)
        {
            UserId = userId;
        }

        public string UserId { get; }
    }
}
