namespace Icbank.Platform.Application.Projects;

/// <summary>The portfolio headline figures shown above the project cards.</summary>
/// <param name="Total">Total tracked projects.</param>
/// <param name="Operational">Projects in the operational bucket.</param>
/// <param name="Strategic">Projects in the strategic bucket.</param>
/// <param name="AverageProgressPercent">Mean completion across the portfolio.</param>
/// <param name="OnTrack">Projects keeping up with their schedule.</param>
/// <param name="AtRisk">Projects drifting behind or paused.</param>
/// <param name="Delayed">Projects past their due date or far behind.</param>
/// <param name="Completed">Projects delivered and closed.</param>
/// <param name="DueWithin30Days">Open projects due inside the next 30 days.</param>
public sealed record ProjectPortfolioKpisDto(
    int Total,
    int Operational,
    int Strategic,
    int AverageProgressPercent,
    int OnTrack,
    int AtRisk,
    int Delayed,
    int Completed,
    int DueWithin30Days);
