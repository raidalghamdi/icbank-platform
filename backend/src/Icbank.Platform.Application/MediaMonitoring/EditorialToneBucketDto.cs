namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.EditorialToneBucket"/>.</summary>
public sealed record EditorialToneBucketDto(string Label, double Percent, int Count);
