namespace Icbank.Platform.Application.Admin;

/// <summary>One user's row in the effective permission matrix.</summary>
/// <param name="UserId">The user's id.</param>
/// <param name="Email">The user's email.</param>
/// <param name="RoleNames">The union of role machine-names the user holds.</param>
/// <param name="Grants">Page slug → the verb names effectively granted (role grants unioned, then overrides applied).</param>
public sealed record UserEffectivePermissionsDto(
    int UserId,
    string Email,
    IReadOnlyCollection<string> RoleNames,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> Grants);
