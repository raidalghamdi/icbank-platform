using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Application.Projects;

/// <summary>
/// Maps the portfolio enums onto the machine keys the page filters on and the Arabic labels it
/// prints. Kept server-side so the page never ships a second copy of the vocabulary that can
/// drift away from the domain.
/// </summary>
public static class ProjectPortfolioLabels
{
    /// <summary>Gets the filter key for a portfolio bucket.</summary>
    /// <param name="category">The bucket.</param>
    /// <returns>A lowercase key.</returns>
    public static string CategoryKey(ProjectCategory category) => category switch
    {
        ProjectCategory.Strategic => "strategic",
        _ => "operational",
    };

    /// <summary>Gets the Arabic label for a portfolio bucket.</summary>
    /// <param name="category">The bucket.</param>
    /// <returns>The Arabic label.</returns>
    public static string CategoryLabel(ProjectCategory category) => category switch
    {
        ProjectCategory.Strategic => "استراتيجي",
        _ => "تشغيلي",
    };

    /// <summary>Gets the filter key for a lifecycle stage.</summary>
    /// <param name="stage">The stage.</param>
    /// <returns>A lowercase key.</returns>
    public static string StageKey(ProjectStage stage) => stage switch
    {
        ProjectStage.Planning => "planning",
        ProjectStage.OnHold => "on_hold",
        ProjectStage.Completed => "completed",
        _ => "in_progress",
    };

    /// <summary>Gets the Arabic label for a lifecycle stage.</summary>
    /// <param name="stage">The stage.</param>
    /// <returns>The Arabic label.</returns>
    public static string StageLabel(ProjectStage stage) => stage switch
    {
        ProjectStage.Planning => "تخطيط",
        ProjectStage.OnHold => "متوقف مؤقتاً",
        ProjectStage.Completed => "مكتمل",
        _ => "قيد التنفيذ",
    };

    /// <summary>Gets the filter key for a tracking signal.</summary>
    /// <param name="health">The signal.</param>
    /// <returns>A lowercase key.</returns>
    public static string HealthKey(ProjectHealth health) => health switch
    {
        ProjectHealth.AtRisk => "at_risk",
        ProjectHealth.Delayed => "delayed",
        ProjectHealth.Completed => "completed",
        _ => "on_track",
    };

    /// <summary>Gets the Arabic label for a tracking signal.</summary>
    /// <param name="health">The signal.</param>
    /// <returns>The Arabic label.</returns>
    public static string HealthLabel(ProjectHealth health) => health switch
    {
        ProjectHealth.AtRisk => "بحاجة إلى المتابعة",
        ProjectHealth.Delayed => "متأخر",
        ProjectHealth.Completed => "مكتمل",
        _ => "على المسار",
    };
}
