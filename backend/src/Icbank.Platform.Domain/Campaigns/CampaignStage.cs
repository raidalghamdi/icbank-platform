namespace Icbank.Platform.Domain.Campaigns;

/// <summary>
/// Where a campaign is in its operating lifecycle. A campaign only reaches <see cref="Closed"/>
/// after its performance has been measured, so the end of publishing never closes it by itself.
/// </summary>
public enum CampaignStage
{
    /// <summary>The plan, timeline and deliverables are being prepared.</summary>
    Planning = 0,

    /// <summary>The campaign is live and deliverables are being published.</summary>
    Execution = 1,

    /// <summary>Publishing has ended and results are being measured against the targets.</summary>
    Measurement = 2,

    /// <summary>Results are recorded and the campaign is closed.</summary>
    Closed = 3,
}
