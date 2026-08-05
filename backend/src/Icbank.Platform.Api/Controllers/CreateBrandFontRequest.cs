namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="DesignsController.CreateFontAsync"/>.</summary>
/// <param name="FontName">The font's display name.</param>
/// <param name="FontFileUrl">The already-uploaded object path.</param>
public sealed record CreateBrandFontRequest(string FontName, string FontFileUrl);
