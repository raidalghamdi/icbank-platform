using System.Text.Json;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>
/// Handles <see cref="SaveCampaignCommand"/>. Progress is never taken from the browser: it is
/// recomputed from the completed tasks and deliverables in the plan, and the board status is
/// derived from the lifecycle stage so the page filters always agree with the stage.
/// </summary>
public sealed class SaveCampaignCommandHandler : IRequestHandler<SaveCampaignCommand, Result<int>>
{
    private const int NameMaxLength = 300;
    private const int TextMaxLength = 600;
    private const int ListMaxLength = 4000;
    private const int OwnerMaxLength = 150;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="SaveCampaignCommandHandler"/> class.</summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="queryExecutor">The async query executor.</param>
    public SaveCampaignCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(SaveCampaignCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CampaignAudience audience = CampaignLabels.ParseAudience(request.Audience) ?? CampaignAudience.Internal;
        SaveCampaignFields fields = request.Fields;
        CampaignStage? stage = CampaignLabels.ParseStage(fields.Stage);
        var error = Validate(fields, stage);
        if (error is not null)
        {
            return Result<int>.Failure(error);
        }

        Campaign? campaign = request.CampaignId is int id
            ? await _queryExecutor.SingleOrDefaultAsync(
                _dbContext.Campaigns.Where(c => c.Id == id && c.IsActive && c.Audience == audience), cancellationToken)
            : NewCampaign(audience);
        if (campaign is null)
        {
            return Result<int>.Failure(SaveCampaignCommand.NotFoundError);
        }

        // Why: closing is the last step of the lifecycle; a campaign that is still publishing or
        // planning must pass through performance measurement first.
        if (stage == CampaignStage.Closed && campaign.Stage < CampaignStage.Measurement)
        {
            return Result<int>.Failure(SaveCampaignCommand.ClosureError);
        }

        Apply(campaign, fields, stage!.Value);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(campaign.Id);
    }

    private static string? Validate(SaveCampaignFields fields, CampaignStage? stage)
    {
        if (string.IsNullOrWhiteSpace(fields.Name))
        {
            return SaveCampaignCommand.NameRequiredError;
        }

        if (fields.EndDate.Date < fields.StartDate.Date)
        {
            return SaveCampaignCommand.PeriodError;
        }

        if (stage is null)
        {
            return SaveCampaignCommand.StageError;
        }

        return fields.PlanJson is null || CampaignPlan.IsValid(fields.PlanJson) ? null : SaveCampaignCommand.PlanError;
    }

    private static void Apply(Campaign campaign, SaveCampaignFields fields, CampaignStage stage)
    {
        campaign.Name = Clip(fields.Name, NameMaxLength);
        campaign.Objective = Clip(fields.Objective, TextMaxLength);
        campaign.Description = campaign.Description.Length == 0 ? campaign.Objective : campaign.Description;
        campaign.TargetAudience = Clip(fields.TargetAudience, TextMaxLength);
        campaign.StartDate = fields.StartDate.Date;
        campaign.EndDate = fields.EndDate.Date;
        campaign.KeyMessages = Clip(fields.KeyMessages, ListMaxLength);
        campaign.Owner = Clip(fields.Owner, OwnerMaxLength);
        campaign.TeamMembers = Clip(fields.TeamMembers, ListMaxLength);
        campaign.PlannedChannels = Clip(fields.PlannedChannels, ListMaxLength);
        campaign.LatestUpdate = fields.LatestUpdate is null ? campaign.LatestUpdate : Clip(fields.LatestUpdate, TextMaxLength);
        campaign.Stage = stage;
        campaign.Status = CampaignLabels.StatusOf(stage);
        campaign.IsUserManaged = true;
        if (fields.PlanJson is not null)
        {
            campaign.PlanJson = fields.PlanJson;
            ApplyPlanFigures(campaign, CampaignPlan.ToElement(fields.PlanJson)!.Value);
        }
    }

    private static void ApplyPlanFigures(Campaign campaign, JsonElement plan)
    {
        campaign.ProgressPercent = CampaignPlan.Progress(plan) ?? campaign.ProgressPercent;
        campaign.ReachCount = CampaignPlan.Result(plan, "reach") ?? campaign.ReachCount;
        campaign.ImpressionsCount = CampaignPlan.Result(plan, "impressions") ?? campaign.ImpressionsCount;
        campaign.EngagementCount = CampaignPlan.Result(plan, "engagement") ?? campaign.EngagementCount;
    }

    private static string Clip(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private Campaign NewCampaign(CampaignAudience audience)
    {
        var campaign = new Campaign
        {
            Audience = audience,
            Code = string.Concat(audience == CampaignAudience.External ? "EXT-U-" : "INT-U-", Guid.NewGuid().ToString("N").AsSpan(0, 8)),
            Department = "الاتصال المؤسسي",
            SortOrder = 1000,
            IsActive = true,
            IsUserManaged = true,
        };
        _dbContext.Add(campaign);
        return campaign;
    }
}
