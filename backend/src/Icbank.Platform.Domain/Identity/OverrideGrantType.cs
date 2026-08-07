namespace Icbank.Platform.Domain.Identity;

/// <summary>Allow/deny grant kind for <c>user_page_overrides.grant_type</c> (DATA-MODEL.md §5).</summary>
public enum OverrideGrantType
{
    /// <summary>Explicitly grants the permission.</summary>
    Allow = 0,

    /// <summary>Explicitly denies the permission, overriding any role-based grant.</summary>
    Deny = 1,
}
