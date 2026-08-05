using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Domain.Reports;
using Icbank.Platform.Domain.Weekend;

namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Application-layer port onto the persistence context (R-BE-002: Application may not reference
/// EF Core directly — this interface exposes only <see cref="IQueryable{T}"/> for reads and
/// explicit Add/Remove/SaveChanges methods for writes, never <c>DbSet&lt;T&gt;</c> or
/// <c>DbContext</c> itself). Implemented by <c>AppDbContext</c> in Infrastructure. Only the
/// identity/RBAC sets needed by the auth and admin handlers are exposed here — other feature
/// areas will grow their own narrow slice of this interface as they're ported.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Gets a queryable over platform user accounts.</summary>
    IQueryable<User> Users { get; }

    /// <summary>Gets a queryable over RBAC roles.</summary>
    IQueryable<Role> Roles { get; }

    /// <summary>Gets a queryable over RBAC-gated app pages.</summary>
    IQueryable<Page> Pages { get; }

    /// <summary>Gets a queryable over the four action-verb permissions.</summary>
    IQueryable<Permission> Permissions { get; }

    /// <summary>Gets a queryable over the role × page × permission grant matrix.</summary>
    IQueryable<RolePermission> RolePermissions { get; }

    /// <summary>Gets a queryable over user → role assignments (many-to-many; union-of-permissions semantics).</summary>
    IQueryable<UserRole> UserRoles { get; }

    /// <summary>Gets a queryable over per-user allow/deny permission overrides.</summary>
    IQueryable<UserPageOverride> UserPageOverrides { get; }

    /// <summary>Gets a queryable over the audit trail of auth/admin events.</summary>
    IQueryable<ActivityLog> ActivityLogs { get; }

    /// <summary>Gets a queryable over the dedicated privileged-action audit log.</summary>
    IQueryable<AuditLogEntry> AuditLogEntries { get; }

    /// <summary>Gets a queryable over the key/value system settings store.</summary>
    IQueryable<SystemSetting> SystemSettings { get; }

    /// <summary>Gets a queryable over rotatable refresh tokens.</summary>
    IQueryable<RefreshToken> RefreshTokens { get; }

    /// <summary>Gets a queryable over ingested daily-report payloads (Wave 1: Daily Report).</summary>
    IQueryable<DailyReport> DailyReports { get; }

    /// <summary>Gets a queryable over the curated library of weekend venues/places (Wave 1: Weekend Places).</summary>
    IQueryable<WeekendPlace> WeekendPlaces { get; }

    /// <summary>Gets a queryable over the AI-generated weekly weekend-content drafts (Wave 1: Weekend Drafts).</summary>
    IQueryable<WeekendDraft> WeekendDrafts { get; }

    /// <summary>Gets a queryable over the Week Start message archive (Wave 1: Week Start).</summary>
    IQueryable<ArchiveEntry> ArchiveEntries { get; }

    /// <summary>Gets a queryable over the singleton learned writing-style profile (Wave 1: Week Start).</summary>
    IQueryable<StyleProfile> StyleProfiles { get; }

    /// <summary>Gets a queryable over the AI-generated week-start message drafts (Wave 1: Week Start).</summary>
    IQueryable<GeneratedOutput> GeneratedOutputs { get; }

    /// <summary>Gets a queryable over AI Year 2026 activation records (Wave 1: Dashboard aggregation input; Wave 2: full CRUD).</summary>
    IQueryable<AiYearActivation> AiYearActivations { get; }

    /// <summary>Gets a queryable over AI Year activation channels (Wave 2: AI Year).</summary>
    IQueryable<AiYearActivationChannel> AiYearActivationChannels { get; }

    /// <summary>Gets a queryable over AI Year activation media (Wave 2: AI Year).</summary>
    IQueryable<AiYearMedia> AiYearMedia { get; }

    /// <summary>Gets a queryable over AI Year activation metrics (Wave 2: AI Year).</summary>
    IQueryable<AiYearMetric> AiYearMetrics { get; }

    /// <summary>Gets a queryable over the international-observance-day catalogue (Wave 1: Dashboard aggregation input; Wave 2: full CRUD).</summary>
    IQueryable<InternationalDay> InternationalDays { get; }

    /// <summary>Gets a queryable over per-year day themes (Wave 2: International Days).</summary>
    IQueryable<DayYearlyTheme> DayYearlyThemes { get; }

    /// <summary>Gets a queryable over recorded day activations (Wave 2: International Days).</summary>
    IQueryable<DayActivation> DayActivations { get; }

    /// <summary>Gets a queryable over AI-search source citations (Wave 2: International Days).</summary>
    IQueryable<IntlDaySource> IntlDaySources { get; }

    /// <summary>Gets a queryable over the AI-search rate-limit/audit history (Wave 2: International Days).</summary>
    IQueryable<IntlSearchHistory> IntlSearchHistories { get; }

    /// <summary>Gets a queryable over the GAC publications library (Wave 2: GAC Content).</summary>
    IQueryable<GacPublication> GacPublications { get; }

    /// <summary>Gets a queryable over cached GAC social posts (Wave 2: GAC Content).</summary>
    IQueryable<GacSocialPost> GacSocialPosts { get; }

    /// <summary>Gets a queryable over cached GAC news/decision items (Wave 2: GAC Content).</summary>
    IQueryable<GacNewsItem> GacNewsItems { get; }

    /// <summary>Gets a queryable over editable, audience-tiered media-monitoring reports (Wave 3a: Media Monitoring).</summary>
    IQueryable<MediaReport> MediaReports { get; }

    /// <summary>Gets a queryable over reusable AI prompt frameworks (Wave 3a: Media Monitoring + Prompts).</summary>
    IQueryable<PromptFramework> PromptFrameworks { get; }

    /// <summary>Gets a queryable over immutable, officially-numbered final media reports (Wave 3a: Final Media Reports).</summary>
    IQueryable<FinalMediaReport> FinalMediaReports { get; }

    /// <summary>Gets a queryable over the QA/search query audit trail against final reports (Wave 3a: Final Media Reports).</summary>
    IQueryable<ReportsQaQuery> ReportsQaQueries { get; }

    /// <summary>Tracks a new entity for insertion.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to add.</param>
    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>Tracks an existing entity for removal (hard delete — only used for lookup/reference tables, never business tables, per R-BE-023).</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to remove.</param>
    void Remove<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>Persists all tracked changes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
