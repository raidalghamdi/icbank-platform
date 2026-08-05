namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /weekend/generate</c>.</summary>
public sealed class GenerateWeekendDraftRequest
{
    /// <summary>Gets or sets the optional target weekend date override.</summary>
    public string? WeekendDate { get; set; }
}
