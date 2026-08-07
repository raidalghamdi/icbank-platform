namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="FinalMediaReportsController.SearchAsync"/>.</summary>
/// <param name="Mode">The search mode: <c>full</c> or <c>info</c>.</param>
/// <param name="Query">The free-text search/question text.</param>
/// <param name="Limit">The maximum number of reports to match.</param>
public sealed record SearchFinalMediaReportsRequest(string Mode, string Query, int? Limit);
