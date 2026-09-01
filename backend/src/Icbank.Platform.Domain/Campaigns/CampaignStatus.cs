namespace Icbank.Platform.Domain.Campaigns;

/// <summary>
/// Where a campaign sits in its lifecycle. These are the four states the campaigns pages filter
/// on, in the order the department reads them: what is live now, what is coming, what is waiting
/// on an approval, and what is closed.
/// </summary>
public enum CampaignStatus
{
    /// <summary>Live and publishing — «قائمة».</summary>
    Running = 0,

    /// <summary>Approved and scheduled but not started — «قادمة».</summary>
    Upcoming = 1,

    /// <summary>Content or results are with a reviewer — «تحت المراجعة».</summary>
    UnderReview = 2,

    /// <summary>Delivered and closed out — «مكتملة».</summary>
    Completed = 3,
}
