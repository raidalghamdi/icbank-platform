namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Resolves a user's effective permission set from the three-layer model (BUSINESS-RULES.md
/// §10.1): role-union (closing the old system's <c>.limit(1)</c> single-role bug — every role a
/// user holds contributes its grants, unioned together), then per-user allow/deny overrides
/// applied on top (deny always wins over any role grant; allow always adds).
/// </summary>
public interface IPermissionResolver
{
    /// <summary>
    /// Computes the effective <c>{pageSlug}:{verb}</c> permission strings for a user, unioning
    /// every role the user holds and applying per-user overrides last.
    /// </summary>
    /// <param name="userId">The user to resolve permissions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective permission strings and the union of role machine-names held.</returns>
    Task<PermissionResolution> ResolveAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Checks whether a specific policy (page slug + verb) is granted to a user, without materializing the full set.</summary>
    /// <param name="userId">The user to check.</param>
    /// <param name="pageSlug">The page slug being accessed.</param>
    /// <param name="verb">The action verb being attempted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the effective permission set grants this policy.</returns>
    Task<bool> HasPermissionAsync(int userId, string pageSlug, Icbank.Platform.Domain.Identity.PermissionVerb verb, CancellationToken cancellationToken);
}
