namespace Icbank.Platform.Application.MediaMonitoring.Appearance;

/// <summary>One social platform's measured activity, emitted only for platforms that actually have monitored posts.</summary>
/// <param name="Name">The platform's Arabic display name.</param>
/// <param name="Posts">The number of monitored posts on that platform.</param>
/// <param name="Engagement">The summed likes, comments and shares carried on those posts.</param>
/// <param name="Reposts">The summed share count carried on those posts.</param>
public sealed record MediaAppearancePlatformDto(string Name, int Posts, int Engagement, int Reposts);
