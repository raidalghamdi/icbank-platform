namespace Icbank.Platform.Domain.Gac;

/// <summary>Where a GAC publication was sourced from (DATA-MODEL.md section 5).</summary>
public enum GacPublicationSourceDomain
{
    /// <summary>Extracted from gacbep.gac.gov.sa via Wayback Machine.</summary>
    Gacbep = 0,

    /// <summary>Sourced from ACNBE.</summary>
    Acnbe = 1,

    /// <summary>Sourced from UNESCWA.</summary>
    Unescwa = 2,

    /// <summary>Sourced directly from the origin site.</summary>
    Direct = 3,

    /// <summary>Manually uploaded.</summary>
    Manual = 4,
}
