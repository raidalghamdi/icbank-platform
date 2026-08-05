namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.DeepAnalysisQuote"/>.</summary>
public sealed record DeepAnalysisQuoteDto(string Text, string Source, string Date);
