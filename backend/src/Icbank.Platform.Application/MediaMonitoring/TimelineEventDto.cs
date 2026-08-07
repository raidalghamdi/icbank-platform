namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.TimelineEvent"/>.</summary>
public sealed record TimelineEventDto(string Date, string Event, string Outlet, string Tone, int Count);
