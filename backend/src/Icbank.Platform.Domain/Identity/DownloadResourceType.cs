namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// The fixed set of resources a <see cref="DownloadToken"/> may be scoped to. Deliberately a
/// closed enum, not a free-text string, so a token minted for one resource family can never be
/// redeemed against another family's endpoint even if the numeric id happens to collide.
/// </summary>
public enum DownloadResourceType
{
    /// <summary>A single Shorfah issue's PDF (preview HTML or binary download — both share one token).</summary>
    ShorfahIssuePdf = 0,

    /// <summary>A single international day's HTML export.</summary>
    InternationalDayExport = 1,
}
