namespace Icbank.Platform.Domain.Gac;

/// <summary>Category of a GAC publication (DATA-MODEL.md section 5).</summary>
public enum GacPublicationCategory
{
    /// <summary>Guideline documents.</summary>
    Guidelines = 0,

    /// <summary>Regulatory documents.</summary>
    Regulations = 1,

    /// <summary>Statistical reports.</summary>
    Statistics = 2,

    /// <summary>Research papers.</summary>
    Research = 3,

    /// <summary>Brand/identity guidelines.</summary>
    Brand = 4,

    /// <summary>Policy documents.</summary>
    Policy = 5,
}
