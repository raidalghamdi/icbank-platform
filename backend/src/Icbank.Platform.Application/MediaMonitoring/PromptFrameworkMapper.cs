using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Shared entity-to-DTO mapping for <see cref="PromptFramework"/>, used by every prompt-framework query/command handler.</summary>
public static class PromptFrameworkMapper
{
    /// <summary>Maps a <see cref="PromptFramework"/> entity to its read model.</summary>
    /// <param name="framework">The entity to map.</param>
    /// <returns>The mapped <see cref="PromptFrameworkDto"/>.</returns>
    public static PromptFrameworkDto ToDto(PromptFramework framework) => new(
        framework.Id,
        framework.NameAr,
        framework.NameEn,
        framework.DescriptionAr,
        framework.Category.ToString(),
        framework.Kind.ToString(),
        framework.PromptText,
        framework.Variables.Select(v => new PromptVariableDto(v.Key, v.Label, v.Type, v.Required)).ToList(),
        framework.ExampleInput,
        framework.ExampleOutput,
        framework.Tags,
        framework.RecommendedModel,
        framework.IsApproved,
        framework.UsageCount,
        framework.Status.ToString());
}
