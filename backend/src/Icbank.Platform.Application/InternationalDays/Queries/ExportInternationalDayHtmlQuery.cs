using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Queries;

/// <summary>
/// Ports <c>GET /intl-days/export/:id</c> (API-SURFACE.md §14): a Word-compatible HTML export.
/// Closes DEFECT-LOG.md SEC-21/H-1 -- the Node source interpolated AI-generated content
/// (<c>historySummary</c>, activation descriptions, suggestions, source titles/URLs) directly
/// into the HTML document with zero escaping, a stored-XSS risk if the AI ever returns
/// <c>&lt;script&gt;</c> content. This port's handler builds the document exclusively through
/// <see cref="InternationalDayHtmlExportBuilder"/>, which HTML-encodes every interpolated value
/// via <see cref="System.Net.WebUtility.HtmlEncode(string?)"/> -- never raw string interpolation.
/// </summary>
/// <param name="DayId">The day id to export.</param>
public sealed record ExportInternationalDayHtmlQuery(int DayId) : IRequest<Result<InternationalDayHtmlExportDto>>;
