namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>
/// Port for the server-side design composer (ports <c>composer/composer.ts</c>'s
/// sharp+Pango-based raster compositor -- 700+ lines of image-manipulation code, out of scope for
/// this port per the mandated narrow-named-interface + deterministic-placeholder pattern, see
/// WAVE3B-PORT-NOTES.md). The default implementation is a placeholder that never fabricates a
/// real image; every downstream concern (template/logo lookup, authorization, audit logging,
/// storage persistence) is fully exercisable end-to-end.
/// </summary>
public interface IDesignComposer
{
    /// <summary>Composes the given input into final image bytes.</summary>
    /// <param name="input">The resolved compose input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The composed image bytes.</returns>
    Task<byte[]> ComposeAsync(ComposeDesignInput input, CancellationToken cancellationToken);
}
