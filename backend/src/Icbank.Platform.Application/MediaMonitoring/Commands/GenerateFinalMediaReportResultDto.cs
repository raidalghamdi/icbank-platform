namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Result of generating a final-report draft. Exactly one of <see cref="Draft"/> or <see cref="NoSourceData"/> is populated.</summary>
/// <param name="Draft">The generated 8-section draft, populated only when source data existed.</param>
/// <param name="NoSourceData">The no-data diagnostic payload (BUSINESS-RULES.md §5.3's <c>NO_SOURCE_DATA</c> guard), populated only when the range had zero posts and zero news.</param>
public sealed record GenerateFinalMediaReportResultDto(FinalReportDraftDto? Draft, NoSourceDataDto? NoSourceData);
