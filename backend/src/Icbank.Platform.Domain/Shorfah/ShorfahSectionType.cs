namespace Icbank.Platform.Domain.Shorfah;

/// <summary>The 13 canonical Shorfah section types (DATA-MODEL.md section 5).</summary>
public enum ShorfahSectionType
{
    /// <summary>Global news roundup.</summary>
    GlobalNews = 0,

    /// <summary>Local news roundup.</summary>
    News = 1,

    /// <summary>International participation coverage.</summary>
    IntlParticipation = 2,

    /// <summary>Our communications activities.</summary>
    OurComms = 3,

    /// <summary>Economic observatory section.</summary>
    EconomicObservatory = 4,

    /// <summary>System index section.</summary>
    SystemIndex = 5,

    /// <summary>Legal window section.</summary>
    LegalWindow = 6,

    /// <summary>Office interview feature.</summary>
    OfficeInterview = 7,

    /// <summary>Competition culture feature.</summary>
    CompetitionCulture = 8,

    /// <summary>Outside-the-box feature.</summary>
    OutsideBox = 9,

    /// <summary>Events roundup.</summary>
    Events = 10,

    /// <summary>Agency literature section.</summary>
    AgencyLit = 11,

    /// <summary>Employee Q&amp;A feature.</summary>
    EmployeeQa = 12,
}
