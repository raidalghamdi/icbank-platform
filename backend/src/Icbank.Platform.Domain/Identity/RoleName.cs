namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// The nine seeded role machine-names (DATA-MODEL.md §5 "Role name"). Stored as its
/// string name in <c>roles.name</c> — the enum exists in .NET purely for compile-time safety
/// in seed data and business logic, not as the SQL Server column type (R-BE-013: enums are
/// string-stored).
/// </summary>
public enum RoleName
{
    /// <summary>Unrestricted platform administrator.</summary>
    SuperAdmin = 0,

    /// <summary>Standard administrator.</summary>
    Admin = 1,

    /// <summary>System-level administrator.</summary>
    SystemAdmin = 2,

    /// <summary>Manager who has approved elevated access.</summary>
    ApprovedManager = 3,

    /// <summary>Ordinary team member.</summary>
    TeamMember = 4,

    /// <summary>User who can request actions but not approve them.</summary>
    Requester = 5,

    /// <summary>Content editor.</summary>
    Editor = 6,

    /// <summary>Read-only viewer.</summary>
    Viewer = 7,

    /// <summary>Guest with minimal access.</summary>
    Guest = 8,
}
