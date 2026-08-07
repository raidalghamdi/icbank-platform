using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Handles <see cref="GetSystemSettingsQuery"/>, masking every secret-classified key.</summary>
public sealed class GetSystemSettingsQueryHandler : IRequestHandler<GetSystemSettingsQuery, Result<IReadOnlyDictionary<string, string>>>
{
    private const string MaskedValue = "********";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetSystemSettingsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetSystemSettingsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyDictionary<string, string>>> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        List<SystemSetting> stored = await _queryExecutor.ToListAsync(_dbContext.SystemSettings, cancellationToken);
        var byKey = stored.ToDictionary(setting => setting.Key, setting => setting.Value);

        var result = SystemSettingsSchema.AllKeys.ToDictionary(
            key => key,
            key => SystemSettingsSchema.SecretKeys.Contains(key) && byKey.ContainsKey(key)
                ? MaskedValue
                : byKey.GetValueOrDefault(key, string.Empty));

        return Result<IReadOnlyDictionary<string, string>>.Success(result);
    }
}
