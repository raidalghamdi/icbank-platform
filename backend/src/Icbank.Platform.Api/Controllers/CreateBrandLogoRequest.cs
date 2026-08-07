namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="DesignsController.CreateLogoAsync"/>.</summary>
/// <param name="LogoName">The logo's display name.</param>
/// <param name="FileUrl">The already-uploaded object path.</param>
/// <param name="Transparent">Whether the logo has a transparent background.</param>
/// <param name="DefaultWidth">The optional default render width.</param>
public sealed record CreateBrandLogoRequest(string LogoName, string FileUrl, bool Transparent, int? DefaultWidth);
