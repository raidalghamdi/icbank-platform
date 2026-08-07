using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="GenerateWeekStartMessagesCommand"/>. Ports BUSINESS-RULES.md §2.5's style-context assembly and persists one <see cref="GeneratedOutput"/> row per model.</summary>
public sealed class GenerateWeekStartMessagesCommandHandler
    : IRequestHandler<GenerateWeekStartMessagesCommand, Result<IReadOnlyList<GeneratedOutputDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IWeekStartMessageGenerator _generator;

    /// <summary>Initializes a new instance of the <see cref="GenerateWeekStartMessagesCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="generator">The AI-backed (or placeholder) message generation port.</param>
    public GenerateWeekStartMessagesCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IWeekStartMessageGenerator generator)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _generator = generator;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GeneratedOutputDto>>> Handle(GenerateWeekStartMessagesCommand request, CancellationToken cancellationToken)
    {
        List<StyleProfile> profiles = await _queryExecutor.ToListAsync(_dbContext.StyleProfiles, cancellationToken);
        StyleProfile? profile = profiles.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).FirstOrDefault();
        var styleContext = profile?.ToneSummary;

        var generationRequest = new WeekStartGenerationRequest(request.Topic, request.Occasion, request.Audience, request.Tone, request.Length, styleContext);
        IReadOnlyList<WeekStartModelOutput> outputs = await _generator.GenerateAsync(generationRequest, cancellationToken);

        var savedOutputs = new List<GeneratedOutput>();
        foreach (WeekStartModelOutput modelOutput in outputs)
        {
            var entity = new GeneratedOutput
            {
                Topic = request.Topic,
                ModelName = modelOutput.ModelName,
                OutputText = modelOutput.OutputText,
                CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            _dbContext.Add(entity);
            savedOutputs.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dtos = savedOutputs.Select(ToDto).ToList();
        return Result<IReadOnlyList<GeneratedOutputDto>>.Success(dtos);
    }

    private static GeneratedOutputDto ToDto(GeneratedOutput output) =>
        new(output.Id, output.Topic, output.ModelName, output.OutputText, output.Selected, output.CreatedAt);
}
