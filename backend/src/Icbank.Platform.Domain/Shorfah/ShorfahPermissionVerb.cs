namespace Icbank.Platform.Domain.Shorfah;

/// <summary>Section-scoped permission verbs (DATA-MODEL.md section 5).</summary>
public enum ShorfahPermissionVerb
{
    /// <summary>Permission to view the section.</summary>
    View = 0,

    /// <summary>Permission to contribute content to the section.</summary>
    Contribute = 1,

    /// <summary>Permission to review submitted content.</summary>
    Review = 2,

    /// <summary>Permission to give final approval.</summary>
    Approve = 3,
}
