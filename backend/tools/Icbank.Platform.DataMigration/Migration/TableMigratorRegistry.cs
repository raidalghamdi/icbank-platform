using Icbank.Platform.DataMigration.Migration.Migrators;

namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// The single, authoritative FK-safe dependency order every mode (validate/migrate/reconcile)
/// walks tables in (task requirement 2). Ordering here — not inside any individual migrator —
/// is what makes cross-table FK resolution via <see cref="IIdMappingStore"/> safe: a table is
/// never processed before every table it references.
/// </summary>
/// <remarks>
/// <para>Hand-built, individually-transformed migrators exist today for the tables that needed
/// non-trivial decisions (multi-role union, duplicate detection, nullable-timestamp backfill,
/// native-array fan-out, password non-portability) plus the core RBAC lookup tables the rest of
/// the graph roots from. See spec/DATA-MIGRATION-NOTES.md "Table-by-table mapping status" for
/// the remaining tables not yet covered — they are a mechanical extension of the exact same
/// read-row → transform → resolve-FK-via-id-map → write-via-EF → record-mapping pattern used by
/// every migrator here, deliberately not hand-built individually within this engagement's scope.
/// </para>
/// </remarks>
public static class TableMigratorRegistry
{
    /// <summary>Gets every implemented table migrator, in FK-safe dependency order.</summary>
    /// <returns>The ordered list of migrators to run.</returns>
    public static IReadOnlyList<ITableMigrator> GetOrderedMigrators() => new ITableMigrator[]
    {
        // Identity / RBAC root tables -- everything else references users and/or roles.
        new RoleTableMigrator(),
        new PageTableMigrator(),
        new PermissionTableMigrator(),
        new UserTableMigrator(),
        new RolePermissionTableMigrator(),
        new UserRoleTableMigrator(),
        new UserPageOverrideTableMigrator(),
        new ActivityLogTableMigrator(),
        new SystemSettingTableMigrator(),

        // Feature-domain "hard case" tables demonstrated end-to-end in this engagement.
        new AiYearActivationTableMigrator(),
        new GacSocialPostTableMigrator(),

        // AI Year and GAC children -- both FK to their respective parent/sibling tables above.
        new AiYearMediaTableMigrator(),
        new AiYearMetricTableMigrator(),
        new GacPublicationTableMigrator(),
        new GacNewsItemTableMigrator(),

        // International Days domain -- InternationalDay is the root; DayYearlyTheme and
        // DayActivation both cascade-FK to it. IntlDaySource's related_id is polymorphic but in
        // practice only ever targets international_days, and IntlSearchHistory's day_id is an
        // unenforced/optional implied FK, so both must still follow InternationalDay to resolve.
        new InternationalDayTableMigrator(),
        new DayYearlyThemeTableMigrator(),
        new DayActivationTableMigrator(),
        new IntlDaySourceTableMigrator(),
        new IntlSearchHistoryTableMigrator(),

        // ShorfahIssue is the top-level workflow container; ShorfahSection FKs to it (issue_id)
        // and to Users for the four actor FKs, so issues must be migrated first.
        new ShorfahIssueTableMigrator(),
        new ShorfahSectionTableMigrator(),

        // Section-scoped children -- all FK to ShorfahSection, some also to Users.
        // ShorfahAssignment must precede ShorfahReminder (optional assignment_id FK).
        new ShorfahSectionPermissionTableMigrator(),
        new ShorfahSectionMediaTableMigrator(),
        new ShorfahWorkflowLogTableMigrator(),
        new ShorfahAssignmentTableMigrator(),
        new ShorfahReminderTableMigrator(),

        // Natural-key (section_type) template config -- independent of any issue/section row.
        new ShorfahSectionSlaDefaultTableMigrator(),

        // Notifications reference Users (required) plus optional Issue/Section -- must run last
        // in the Shorfah group so both optional FKs can resolve.
        new ShorfahNotificationTableMigrator(),

        // Week-start content-generation domain -- ArchiveEntry has no FKs; GeneratedOutput
        // re-points its archive_refs list through ArchiveEntry's id mapping, so it must follow.
        new ArchiveEntryTableMigrator(),
        new StyleProfileTableMigrator(),
        new GeneratedOutputTableMigrator(),

        // Weekend content domain -- WeekendPlace has no FKs; WeekendDraft optionally FKs to
        // Users (generated_by/approved_by), already migrated above.
        new WeekendPlaceTableMigrator(),
        new WeekendDraftTableMigrator(),

        // Designs domain -- DesignTemplate has no FKs; BrandLogo/BrandFont have no FKs;
        // GeneratedDesign optionally FKs to DesignTemplate, Users, and re-points its
        // selected_logos id list through BrandLogo's id mapping, so it must run last.
        new DesignTemplateTableMigrator(),
        new BrandLogoTableMigrator(),
        new BrandFontTableMigrator(),
        new GeneratedDesignTableMigrator(),

        // Media-monitoring domain -- MediaReport and PromptFramework both optionally FK to
        // Users only. ReportsQaQuery optionally FKs to Users and to final_media_reports; the
        // latter has no migrator yet (see spec/DATA-MIGRATION-NOTES.md), so every migrated
        // ReportsQaQuery row's FinalReportId will resolve to null until that gap is closed.
        new MediaReportTableMigrator(),
        new PromptFrameworkTableMigrator(),
        new ReportsQaQueryTableMigrator(),

        // Daily Reports domain -- daily_reports has no FKs to any other table, so it is
        // independent of ordering relative to every other migrator above.
        new DailyReportTableMigrator(),
    };
}
