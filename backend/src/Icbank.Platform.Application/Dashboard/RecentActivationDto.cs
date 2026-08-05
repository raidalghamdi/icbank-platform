namespace Icbank.Platform.Application.Dashboard;

/// <summary>A recently-created AI Year activation, as surfaced on the dashboard.</summary>
/// <param name="Id">The activation id.</param>
/// <param name="Title">The activation title.</param>
/// <param name="Type">The free-text activation type.</param>
/// <param name="Status">The publication status.</param>
/// <param name="ActivationDate">The free-text activation date as captured by the source system.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
public sealed record RecentActivationDto(int Id, string Title, string Type, string Status, string? ActivationDate, DateTime CreatedAt);
