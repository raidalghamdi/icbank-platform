namespace Icbank.Platform.Domain.Campaigns;

/// <summary>Where an internal campaign request is in the Corporate Communications review.</summary>
public enum CampaignRequestStatus
{
    /// <summary>Submitted by the department and waiting for review.</summary>
    Submitted = 0,

    /// <summary>Approved and converted into a campaign.</summary>
    Approved = 1,

    /// <summary>Returned to the department with a note.</summary>
    Rejected = 2,
}
