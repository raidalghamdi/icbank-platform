namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>The response shape for <c>POST /designs/templates/seed-test</c>.</summary>
/// <param name="Skipped">Whether the seed was skipped because a template already existed.</param>
/// <param name="Template">The resulting (existing or newly-created) template.</param>
public sealed record SeedTestDesignTemplateResultDto(bool Skipped, DesignTemplateDto Template);
