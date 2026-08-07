using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Application.Designs.Composer.Commands;
using Icbank.Platform.Application.Designs.Composer.Queries;
using Icbank.Platform.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/designs.ts</c> (API-SURFACE.md §17, BUSINESS-RULES.md §7). The Node source
/// gates this entire router with <c>router.use(requireAdmin)</c>. This port expresses the same
/// "privileged, coarse-grained" intent via the seeded <c>design_studio:{verb}</c> policy family
/// rather than a blanket role check, per DOTNET-CONVENTIONS.md §5.4's graduated-permission model
/// (also unifying with <see cref="IconEventDesignsController"/>, resolving AMBIGUOUS-API-5).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/designs")]
public sealed class DesignsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="DesignsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch design commands/queries.</param>
    public DesignsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists all design templates.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated template list.</returns>
    [HttpGet("templates")]
    [Authorize(Policy = "design_studio:view")]
    public async Task<ActionResult<PagedResult<DesignTemplateDto>>> ListTemplatesAsync(
        [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var paging = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<DesignTemplateDto>> result = await _sender.Send(new ListDesignTemplatesQuery(paging), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Fetches a single design template.</summary>
    /// <param name="templateId">The template id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpGet("templates/{templateId:int}")]
    [Authorize(Policy = "design_studio:view")]
    public async Task<ActionResult<DesignTemplateDto>> GetTemplateByIdAsync(int templateId, CancellationToken cancellationToken)
    {
        Result<DesignTemplateDto> result = await _sender.Send(new GetDesignTemplateByIdQuery(templateId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Creates a design template.</summary>
    /// <param name="request">The new template's fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new template.</returns>
    [HttpPost("templates")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<DesignTemplateDto>> CreateTemplateAsync([FromBody] CreateDesignTemplateRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new CreateDesignTemplateCommand(
            actorUserId,
            request.TemplateNameAr,
            request.Category,
            request.CanvasWidth,
            request.CanvasHeight,
            request.BackgroundPanelConfig,
            request.TextSlots,
            request.LogoSlots,
            request.PromptHint);
        Result<DesignTemplateDto> result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Seeds one legacy pixel-based test template. No-op if any template already exists.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the resulting template.</returns>
    [HttpPost("templates/seed-test")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<SeedTestDesignTemplateResultDto>> SeedTestTemplateAsync(CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<SeedTestDesignTemplateResultDto> result = await _sender.Send(new SeedTestDesignTemplateCommand(actorUserId), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Reseeds the presentation-layout template set. Idempotent overwrite-by-name.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the reseed result.</returns>
    [HttpPost("templates/reseed-presentation")]
    [Authorize(Policy = "design_studio:create")]
    public Task<ActionResult<ReseedDesignTemplateSetResultDto>> ReseedPresentationAsync(CancellationToken cancellationToken) =>
        ReseedAsync(DesignTemplateSeedSet.Presentation, cancellationToken);

    /// <summary>Reseeds the V2 social-media template set. Idempotent overwrite-by-name.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the reseed result.</returns>
    [HttpPost("templates/reseed-v2")]
    [Authorize(Policy = "design_studio:create")]
    public Task<ActionResult<ReseedDesignTemplateSetResultDto>> ReseedV2Async(CancellationToken cancellationToken) =>
        ReseedAsync(DesignTemplateSeedSet.SocialV2, cancellationToken);

    /// <summary>Reseeds the 2026 template set. Idempotent overwrite-by-name.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the reseed result.</returns>
    [HttpPost("templates/reseed-2026")]
    [Authorize(Policy = "design_studio:create")]
    public Task<ActionResult<ReseedDesignTemplateSetResultDto>> ReseedYear2026Async(CancellationToken cancellationToken) =>
        ReseedAsync(DesignTemplateSeedSet.Year2026, cancellationToken);

    /// <summary>Seeds the official GAC brand logos. Idempotent on <c>logoName</c>.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the seed result.</returns>
    [HttpPost("logos/seed-gac")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<SeedGacLogosResultDto>> SeedGacLogosAsync(CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<SeedGacLogosResultDto> result = await _sender.Send(new SeedGacLogosCommand(actorUserId), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Deletes a design template.</summary>
    /// <param name="templateId">The template id to delete.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("templates/{templateId:int}")]
    [Authorize(Policy = "design_studio:delete")]
    public async Task<ActionResult> DeleteTemplateAsync(int templateId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteDesignTemplateCommand(actorUserId, templateId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Composes a design server-side from a template, optional background, and selected logos.</summary>
    /// <param name="request">The render parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the composed image's object path, or 404 if the template does not exist.</returns>
    [HttpPost("render")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<RenderDesignResultDto>> RenderAsync([FromBody] RenderDesignRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new RenderDesignCommand(
            actorUserId,
            request.TemplateId,
            request.TitleText,
            request.BodyText,
            request.BackgroundUrl,
            request.SelectedLogoIds,
            request.TitleFontSize,
            request.BodyFontSize,
            request.Department,
            request.FontFamily);
        Result<RenderDesignResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Gets a presigned upload URL for a logo.</summary>
    /// <param name="request">The file name/content-type.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the presigned upload descriptor.</returns>
    [HttpPost("logos/upload-url")]
    [Authorize(Policy = "design_studio:create")]
    public Task<ActionResult<PresignedUpload>> GetLogoUploadUrlAsync([FromBody] UploadUrlRequest request, CancellationToken cancellationToken) =>
        GetUploadUrlAsync(request, "logos", cancellationToken);

    /// <summary>Lists all brand logos.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated logo list.</returns>
    [HttpGet("logos")]
    [Authorize(Policy = "design_studio:view")]
    public async Task<ActionResult<PagedResult<BrandLogoDto>>> ListLogosAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var paging = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<BrandLogoDto>> result = await _sender.Send(new ListBrandLogosQuery(paging), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Creates a logo record referencing an already-uploaded object.</summary>
    /// <param name="request">The new logo's fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new logo.</returns>
    [HttpPost("logos")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<BrandLogoDto>> CreateLogoAsync([FromBody] CreateBrandLogoRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new CreateBrandLogoCommand(actorUserId, request.LogoName, request.FileUrl, request.Transparent, request.DefaultWidth);
        Result<BrandLogoDto> result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Deletes a logo.</summary>
    /// <param name="logoId">The logo id to delete.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("logos/{logoId:int}")]
    [Authorize(Policy = "design_studio:delete")]
    public async Task<ActionResult> DeleteLogoAsync(int logoId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteBrandLogoCommand(actorUserId, logoId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Gets a presigned upload URL for a font.</summary>
    /// <param name="request">The file name/content-type.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the presigned upload descriptor.</returns>
    [HttpPost("fonts/upload-url")]
    [Authorize(Policy = "design_studio:create")]
    public Task<ActionResult<PresignedUpload>> GetFontUploadUrlAsync([FromBody] UploadUrlRequest request, CancellationToken cancellationToken) =>
        GetUploadUrlAsync(request, "fonts", cancellationToken);

    /// <summary>Lists all brand fonts.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated font list.</returns>
    [HttpGet("fonts")]
    [Authorize(Policy = "design_studio:view")]
    public async Task<ActionResult<PagedResult<BrandFontDto>>> ListFontsAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var paging = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<BrandFontDto>> result = await _sender.Send(new ListBrandFontsQuery(paging), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Creates a font record referencing an already-uploaded object.</summary>
    /// <param name="request">The new font's fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new font.</returns>
    [HttpPost("fonts")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<BrandFontDto>> CreateFontAsync([FromBody] CreateBrandFontRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<BrandFontDto> result = await _sender.Send(new CreateBrandFontCommand(actorUserId, request.FontName, request.FontFileUrl), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Sets a font as the default. Closes DEFECT-LOG.md DATA-01 (transactional, scoped update).</summary>
    /// <param name="fontId">The font id to set as default.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the updated font, or 404 if not found.</returns>
    [HttpPatch("fonts/{fontId:int}/default")]
    [Authorize(Policy = "design_studio:edit")]
    public async Task<ActionResult<BrandFontDto>> SetDefaultFontAsync(int fontId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<BrandFontDto> result = await _sender.Send(new SetDefaultBrandFontCommand(actorUserId, fontId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Deletes a font.</summary>
    /// <param name="fontId">The font id to delete.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("fonts/{fontId:int}")]
    [Authorize(Policy = "design_studio:delete")]
    public async Task<ActionResult> DeleteFontAsync(int fontId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteBrandFontCommand(actorUserId, fontId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>AI-generates 4 background image variants. Rate limited and audited (external-cost abuse vector).</summary>
    /// <param name="request">The generation parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the generated variants, or 429 if the caller's generation quota is exhausted.</returns>
    [HttpPost("generate-backgrounds")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<GenerateBackgroundsResultDto>> GenerateBackgroundsAsync(
        [FromBody] GenerateBackgroundsRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<GenerateBackgroundsResultDto> result = await _sender.Send(new GenerateBackgroundsCommand(actorUserId, request.Prompt, request.TemplateId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status429TooManyRequests);
    }

    private async Task<ActionResult<ReseedDesignTemplateSetResultDto>> ReseedAsync(DesignTemplateSeedSet seedSet, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<ReseedDesignTemplateSetResultDto> result = await _sender.Send(new ReseedDesignTemplateSetCommand(actorUserId, seedSet), cancellationToken);
        return Ok(result.Value);
    }

    private async Task<ActionResult<PresignedUpload>> GetUploadUrlAsync(UploadUrlRequest request, string folder, CancellationToken cancellationToken)
    {
        Result<PresignedUpload> result = await _sender.Send(new GetDesignAssetUploadUrlQuery(request.FileName, request.ContentType, folder), cancellationToken);
        return Ok(result.Value);
    }
}
