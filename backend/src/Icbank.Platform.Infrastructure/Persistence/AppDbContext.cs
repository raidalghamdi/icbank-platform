using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Domain.Reports;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Domain.Weekend;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core write-model context (R-BE-021: Code-First with explicit
/// <c>OnModelCreating</c>, no attribute mapping). Entity configuration is discovered from
/// <see cref="IEntityTypeConfiguration{TEntity}"/> implementations in this assembly. Each
/// configuration is responsible for its own soft-delete query filter (R-BE-023) — this context
/// deliberately does not apply a global reflection-based filter (see SCAFFOLD-NOTES.md).
/// </summary>
public sealed class AppDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Initializes a new instance of the <see cref="AppDbContext"/> class.</summary>
    /// <param name="options">The EF Core options, including the SQL Server connection and registered interceptors.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ── Identity / RBAC ────────────────────────────────────────────────────

    /// <summary>Gets the set of platform user accounts.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the set of RBAC roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Gets the set of RBAC-gated app pages.</summary>
    public DbSet<Page> Pages => Set<Page>();

    /// <summary>Gets the set of the five action-verb permissions.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Gets the role × page × permission grant matrix.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>Gets the user → role assignments.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Gets the per-user allow/deny permission overrides.</summary>
    public DbSet<UserPageOverride> UserPageOverrides => Set<UserPageOverride>();

    /// <summary>Gets the audit trail of auth/admin events.</summary>
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    /// <summary>Gets the key/value system settings store.</summary>
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    /// <summary>Gets the set of rotatable refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Gets the dedicated privileged-action audit log.</summary>
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    // ── AI Year ─────────────────────────────────────────────────────────────

    /// <summary>Gets the set of AI Year 2026 activation records.</summary>
    public DbSet<AiYearActivation> AiYearActivations => Set<AiYearActivation>();

    /// <summary>Gets the set of media attached to AI Year activations.</summary>
    public DbSet<AiYearActivationChannel> AiYearActivationChannels => Set<AiYearActivationChannel>();

    /// <summary>Gets the set of AI Year activation media.</summary>
    public DbSet<AiYearMedia> AiYearMedia => Set<AiYearMedia>();

    /// <summary>Gets the set of free-form metrics attached to AI Year activations.</summary>
    public DbSet<AiYearMetric> AiYearMetrics => Set<AiYearMetric>();

    // ── Daily Reports ────────────────────────────────────────────────────────

    /// <summary>Gets the set of ingested daily-report payloads.</summary>
    public DbSet<DailyReport> DailyReports => Set<DailyReport>();

    // ── Designs ──────────────────────────────────────────────────────────────

    /// <summary>Gets the set of reusable design templates.</summary>
    public DbSet<DesignTemplate> DesignTemplates => Set<DesignTemplate>();

    /// <summary>Gets the set of uploaded brand logo assets.</summary>
    public DbSet<BrandLogo> BrandLogos => Set<BrandLogo>();

    /// <summary>Gets the set of uploaded brand font assets.</summary>
    public DbSet<BrandFont> BrandFonts => Set<BrandFont>();

    /// <summary>Gets the set of AI/composer-rendered designs.</summary>
    public DbSet<GeneratedDesign> GeneratedDesigns => Set<GeneratedDesign>();

    // ── GAC content ─────────────────────────────────────────────────────────

    /// <summary>Gets the set of GAC's official publications.</summary>
    public DbSet<GacPublication> GacPublications => Set<GacPublication>();

    /// <summary>Gets the set of cached social-feed items.</summary>
    public DbSet<GacSocialPost> GacSocialPosts => Set<GacSocialPost>();

    /// <summary>Gets the set of cached news/decision items.</summary>
    public DbSet<GacNewsItem> GacNewsItems => Set<GacNewsItem>();

    // ── International Days ─────────────────────────────────────────────────

    /// <summary>Gets the catalogue of international observance days.</summary>
    public DbSet<InternationalDay> InternationalDays => Set<InternationalDay>();

    /// <summary>Gets the per-year theme records for a day.</summary>
    public DbSet<DayYearlyTheme> DayYearlyThemes => Set<DayYearlyTheme>();

    /// <summary>Gets the recorded campaign activations for a day.</summary>
    public DbSet<DayActivation> DayActivations => Set<DayActivation>();

    /// <summary>Gets the source-citation records for AI-search provenance.</summary>
    public DbSet<IntlDaySource> IntlDaySources => Set<IntlDaySource>();

    /// <summary>Gets the AI search-query audit log.</summary>
    public DbSet<IntlSearchHistory> IntlSearchHistories => Set<IntlSearchHistory>();

    // ── Media Monitoring ───────────────────────────────────────────────────

    /// <summary>Gets the set of editable media-monitoring reports.</summary>
    public DbSet<MediaReport> MediaReports => Set<MediaReport>();

    /// <summary>Gets the reusable AI prompt-template library.</summary>
    public DbSet<PromptFramework> PromptFrameworks => Set<PromptFramework>();

    /// <summary>Gets the set of immutable, officially-numbered final media reports.</summary>
    public DbSet<FinalMediaReport> FinalMediaReports => Set<FinalMediaReport>();

    /// <summary>Gets the audit log of QA/search queries against final reports.</summary>
    public DbSet<ReportsQaQuery> ReportsQaQueries => Set<ReportsQaQuery>();

    // ── Shorfah Magazine ────────────────────────────────────────────────────

    /// <summary>Gets the set of monthly magazine issues.</summary>
    public DbSet<ShorfahIssue> ShorfahIssues => Set<ShorfahIssue>();

    /// <summary>Gets the set of content sections within an issue.</summary>
    public DbSet<ShorfahSection> ShorfahSections => Set<ShorfahSection>();

    /// <summary>Gets the per-section permission grants.</summary>
    public DbSet<ShorfahSectionPermission> ShorfahSectionPermissions => Set<ShorfahSectionPermission>();

    /// <summary>Gets the media attached to a section.</summary>
    public DbSet<ShorfahSectionMedia> ShorfahSectionMedia => Set<ShorfahSectionMedia>();

    /// <summary>Gets the workflow-transition audit trail.</summary>
    public DbSet<ShorfahWorkflowLog> ShorfahWorkflowLogs => Set<ShorfahWorkflowLog>();

    /// <summary>Gets the contributor/role assignments per section.</summary>
    public DbSet<ShorfahAssignment> ShorfahAssignments => Set<ShorfahAssignment>();

    /// <summary>Gets the log of reminder notifications sent.</summary>
    public DbSet<ShorfahReminder> ShorfahReminders => Set<ShorfahReminder>();

    /// <summary>Gets the default SLA-day configuration per section type.</summary>
    public DbSet<ShorfahSectionSlaDefault> ShorfahSectionSlaDefaults => Set<ShorfahSectionSlaDefault>();

    /// <summary>Gets the in-app notification inbox.</summary>
    public DbSet<ShorfahNotification> ShorfahNotifications => Set<ShorfahNotification>();

    // ── Week Start ──────────────────────────────────────────────────────────

    /// <summary>Gets the archive of past "week start" messages.</summary>
    public DbSet<ArchiveEntry> ArchiveEntries => Set<ArchiveEntry>();

    /// <summary>Gets the singleton learned writing-style profile.</summary>
    public DbSet<StyleProfile> StyleProfiles => Set<StyleProfile>();

    /// <summary>Gets the AI-generated week-start message drafts.</summary>
    public DbSet<GeneratedOutput> GeneratedOutputs => Set<GeneratedOutput>();

    // ── Weekend ─────────────────────────────────────────────────────────────

    /// <summary>Gets the curated library of weekend venues/places.</summary>
    public DbSet<WeekendPlace> WeekendPlaces => Set<WeekendPlace>();

    /// <summary>Gets the AI-generated weekly weekend-content drafts.</summary>
    public DbSet<WeekendDraft> WeekendDrafts => Set<WeekendDraft>();

    // ── IApplicationDbContext explicit surface (R-BE-002: Application sees IQueryable, not DbSet) ──

    /// <inheritdoc cref="IApplicationDbContext.Users" />
    IQueryable<User> IApplicationDbContext.Users => Users;

    /// <inheritdoc cref="IApplicationDbContext.Roles" />
    IQueryable<Role> IApplicationDbContext.Roles => Roles;

    /// <inheritdoc cref="IApplicationDbContext.Pages" />
    IQueryable<Page> IApplicationDbContext.Pages => Pages;

    /// <inheritdoc cref="IApplicationDbContext.Permissions" />
    IQueryable<Permission> IApplicationDbContext.Permissions => Permissions;

    /// <inheritdoc cref="IApplicationDbContext.RolePermissions" />
    IQueryable<RolePermission> IApplicationDbContext.RolePermissions => RolePermissions;

    /// <inheritdoc cref="IApplicationDbContext.UserRoles" />
    IQueryable<UserRole> IApplicationDbContext.UserRoles => UserRoles;

    /// <inheritdoc cref="IApplicationDbContext.UserPageOverrides" />
    IQueryable<UserPageOverride> IApplicationDbContext.UserPageOverrides => UserPageOverrides;

    /// <inheritdoc cref="IApplicationDbContext.ActivityLogs" />
    IQueryable<ActivityLog> IApplicationDbContext.ActivityLogs => ActivityLogs;

    /// <inheritdoc cref="IApplicationDbContext.AuditLogEntries" />
    IQueryable<AuditLogEntry> IApplicationDbContext.AuditLogEntries => AuditLogEntries;

    /// <inheritdoc cref="IApplicationDbContext.SystemSettings" />
    IQueryable<SystemSetting> IApplicationDbContext.SystemSettings => SystemSettings;

    /// <inheritdoc cref="IApplicationDbContext.RefreshTokens" />
    IQueryable<RefreshToken> IApplicationDbContext.RefreshTokens => RefreshTokens;

    /// <inheritdoc cref="IApplicationDbContext.DailyReports" />
    IQueryable<DailyReport> IApplicationDbContext.DailyReports => DailyReports;

    /// <inheritdoc cref="IApplicationDbContext.WeekendPlaces" />
    IQueryable<WeekendPlace> IApplicationDbContext.WeekendPlaces => WeekendPlaces;

    /// <inheritdoc cref="IApplicationDbContext.WeekendDrafts" />
    IQueryable<WeekendDraft> IApplicationDbContext.WeekendDrafts => WeekendDrafts;

    /// <inheritdoc cref="IApplicationDbContext.ArchiveEntries" />
    IQueryable<ArchiveEntry> IApplicationDbContext.ArchiveEntries => ArchiveEntries;

    /// <inheritdoc cref="IApplicationDbContext.StyleProfiles" />
    IQueryable<StyleProfile> IApplicationDbContext.StyleProfiles => StyleProfiles;

    /// <inheritdoc cref="IApplicationDbContext.GeneratedOutputs" />
    IQueryable<GeneratedOutput> IApplicationDbContext.GeneratedOutputs => GeneratedOutputs;

    /// <inheritdoc cref="IApplicationDbContext.AiYearActivations" />
    IQueryable<AiYearActivation> IApplicationDbContext.AiYearActivations => AiYearActivations;

    /// <inheritdoc cref="IApplicationDbContext.AiYearActivationChannels" />
    IQueryable<AiYearActivationChannel> IApplicationDbContext.AiYearActivationChannels => AiYearActivationChannels;

    /// <inheritdoc cref="IApplicationDbContext.AiYearMedia" />
    IQueryable<AiYearMedia> IApplicationDbContext.AiYearMedia => AiYearMedia;

    /// <inheritdoc cref="IApplicationDbContext.AiYearMetrics" />
    IQueryable<AiYearMetric> IApplicationDbContext.AiYearMetrics => AiYearMetrics;

    /// <inheritdoc cref="IApplicationDbContext.InternationalDays" />
    IQueryable<InternationalDay> IApplicationDbContext.InternationalDays => InternationalDays;

    /// <inheritdoc cref="IApplicationDbContext.DayYearlyThemes" />
    IQueryable<DayYearlyTheme> IApplicationDbContext.DayYearlyThemes => DayYearlyThemes;

    /// <inheritdoc cref="IApplicationDbContext.DayActivations" />
    IQueryable<DayActivation> IApplicationDbContext.DayActivations => DayActivations;

    /// <inheritdoc cref="IApplicationDbContext.IntlDaySources" />
    IQueryable<IntlDaySource> IApplicationDbContext.IntlDaySources => IntlDaySources;

    /// <inheritdoc cref="IApplicationDbContext.IntlSearchHistories" />
    IQueryable<IntlSearchHistory> IApplicationDbContext.IntlSearchHistories => IntlSearchHistories;

    /// <inheritdoc cref="IApplicationDbContext.GacPublications" />
    IQueryable<GacPublication> IApplicationDbContext.GacPublications => GacPublications;

    /// <inheritdoc cref="IApplicationDbContext.GacSocialPosts" />
    IQueryable<GacSocialPost> IApplicationDbContext.GacSocialPosts => GacSocialPosts;

    /// <inheritdoc cref="IApplicationDbContext.GacNewsItems" />
    IQueryable<GacNewsItem> IApplicationDbContext.GacNewsItems => GacNewsItems;

    /// <inheritdoc cref="IApplicationDbContext.MediaReports" />
    IQueryable<MediaReport> IApplicationDbContext.MediaReports => MediaReports;

    /// <inheritdoc cref="IApplicationDbContext.PromptFrameworks" />
    IQueryable<PromptFramework> IApplicationDbContext.PromptFrameworks => PromptFrameworks;

    /// <inheritdoc cref="IApplicationDbContext.FinalMediaReports" />
    IQueryable<FinalMediaReport> IApplicationDbContext.FinalMediaReports => FinalMediaReports;

    /// <inheritdoc cref="IApplicationDbContext.ReportsQaQueries" />
    IQueryable<ReportsQaQuery> IApplicationDbContext.ReportsQaQueries => ReportsQaQueries;

    /// <inheritdoc cref="IApplicationDbContext.DesignTemplates" />
    IQueryable<DesignTemplate> IApplicationDbContext.DesignTemplates => DesignTemplates;

    /// <inheritdoc cref="IApplicationDbContext.BrandLogos" />
    IQueryable<BrandLogo> IApplicationDbContext.BrandLogos => BrandLogos;

    /// <inheritdoc cref="IApplicationDbContext.BrandFonts" />
    IQueryable<BrandFont> IApplicationDbContext.BrandFonts => BrandFonts;

    /// <inheritdoc cref="IApplicationDbContext.GeneratedDesigns" />
    IQueryable<GeneratedDesign> IApplicationDbContext.GeneratedDesigns => GeneratedDesigns;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahIssues" />
    IQueryable<ShorfahIssue> IApplicationDbContext.ShorfahIssues => ShorfahIssues;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahSections" />
    IQueryable<ShorfahSection> IApplicationDbContext.ShorfahSections => ShorfahSections;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahSectionPermissions" />
    IQueryable<ShorfahSectionPermission> IApplicationDbContext.ShorfahSectionPermissions => ShorfahSectionPermissions;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahSectionMedia" />
    IQueryable<ShorfahSectionMedia> IApplicationDbContext.ShorfahSectionMedia => ShorfahSectionMedia;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahWorkflowLogs" />
    IQueryable<ShorfahWorkflowLog> IApplicationDbContext.ShorfahWorkflowLogs => ShorfahWorkflowLogs;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahAssignments" />
    IQueryable<ShorfahAssignment> IApplicationDbContext.ShorfahAssignments => ShorfahAssignments;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahReminders" />
    IQueryable<ShorfahReminder> IApplicationDbContext.ShorfahReminders => ShorfahReminders;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahSectionSlaDefaults" />
    IQueryable<ShorfahSectionSlaDefault> IApplicationDbContext.ShorfahSectionSlaDefaults => ShorfahSectionSlaDefaults;

    /// <inheritdoc cref="IApplicationDbContext.ShorfahNotifications" />
    IQueryable<ShorfahNotification> IApplicationDbContext.ShorfahNotifications => ShorfahNotifications;

    /// <inheritdoc cref="IApplicationDbContext.Add{TEntity}" />
    void IApplicationDbContext.Add<TEntity>(TEntity entity) => Set<TEntity>().Add(entity);

    /// <inheritdoc cref="IApplicationDbContext.Remove{TEntity}" />
    void IApplicationDbContext.Remove<TEntity>(TEntity entity) => Set<TEntity>().Remove(entity);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Why: discovering IEntityTypeConfiguration<T> from this assembly means every future
        // entity config is picked up automatically — no edits to AppDbContext are needed when
        // a new entity config lands (R-BE-021).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
