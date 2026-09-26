using Icbank.Platform.Application.MediaMonitoring.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>The Review &amp; Edit save body for <c>PUT /final-media-reports/{id}</c>.</summary>
/// <param name="Title">The edited report title.</param>
/// <param name="Draft">The edited report content.</param>
/// <param name="Layout">The section layout, Contact Center figures and reviewer-added sections, as a JSON object.</param>
public sealed record UpdateFinalMediaReportDraftRequest(string Title, FinalReportDraftDto Draft, System.Text.Json.JsonElement? Layout);
