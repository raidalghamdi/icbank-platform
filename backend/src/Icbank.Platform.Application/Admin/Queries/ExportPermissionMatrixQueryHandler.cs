using System.Text;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using MediatR;
using Result = Icbank.Platform.Application.Common.Models.Result<Icbank.Platform.Application.Admin.Queries.PermissionMatrixExportDto>;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Handles <see cref="ExportPermissionMatrixQuery"/>. Mirrors BUSINESS-RULES.md §10.6's
/// "access level" glyph heuristic (<c>delete/export → full, create/edit → edit, view → view, else
/// none</c>) for the CSV rendering, since the export's whole purpose (per the old system) is a
/// human-skimmable summary rather than a literal permission dump.
/// </summary>
public sealed class ExportPermissionMatrixQueryHandler : IRequestHandler<ExportPermissionMatrixQuery, Result>
{
    private const string CsvFormat = "csv";
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IPermissionResolver _permissionResolver;

    /// <summary>Initializes a new instance of the <see cref="ExportPermissionMatrixQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="permissionResolver">The shared effective-permission resolution port.</param>
    public ExportPermissionMatrixQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IPermissionResolver permissionResolver)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _permissionResolver = permissionResolver;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ExportPermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        List<User> users = await _queryExecutor.ToListAsync(_dbContext.Users.OrderBy(u => u.Id), cancellationToken);
        List<Page> pages = await _queryExecutor.ToListAsync(_dbContext.Pages.OrderBy(p => p.SortOrder), cancellationToken);

        var rows = new List<(User User, PermissionResolution Resolution)>();
        foreach (User user in users)
        {
            rows.Add((user, await _permissionResolver.ResolveAsync(user.Id, cancellationToken)));
        }

        return string.Equals(request.Format, CsvFormat, StringComparison.OrdinalIgnoreCase)
            ? Result.Success(RenderCsv(rows, pages))
            : Result.Success(RenderJson(rows, pages));
    }

    private static PermissionMatrixExportDto RenderCsv(List<(User User, PermissionResolution Resolution)> rows, List<Page> pages)
    {
        var builder = new StringBuilder();
        builder.Append("email,role").Append(',').AppendJoin(',', pages.Select(p => p.Slug)).Append('\n');
        foreach ((User user, PermissionResolution resolution) in rows)
        {
            var roleLabel = string.Join('|', resolution.RoleNames);
            builder.Append(CsvEscape(user.Email)).Append(',').Append(CsvEscape(roleLabel));
            foreach (Page page in pages)
            {
                builder.Append(',').Append(AccessLevelGlyph(resolution.Permissions, page.Slug));
            }

            builder.Append('\n');
        }

        return new PermissionMatrixExportDto("text/csv", "permission-matrix.csv", Utf8WithBom.GetBytes(builder.ToString()));
    }

    private static PermissionMatrixExportDto RenderJson(List<(User User, PermissionResolution Resolution)> rows, List<Page> pages)
    {
        var payload = rows.Select(row => new
        {
            email = row.User.Email,
            roles = row.Resolution.RoleNames,
            permissions = pages.ToDictionary(page => page.Slug, page => AccessLevelGlyph(row.Resolution.Permissions, page.Slug)),
        });

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return new PermissionMatrixExportDto("application/json", "permission-matrix.json", Encoding.UTF8.GetBytes(json));
    }

    private static string AccessLevelGlyph(IReadOnlyCollection<string> permissions, string pageSlug)
    {
        bool Has(string verb) => permissions.Contains(pageSlug + ":" + verb);

        if (Has("delete") || Has("export"))
        {
            return "full";
        }

        if (Has("create") || Has("edit"))
        {
            return "edit";
        }

        return Has("view") ? "view" : "none";
    }

    private static string CsvEscape(string value)
    {
        return value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal)
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }
}
