namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.EditorialTone"/>.</summary>
public sealed record EditorialToneDto(
    IReadOnlyList<EditorialToneBucketDto> Distribution, IReadOnlyList<EditorialToneBucketDto> Classification, IReadOnlyList<EditorialToneBucketDto> Sources);
