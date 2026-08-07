namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.DeepAnalysis"/>.</summary>
public sealed record DeepAnalysisDto(
    IReadOnlyList<DeepAnalysisKeywordDto> Keywords, DeepAnalysisQuoteDto? Quote, IReadOnlyList<string> Strengths, IReadOnlyList<string> Weaknesses);
