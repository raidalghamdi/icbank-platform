namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>The save outcome.</summary>
/// <param name="Id">The upserted day's id.</param>
/// <param name="Day">The saved day.</param>
public sealed record SaveInternationalDayResultDto(int Id, InternationalDayDto Day);
