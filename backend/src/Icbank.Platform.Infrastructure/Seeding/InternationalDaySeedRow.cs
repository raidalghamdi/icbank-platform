namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>A single observance in <see cref="InternationalDaySeedCatalog"/>.</summary>
/// <param name="NameAr">The Arabic day name, used as the natural key when seeding.</param>
/// <param name="NameEn">The English day name.</param>
/// <param name="AnnualDate">The fixed annual date in the Arabic "d MMMM" form.</param>
/// <param name="Category">The planning category shown on the dashboard.</param>
/// <param name="Organizer">The organisation that owns the observance.</param>
/// <param name="OrganizerSource">A citable URL for the organiser and date.</param>
/// <param name="History">A one-line origin note.</param>
internal sealed record InternationalDaySeedRow(
    string NameAr,
    string NameEn,
    string AnnualDate,
    string Category,
    string Organizer,
    string OrganizerSource,
    string History);
