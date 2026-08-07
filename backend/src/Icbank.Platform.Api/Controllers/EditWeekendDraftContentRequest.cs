using System.Text.Json;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PATCH /weekend/drafts/:id</c>.</summary>
public sealed class EditWeekendDraftContentRequest
{
    /// <summary>Gets or sets the replacement content payload.</summary>
    public JsonElement Content { get; set; }
}
