using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Ports <c>GET /shorfah/issues/:id/pdf.pdf</c> (API-SURFACE.md §19, BUSINESS-RULES.md §1.9): the binary PDF download.</summary>
/// <param name="IssueId">The issue being exported.</param>
/// <param name="Preview">When <c>true</c>, includes every <c>IncludeInPdf</c> section regardless of approval; when <c>false</c>, only approved+included sections.</param>
public sealed record GetShorfahIssuePdfBinaryQuery(int IssueId, bool Preview) : IRequest<Result<byte[]>>;
