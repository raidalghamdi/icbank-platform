using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Ports <c>POST /shorfah/issues/:id/sections</c> (API-SURFACE.md §19). Admin-only. Adds one custom section to an issue.</summary>
/// <param name="ActorUserId">The admin's id.</param>
/// <param name="IssueId">The owning issue's id.</param>
/// <param name="SectionType">The section type.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="DisplayOrder">The optional display order, defaulted to 0.</param>
/// <param name="OwnerUserId">The optional owning user's id.</param>
/// <param name="OwnerRole">The optional owning role name.</param>
/// <param name="AutoGenerate">Whether the section content is AI-auto-generated.</param>
/// <param name="GenerationPrompt">An optional custom AI prompt override.</param>
/// <param name="ParentSectionId">The optional parent section's id, for sub-sections.</param>
/// <param name="SlaDays">An explicit SLA-day override, or <c>null</c> to use the per-type default.</param>
public sealed record AddShorfahSectionCommand(
    int ActorUserId,
    int IssueId,
    string SectionType,
    string TitleAr,
    string? DescriptionAr,
    int? DisplayOrder,
    int? OwnerUserId,
    string? OwnerRole,
    bool AutoGenerate,
    string? GenerationPrompt,
    int? ParentSectionId,
    int? SlaDays) : IRequest<Result<ShorfahSectionDto>>;
