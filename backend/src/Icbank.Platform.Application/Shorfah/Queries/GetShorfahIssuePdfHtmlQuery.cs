using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Ports <c>GET /shorfah/issues/:id/pdf</c> (API-SURFACE.md §19, BUSINESS-RULES.md §1.9): the HTML preview of the issue PDF.</summary>
/// <param name="IssueId">The issue being exported.</param>
/// <param name="Preview">When <c>true</c>, includes every <c>IncludeInPdf</c> section regardless of approval; when <c>false</c>, only approved+included sections.</param>
public sealed record GetShorfahIssuePdfHtmlQuery(int IssueId, bool Preview) : IRequest<Result<string>>;
