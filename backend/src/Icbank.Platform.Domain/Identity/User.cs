using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// Core identity/account record for every platform user, either password-based or Azure AD SSO
/// (DATA-MODEL.md §3.1 <c>users</c>). Source table has no soft-delete or <c>created_by</c>/
/// <c>updated_by</c> columns; both are added here to close the rulebook non-compliance flagged in
/// DATA-MODEL.md §8.
/// </summary>
public sealed class User : AuditableEntity
{
    /// <summary>Gets or sets the user's email address. Lowercased/trimmed by the application before insert.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's job title, if known.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the user's department, if known.</summary>
    public string? Department { get; set; }

    /// <summary>Gets or sets the password hash. Null for SSO-only accounts.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Gets or sets the Azure AD object id, set on first SSO login.</summary>
    public string? AzureOid { get; set; }

    /// <summary>Gets or sets a value indicating whether the account is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the account is locked (set after repeated failed logins).</summary>
    public bool IsLocked { get; set; }

    /// <summary>Gets or sets the number of consecutive failed login attempts.</summary>
    public int FailedAttempts { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the last successful login, if any.</summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>Gets or sets the UTC timestamp the password was last changed, if any.</summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user must change their password before
    /// performing any other action. Set by the seeder for the initial super-admin account and by
    /// admin-triggered password resets (task requirement 6: "forces a password change on first
    /// login").
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Gets the role assignments held by this user.</summary>
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();

    /// <summary>Gets the per-page permission overrides granted to this user.</summary>
    public ICollection<UserPageOverride> PageOverrides { get; init; } = new List<UserPageOverride>();

    /// <summary>Gets the activity-log entries recorded for this user.</summary>
    public ICollection<ActivityLog> ActivityLogs { get; init; } = new List<ActivityLog>();
}
