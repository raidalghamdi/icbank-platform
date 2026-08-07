using Icbank.Platform.Application.Dashboard;
using Icbank.Platform.Infrastructure.Seeding;

namespace Icbank.Platform.UnitTests.Infrastructure;

/// <summary>
/// Guards the seeded observance catalogue.
/// <para>
/// The dashboard silently skips any day whose <c>AnnualDate</c> it cannot parse
/// (<see cref="ArabicAnnualDateParser"/> returns null and the row is dropped from "upcoming
/// events"). A typo in a month name therefore produces no error anywhere — the day simply never
/// appears, which is exactly the failure this catalogue exists to end. These tests run the real
/// parser over every row so a bad date fails the build instead.
/// </para>
/// </summary>
public sealed class InternationalDaySeedCatalogTests
{
    /// <summary>Every seeded date must survive the parser the dashboard actually uses.</summary>
    [Fact]
    public void EveryAnnualDateIsParseableByTheDashboardParser()
    {
        var unparseable = InternationalDaySeedCatalog.Rows
            .Where(r => ArabicAnnualDateParser.Parse(r.AnnualDate) is null)
            .Select(r => $"{r.NameAr} => '{r.AnnualDate}'")
            .ToList();

        Assert.True(
            unparseable.Count == 0,
            "These rows would be dropped from the dashboard without any error: " + string.Join("; ", unparseable));
    }

    /// <summary>Parsed month and day must be a real calendar date.</summary>
    [Fact]
    public void EveryAnnualDateIsARealCalendarDate()
    {
        foreach (InternationalDaySeedRow row in InternationalDaySeedCatalog.Rows)
        {
            (int Month, int Day)? parsed = ArabicAnnualDateParser.Parse(row.AnnualDate);
            Assert.NotNull(parsed);

            (var month, var day) = parsed!.Value;
            Assert.InRange(month, 1, 12);

            // 2024 is a leap year, so 29 February is accepted here rather than rejected as a
            // typo. The dashboard's own ResolveNextOccurrence handles the common-year case.
            Assert.InRange(day, 1, DateTime.DaysInMonth(2024, month));
        }
    }

    /// <summary>
    /// The seeder matches existing rows on the Arabic name, so a duplicate name in the catalogue
    /// would silently seed only one of the pair.
    /// </summary>
    [Fact]
    public void ArabicNamesAreUniqueBecauseTheSeederKeysOnThem()
    {
        var duplicates = InternationalDaySeedCatalog.Rows
            .GroupBy(r => r.NameAr, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0, "Duplicate Arabic names: " + string.Join("; ", duplicates));
    }

    /// <summary>Each row must carry an organiser and a citable source, since this ships as fact.</summary>
    [Fact]
    public void EveryRowIsAttributedToASource()
    {
        foreach (InternationalDaySeedRow row in InternationalDaySeedCatalog.Rows)
        {
            Assert.False(string.IsNullOrWhiteSpace(row.Organizer), $"{row.NameAr} has no organiser");
            Assert.False(string.IsNullOrWhiteSpace(row.History), $"{row.NameAr} has no history note");
            Assert.StartsWith("https://", row.OrganizerSource, StringComparison.Ordinal);
        }
    }

    /// <summary>The catalogue must actually be populated, or the landing page stays empty.</summary>
    [Fact]
    public void CatalogueIsNotEmpty()
    {
        Assert.NotEmpty(InternationalDaySeedCatalog.Rows);
    }
}
