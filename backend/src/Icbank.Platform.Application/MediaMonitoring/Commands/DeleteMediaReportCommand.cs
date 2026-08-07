using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Deletes an editable media-monitoring report (<c>DELETE /media-reports/:id</c>).</summary>
/// <param name="ActorUserId">The id of the authenticated caller performing the deletion.</param>
/// <param name="ReportId">The report id to delete.</param>
public sealed record DeleteMediaReportCommand(int ActorUserId, int ReportId) : IRequest<Result<bool>>;
