namespace Icbank.Platform.Domain.Identity;

/// <summary>The five action verbs catalogued in <c>permissions.name</c> (DATA-MODEL.md §5).</summary>
public enum PermissionVerbName
{
    /// <summary>Permission to view/read a resource.</summary>
    View = 0,

    /// <summary>Permission to create a resource.</summary>
    Create = 1,

    /// <summary>Permission to edit a resource.</summary>
    Edit = 2,

    /// <summary>Permission to delete a resource.</summary>
    Delete = 3,

    /// <summary>Permission to export a resource.</summary>
    Export = 4,
}
