namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /weekend/drafts/:id/reject</c>.</summary>
public sealed class RejectWeekendDraftRequest
{
    /// <summary>Gets or sets the optional rejection reason.</summary>
    public string? Reason { get; set; }
}
