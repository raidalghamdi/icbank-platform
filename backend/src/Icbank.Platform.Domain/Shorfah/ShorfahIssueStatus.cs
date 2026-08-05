namespace Icbank.Platform.Domain.Shorfah;

/// <summary>State machine for a magazine issue: collecting -> in_review -> published (DATA-MODEL.md section 5).</summary>
public enum ShorfahIssueStatus
{
    /// <summary>Contributions are being collected.</summary>
    Collecting = 0,

    /// <summary>The issue is under editorial review.</summary>
    InReview = 1,

    /// <summary>The issue has been published.</summary>
    Published = 2,
}
