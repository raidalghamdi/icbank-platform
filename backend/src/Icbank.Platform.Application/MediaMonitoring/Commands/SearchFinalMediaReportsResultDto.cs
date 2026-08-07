namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Result of a <see cref="SearchFinalMediaReportsCommand"/>. Exactly one of <see cref="Reports"/> (full mode) or <see cref="Answer"/> (info mode) is populated.</summary>
/// <param name="Mode">The mode that was run.</param>
/// <param name="Reports">The matched report summaries, populated only in <c>full</c> mode.</param>
/// <param name="Answer">The AI-generated answer text, populated only in <c>info</c> mode.</param>
public sealed record SearchFinalMediaReportsResultDto(string Mode, IReadOnlyList<FinalMediaReportDto>? Reports, string? Answer);
