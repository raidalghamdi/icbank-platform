namespace Icbank.Platform.Application.Projects;

/// <summary>The whole projects page payload: one request, no follow-up round trips.</summary>
/// <param name="Kpis">The portfolio headline figures.</param>
/// <param name="Projects">The tracked projects, strategic first then operational, each in sort order.</param>
/// <param name="GeneratedAt">The UTC instant the payload was computed.</param>
public sealed record ProjectPortfolioDto(
    ProjectPortfolioKpisDto Kpis,
    IReadOnlyList<PortfolioProjectDto> Projects,
    DateTime GeneratedAt);
