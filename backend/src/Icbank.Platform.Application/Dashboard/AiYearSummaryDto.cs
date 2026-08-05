namespace Icbank.Platform.Application.Dashboard;

/// <summary>AI Year-specific dashboard counters.</summary>
/// <param name="TotalActivations">Total activation count.</param>
/// <param name="RecentActivations">The 5 most recently created activations.</param>
public sealed record AiYearSummaryDto(int TotalActivations, IReadOnlyList<RecentActivationDto> RecentActivations);
