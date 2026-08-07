namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.QuoteAppendixItem"/>.</summary>
public sealed record QuoteAppendixItemDto(string Quote, string Source, string Date, string Topic);
