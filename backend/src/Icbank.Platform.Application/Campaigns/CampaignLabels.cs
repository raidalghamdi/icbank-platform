using Icbank.Platform.Domain.Campaigns;

namespace Icbank.Platform.Application.Campaigns;

/// <summary>
/// Maps the campaign enums onto the machine keys the pages filter on and the Arabic labels they
/// print. Kept server-side so the browser never ships a second copy of the vocabulary that can
/// drift away from the domain.
/// </summary>
public static class CampaignLabels
{
    /// <summary>Gets the filter key for an audience.</summary>
    /// <param name="audience">The audience.</param>
    /// <returns>A lowercase key, <c>internal</c> or <c>external</c>.</returns>
    public static string AudienceKey(CampaignAudience audience) => audience switch
    {
        CampaignAudience.External => "external",
        _ => "internal",
    };

    /// <summary>Gets the Arabic label for an audience.</summary>
    /// <param name="audience">The audience.</param>
    /// <returns>The Arabic label.</returns>
    public static string AudienceLabel(CampaignAudience audience) => audience switch
    {
        CampaignAudience.External => "خارجية",
        _ => "داخلية",
    };

    /// <summary>Gets the filter key for a lifecycle state.</summary>
    /// <param name="status">The state.</param>
    /// <returns>A lowercase key.</returns>
    public static string StatusKey(CampaignStatus status) => status switch
    {
        CampaignStatus.Upcoming => "upcoming",
        CampaignStatus.UnderReview => "under_review",
        CampaignStatus.Completed => "completed",
        _ => "running",
    };

    /// <summary>Gets the Arabic label for a lifecycle state.</summary>
    /// <param name="status">The state.</param>
    /// <returns>The Arabic label.</returns>
    public static string StatusLabel(CampaignStatus status) => status switch
    {
        CampaignStatus.Upcoming => "قادمة",
        CampaignStatus.UnderReview => "تحت المراجعة",
        CampaignStatus.Completed => "مكتملة",
        _ => "قائمة",
    };

    /// <summary>Parses an audience filter key coming off the query string.</summary>
    /// <param name="key">The key, case-insensitive; <c>internal</c> or <c>external</c>.</param>
    /// <returns>The parsed audience, or <c>null</c> when the key is not recognised.</returns>
    public static CampaignAudience? ParseAudience(string? key) => key?.Trim().ToUpperInvariant() switch
    {
        "INTERNAL" => CampaignAudience.Internal,
        "EXTERNAL" => CampaignAudience.External,
        _ => null,
    };

    /// <summary>Parses a status filter key coming off the query string.</summary>
    /// <param name="key">The key, case-insensitive; <c>all</c> and empty both mean no filter.</param>
    /// <returns>The parsed state, or <c>null</c> when no status filter should be applied.</returns>
    public static CampaignStatus? ParseStatus(string? key) => key?.Trim().ToUpperInvariant() switch
    {
        "RUNNING" => CampaignStatus.Running,
        "UPCOMING" => CampaignStatus.Upcoming,
        "UNDER_REVIEW" => CampaignStatus.UnderReview,
        "COMPLETED" => CampaignStatus.Completed,
        _ => null,
    };

    /// <summary>Returns the stable key of a lifecycle stage.</summary>
    /// <param name="stage">The stage.</param>
    /// <returns>The key.</returns>
    public static string StageKey(CampaignStage stage) => stage switch
    {
        CampaignStage.Execution => "execution",
        CampaignStage.Measurement => "measurement",
        CampaignStage.Closed => "closed",
        _ => "planning",
    };

    /// <summary>Returns the Arabic label of a lifecycle stage.</summary>
    /// <param name="stage">The stage.</param>
    /// <returns>The label.</returns>
    public static string StageLabel(CampaignStage stage) => stage switch
    {
        CampaignStage.Execution => "التنفيذ",
        CampaignStage.Measurement => "قياس الأداء",
        CampaignStage.Closed => "الإغلاق",
        _ => "التخطيط",
    };

    /// <summary>Parses a stage key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The stage, or null for an unknown key.</returns>
    public static CampaignStage? ParseStage(string? key) => key?.Trim().ToUpperInvariant() switch
    {
        "PLANNING" => CampaignStage.Planning,
        "EXECUTION" => CampaignStage.Execution,
        "MEASUREMENT" => CampaignStage.Measurement,
        "CLOSED" => CampaignStage.Closed,
        _ => null,
    };

    /// <summary>
    /// Returns the board bucket a stage is listed under: planning is upcoming, execution is
    /// running, measurement is under review, and only a closed campaign counts as completed.
    /// </summary>
    /// <param name="stage">The stage.</param>
    /// <returns>The board status.</returns>
    public static CampaignStatus StatusOf(CampaignStage stage) => stage switch
    {
        CampaignStage.Execution => CampaignStatus.Running,
        CampaignStage.Measurement => CampaignStatus.UnderReview,
        CampaignStage.Closed => CampaignStatus.Completed,
        _ => CampaignStatus.Upcoming,
    };
}
