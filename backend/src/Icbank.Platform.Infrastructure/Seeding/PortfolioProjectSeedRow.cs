using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>A single project in <see cref="PortfolioProjectSeedCatalog"/>.</summary>
/// <param name="Code">The short reference, used as the natural key when seeding.</param>
/// <param name="Name">The project name.</param>
/// <param name="Description">The one-line description.</param>
/// <param name="Category">The portfolio bucket.</param>
/// <param name="Stage">The lifecycle stage.</param>
/// <param name="Owner">The person accountable for delivery.</param>
/// <param name="Department">The owning organisational unit.</param>
/// <param name="ProgressPercent">The reported completion percentage.</param>
/// <param name="TeamSize">The number of people assigned.</param>
/// <param name="StartOffsetDays">Days from the seed instant to the start date; negative for past dates.</param>
/// <param name="DueOffsetDays">Days from the seed instant to the due date.</param>
/// <param name="LatestUpdate">The latest progress note.</param>
/// <param name="SortOrder">The display order within the bucket.</param>
/// <param name="Milestones">The delivery checkpoints.</param>
internal sealed record PortfolioProjectSeedRow(
    string Code,
    string Name,
    string Description,
    ProjectCategory Category,
    ProjectStage Stage,
    string Owner,
    string Department,
    int ProgressPercent,
    int TeamSize,
    int StartOffsetDays,
    int DueOffsetDays,
    string LatestUpdate,
    int SortOrder,
    IReadOnlyList<PortfolioMilestoneSeedRow> Milestones);
