namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /week-start/approve</c>.</summary>
public sealed class ApproveGeneratedOutputRequest
{
    /// <summary>Gets or sets the generated output id.</summary>
    public int Id { get; set; }
}
