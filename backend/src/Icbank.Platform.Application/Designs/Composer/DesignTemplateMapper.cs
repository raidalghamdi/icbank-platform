using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>Maps <see cref="DesignTemplate"/> entities to <see cref="DesignTemplateDto"/> read models.</summary>
public static class DesignTemplateMapper
{
    /// <summary>Maps a single entity.</summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The mapped DTO.</returns>
    public static DesignTemplateDto ToDto(DesignTemplate entity) =>
        new(entity.Id, entity.TemplateNameAr, entity.Category, entity.CanvasWidth, entity.CanvasHeight, entity.ThumbnailUrl, entity.PromptHint, entity.CreatedAt);
}
