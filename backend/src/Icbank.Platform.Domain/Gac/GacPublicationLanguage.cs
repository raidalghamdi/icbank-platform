namespace Icbank.Platform.Domain.Gac;

/// <summary>Primary language of a GAC publication (DATA-MODEL.md section 5).</summary>
public enum GacPublicationLanguage
{
    /// <summary>Arabic.</summary>
    Ar = 0,

    /// <summary>English.</summary>
    En = 1,

    /// <summary>Both Arabic and English.</summary>
    Both = 2,
}
