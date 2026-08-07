using Icbank.Platform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Icbank.Platform.Infrastructure.Authorization;

/// <summary>
/// The single requirement type behind every generated <c>{pageSlug}:{verb}</c> policy
/// (DOTNET-CONVENTIONS.md §5.4) — one requirement + one handler backs all 72 policies, so a
/// controller declares only <c>[Authorize(Policy = "shorfah:edit")]</c> and nothing else.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>Initializes a new instance of the <see cref="PermissionRequirement"/> class.</summary>
    /// <param name="pageSlug">The page slug this requirement gates.</param>
    /// <param name="verb">The action verb this requirement gates.</param>
    public PermissionRequirement(string pageSlug, PermissionVerb verb)
    {
        PageSlug = pageSlug;
        Verb = verb;
    }

    /// <summary>Gets the page slug this requirement gates.</summary>
    public string PageSlug { get; }

    /// <summary>Gets the action verb this requirement gates.</summary>
    public PermissionVerb Verb { get; }
}
