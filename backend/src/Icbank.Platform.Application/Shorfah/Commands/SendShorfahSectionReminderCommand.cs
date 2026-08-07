using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/remind</c> (BUSINESS-RULES.md §1.7). Ports <c>shorfah.ts:957-994</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="SectionId">The section the reminder concerns.</param>
/// <param name="UserId">The single recipient user's id.</param>
public sealed record SendShorfahSectionReminderCommand(int ActorUserId, int SectionId, int UserId) : IRequest<Result<bool>>;
