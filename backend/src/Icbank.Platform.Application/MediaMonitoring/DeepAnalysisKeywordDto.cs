namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.DeepAnalysisKeyword"/>.</summary>
public sealed record DeepAnalysisKeywordDto(string Keyword, int Frequency, string Context);
