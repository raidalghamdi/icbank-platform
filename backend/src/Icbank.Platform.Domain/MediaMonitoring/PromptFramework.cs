using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>
/// Reusable, versioned AI prompt template (DATA-MODEL.md section 3.7 <c>prompt_frameworks</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>created_by_user_id</c> was an unenforced implied FK in the source schema
/// (DATA-MODEL.md section 4). It is now a proper, enforced, optional foreign key.
/// </remarks>
public sealed class PromptFramework : AuditableEntity
{
    /// <summary>Gets or sets the Arabic name.</summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional English name.</summary>
    public string? NameEn { get; set; }

    /// <summary>Gets or sets the optional Arabic description.</summary>
    public string? DescriptionAr { get; set; }

    /// <summary>Gets or sets the prompt category.</summary>
    public PromptFrameworkCategory Category { get; set; } = PromptFrameworkCategory.ContentCreation;

    /// <summary>Gets or sets whether this row is a framework or a template.</summary>
    public PromptFrameworkKind Kind { get; set; } = PromptFrameworkKind.Framework;

    /// <summary>Gets or sets the prompt text, containing <c>{{variable}}</c> placeholders.</summary>
    public string PromptText { get; set; } = string.Empty;

    /// <summary>Gets or sets the dynamic variable list.</summary>
    public List<PromptVariable> Variables { get; set; } = new();

    /// <summary>Gets or sets an example input.</summary>
    public string? ExampleInput { get; set; }

    /// <summary>Gets or sets an example output.</summary>
    public string? ExampleOutput { get; set; }

    /// <summary>Gets or sets the searchable tag list.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the recommended AI model.</summary>
    public string? RecommendedModel { get; set; } = "gemini-2.5-flash";

    /// <summary>Gets or sets a value indicating whether the framework is officially approved.</summary>
    public bool IsApproved { get; set; }

    /// <summary>Gets or sets the usage counter.</summary>
    public int UsageCount { get; set; }

    /// <summary>Gets or sets the id of the user who created this framework, if known.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Gets or sets the creating-user navigation property.</summary>
    public User? CreatedByUser { get; set; }

    /// <summary>Gets or sets a denormalized snapshot of the creating user's name.</summary>
    public string? CreatedByName { get; set; }

    /// <summary>Gets or sets the lifecycle status.</summary>
    public PromptFrameworkStatus Status { get; set; } = PromptFrameworkStatus.Active;
}
