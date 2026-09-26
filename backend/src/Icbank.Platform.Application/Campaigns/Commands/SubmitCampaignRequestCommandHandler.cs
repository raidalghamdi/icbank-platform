using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>Handles <see cref="SubmitCampaignRequestCommand"/>.</summary>
public sealed class SubmitCampaignRequestCommandHandler : IRequestHandler<SubmitCampaignRequestCommand, Result<CampaignRequestDto>>
{
    private const int ShortMaxLength = 150;
    private const int NameMaxLength = 300;
    private const int TextMaxLength = 600;
    private const int ListMaxLength = 4000;

    private readonly IApplicationDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="SubmitCampaignRequestCommandHandler"/> class.</summary>
    /// <param name="dbContext">The database context.</param>
    public SubmitCampaignRequestCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Result<CampaignRequestDto>> Handle(SubmitCampaignRequestCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.RequestingDepartment) || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Objective) || string.IsNullOrWhiteSpace(request.TargetAudience))
        {
            return Result<CampaignRequestDto>.Failure(SubmitCampaignRequestCommand.RequiredError);
        }

        if (request.ProposedEnd.Date < request.ProposedStart.Date)
        {
            return Result<CampaignRequestDto>.Failure(SaveCampaignCommand.PeriodError);
        }

        List<string> support = CampaignRequestMapper.SplitSupport(string.Join(',', request.SupportTypes ?? Array.Empty<string>()));
        if (support.Count == 0)
        {
            return Result<CampaignRequestDto>.Failure(SubmitCampaignRequestCommand.SupportError);
        }

        var entity = new CampaignRequest
        {
            RequestingDepartment = Clip(request.RequestingDepartment, ShortMaxLength),
            Name = Clip(request.Name, NameMaxLength),
            Objective = Clip(request.Objective, TextMaxLength),
            TargetAudience = Clip(request.TargetAudience, TextMaxLength),
            ProposedStart = request.ProposedStart.Date,
            ProposedEnd = request.ProposedEnd.Date,
            KeyMessages = Clip(request.KeyMessages, ListMaxLength),
            SupportTypes = string.Join(',', support),
            Status = CampaignRequestStatus.Submitted,
            SubmittedByUserId = request.ActorUserId,
            SubmittedByName = Clip(request.ActorName, ShortMaxLength),
        };
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<CampaignRequestDto>.Success(CampaignRequestMapper.ToDto(entity));
    }

    private static string Clip(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
