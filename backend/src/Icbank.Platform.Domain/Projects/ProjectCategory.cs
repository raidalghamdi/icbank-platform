namespace Icbank.Platform.Domain.Projects;

/// <summary>
/// How the department classifies a project in its portfolio. The two buckets are read very
/// differently by leadership: operational work is judged on delivery cadence, strategic work on
/// progress against the authority's multi-quarter objectives.
/// </summary>
public enum ProjectCategory
{
    /// <summary>Day-to-day delivery work owned by a single team.</summary>
    Operational = 0,

    /// <summary>Cross-cutting programme tied to the authority's strategy.</summary>
    Strategic = 1,
}
