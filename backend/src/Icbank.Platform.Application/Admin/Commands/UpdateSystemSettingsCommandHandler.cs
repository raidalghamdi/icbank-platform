using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Handles <see cref="UpdateSystemSettingsCommand"/>.</summary>
public sealed class UpdateSystemSettingsCommandHandler : IRequestHandler<UpdateSystemSettingsCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLog;

    /// <summary>Initializes a new instance of the <see cref="UpdateSystemSettingsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLog">The privileged-action audit log port.</param>
    public UpdateSystemSettingsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLog)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLog = auditLog;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(UpdateSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        var unknownKeys = request.Settings.Keys.Except(SystemSettingsSchema.AllKeys).ToList();
        if (unknownKeys.Count > 0)
        {
            return Result<bool>.Failure("unknown_setting_key: " + string.Join(',', unknownKeys));
        }

        List<string> changedKeys = await ApplySettingsAsync(request, cancellationToken);

        // Why: R-BE-054 — secret values (azure_ad_client_secret) are never written to the audit
        // log payload; only the fact that the key changed is recorded.
        await _auditLog.RecordAsync(
            request.ActorUserId,
            "settings.update",
            "SystemSetting",
            string.Join(',', changedKeys),
            before: null,
            after: new { ChangedKeys = changedKeys },
            cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <summary>Upserts each requested setting against the existing rows and persists the changes.</summary>
    /// <returns>The keys that were included in the request, in request order.</returns>
    private async Task<List<string>> ApplySettingsAsync(UpdateSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        List<SystemSetting> existing = await _queryExecutor.ToListAsync(_dbContext.SystemSettings, cancellationToken);
        var actorId = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var changedKeys = new List<string>();

        foreach ((var key, var value) in request.Settings)
        {
            SystemSetting? row = existing.SingleOrDefault(setting => setting.Key == key);
            if (row is null)
            {
                _dbContext.Add(new SystemSetting { Key = key, Value = value, CreatedBy = actorId });
            }
            else if (row.Value != value)
            {
                row.Value = value;
                row.UpdatedBy = actorId;
            }

            changedKeys.Add(key);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return changedKeys;
    }
}
