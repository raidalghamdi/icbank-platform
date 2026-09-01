namespace Icbank.Platform.Application.Projects;

/// <summary>
/// One tracked project, already carrying every value the page needs to draw a card. Health,
/// schedule position and the milestone tally are computed server-side so the browser renders the
/// portfolio in a single pass instead of recalculating per card.
/// </summary>
/// <param name="Id">The project identifier.</param>
/// <param name="Code">The short reference, e.g. <c>OPS-01</c>.</param>
/// <param name="Name">The project name.</param>
/// <param name="Description">The one-line description.</param>
/// <param name="Category">The portfolio bucket key, <c>operational</c> or <c>strategic</c>.</param>
/// <param name="CategoryLabel">The Arabic label for the bucket.</param>
/// <param name="Stage">The lifecycle stage key.</param>
/// <param name="StageLabel">The Arabic label for the stage.</param>
/// <param name="Health">The tracking signal key.</param>
/// <param name="HealthLabel">The Arabic label for the tracking signal.</param>
/// <param name="Owner">The person accountable for delivery.</param>
/// <param name="Department">The owning organisational unit.</param>
/// <param name="ProgressPercent">The reported completion percentage.</param>
/// <param name="ExpectedProgressPercent">The percentage the schedule says should be done by now.</param>
/// <param name="TeamSize">The number of people assigned.</param>
/// <param name="StartDate">The UTC start date.</param>
/// <param name="DueDate">The UTC due date.</param>
/// <param name="DaysRemaining">Days left until the due date; negative once it has passed.</param>
/// <param name="LatestUpdate">The latest progress note.</param>
/// <param name="MilestonesCompleted">How many checkpoints are delivered.</param>
/// <param name="MilestonesTotal">How many checkpoints the project has.</param>
/// <param name="Milestones">The checkpoints themselves, in display order.</param>
public sealed record PortfolioProjectDto(
    int Id,
    string Code,
    string Name,
    string Description,
    string Category,
    string CategoryLabel,
    string Stage,
    string StageLabel,
    string Health,
    string HealthLabel,
    string Owner,
    string Department,
    int ProgressPercent,
    int ExpectedProgressPercent,
    int TeamSize,
    DateTime StartDate,
    DateTime DueDate,
    int DaysRemaining,
    string LatestUpdate,
    int MilestonesCompleted,
    int MilestonesTotal,
    IReadOnlyList<ProjectMilestoneDto> Milestones);
