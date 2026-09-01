namespace Icbank.Platform.Application.MediaMonitoring.Appearance;

/// <summary>One publishing outlet's measured share of the period's coverage.</summary>
/// <param name="Name">The outlet's display name as stored on the monitored item.</param>
/// <param name="Appearances">The number of monitored items published by this outlet.</param>
/// <param name="SharePercent">The outlet's share of all appearances, rounded to a whole percent.</param>
public sealed record MediaAppearanceOutletDto(string Name, int Appearances, int SharePercent);
