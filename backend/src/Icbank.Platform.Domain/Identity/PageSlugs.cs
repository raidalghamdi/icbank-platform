namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// The 18 seeded RBAC page slugs (DOTNET-CONVENTIONS.md §5.4, BUSINESS-RULES.md §10.3). Centralized
/// as named constants so no controller/handler scatters raw string literals (R-BE-095).
/// </summary>
public static class PageSlugs
{
    /// <summary>The dashboard landing page.</summary>
    public const string Dashboard = "dashboard";

    /// <summary>Internal requests workflow.</summary>
    public const string InternalRequests = "internal_requests";

    /// <summary>Internal campaigns workflow.</summary>
    public const string InternalCampaigns = "internal_campaigns";

    /// <summary>Weekend content cycle.</summary>
    public const string Weekend = "weekend";

    /// <summary>Week Start content cycle.</summary>
    public const string WeekStart = "weekstart";

    /// <summary>International Days feature.</summary>
    public const string InternationalDays = "international_days";

    /// <summary>External requests workflow.</summary>
    public const string ExternalRequests = "external_requests";

    /// <summary>External campaigns workflow.</summary>
    public const string ExternalCampaigns = "external_campaigns";

    /// <summary>Shorfah magazine workflow.</summary>
    public const string Shorfah = "shorfah";

    /// <summary>Media monitoring feature.</summary>
    public const string MediaMonitoring = "media_monitoring";

    /// <summary>World news feed.</summary>
    public const string WorldNews = "world_news";

    /// <summary>Performance reports.</summary>
    public const string PerformanceReports = "performance_reports";

    /// <summary>Initiatives tracker.</summary>
    public const string Initiatives = "initiatives";

    /// <summary>AI Year 2026 campaign tracker.</summary>
    public const string AiYear = "ai_year";

    /// <summary>Design studio / composer.</summary>
    public const string DesignStudio = "design_studio";

    /// <summary>Smart assistant feature.</summary>
    public const string SmartAssistant = "smart_assistant";

    /// <summary>Admin panel — user/role/permission management.</summary>
    public const string AdminPanel = "admin_panel";

    /// <summary>System settings.</summary>
    public const string Settings = "settings";

    /// <summary>Gets every seeded page slug, in seed order.</summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        Dashboard, InternalRequests, InternalCampaigns, Weekend, WeekStart, InternationalDays,
        ExternalRequests, ExternalCampaigns, Shorfah, MediaMonitoring, WorldNews,
        PerformanceReports, Initiatives, AiYear, DesignStudio, SmartAssistant, AdminPanel, Settings,
    };
}
