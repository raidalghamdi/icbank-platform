using Icbank.Platform.Application.Gac.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="GacController.ReseedPublicationsAsync"/>.</summary>
/// <param name="Publications">The publication metadata rows to idempotently insert.</param>
public sealed record ReseedGacPublicationsRequest(IReadOnlyList<ReseedGacPublicationItem> Publications);
