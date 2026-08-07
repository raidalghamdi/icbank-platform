namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PUT /api/v1/admin/settings</c>.</summary>
/// <param name="Settings">The key/value pairs to upsert, validated against the settings whitelist.</param>
public sealed record UpdateSystemSettingsRequest(IReadOnlyDictionary<string, string> Settings);
