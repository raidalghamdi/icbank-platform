namespace Icbank.Platform.Application.Auth;

/// <summary>
/// Validates a post-login redirect target against a configured allow-list (closes SEC-11: the
/// old system stored the <c>redirect</c> query param verbatim with no validation at all). Per the
/// task's explicit instruction, an invalid target is rejected outright — it is never "sanitized"
/// or coerced into something plausible.
/// </summary>
public static class RedirectTargetValidator
{
    /// <summary>The default target used whenever the caller supplies none or an invalid one.</summary>
    public const string DefaultTarget = "/";

    /// <summary>Validates a candidate redirect target against the allow-list.</summary>
    /// <param name="candidate">The caller-supplied redirect target, or <c>null</c>.</param>
    /// <param name="allowedTargets">The configured allow-list of exact, same-origin relative paths.</param>
    /// <returns>The candidate if it is present in the allow-list; otherwise <see cref="DefaultTarget"/>.</returns>
    public static string Validate(string? candidate, IReadOnlyCollection<string> allowedTargets)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return DefaultTarget;
        }

        var isAllowed = allowedTargets.Contains(candidate, StringComparer.Ordinal);
        return isAllowed ? candidate : DefaultTarget;
    }
}
