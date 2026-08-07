using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Migration.Migrators;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icbank.Platform.DataMigration.Tests.Migration;

/// <summary>
/// Integration coverage for <see cref="UserTableMigrator"/>'s EF write half -- the single most
/// consequential migrator in the tool: it is the root every other Identity/RBAC and feature
/// table's <c>*_by</c>/<c>owner_*</c> FK ultimately resolves through, and it is where the
/// bcrypt-to-PBKDF2 password non-portability finding (spec/DATA-MIGRATION-NOTES.md Finding 1)
/// actually gets applied to a real row. Before this test class, <see cref="UserTableMigrator"/>
/// had zero coverage against a real write; only the transformer test suite exercised the
/// pure mapping function it calls into, never the insert/idempotency/skip logic around it.
/// </summary>
/// <remarks>
/// Same <c>ICBANK_TEST_SQL_CONNECTION</c>-gated pattern as
/// <c>DailyReportTableMigratorSqlServerTests</c> -- see that file's remarks for why (no SQL
/// Server instance is reachable in this sandbox, so these tests return early and their real
/// assertions have not yet executed here).
/// </remarks>
public sealed class UserTableMigratorSqlServerTests : IAsyncLifetime
{
    private static readonly string? SqlServerConnectionTemplate =
        Environment.GetEnvironmentVariable("ICBANK_TEST_SQL_CONNECTION");

    private readonly string _databaseName = $"IcbankUserMigrationTest_{Guid.NewGuid():N}";
    private string? _connectionString;

    private static bool HasSqlServer => !string.IsNullOrWhiteSpace(SqlServerConnectionTemplate);

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (!HasSqlServer)
        {
            return;
        }

        _connectionString = new SqlConnectionStringBuilder(SqlServerConnectionTemplate)
        {
            InitialCatalog = _databaseName,
        }.ConnectionString;

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (!HasSqlServer || _connectionString is null)
        {
            return;
        }

        SqlConnection.ClearAllPools();
        var masterBuilder = new SqlConnectionStringBuilder(SqlServerConnectionTemplate) { InitialCatalog = "master" };
        try
        {
            await using var connection = new SqlConnection(masterBuilder.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText =
                $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN " +
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{_databaseName}]; END";
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // Best-effort cleanup only, matching the established precedent.
        }
    }

    [Fact]
    public async Task MigrateAsync_BcryptUser_WritesNullPasswordHashAndMustChangePasswordTrue()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new FakeSource(new[]
        {
            Row(1, "user@icbank.com", "$2b$10$abcdefghijklmnopqrstuvwxyz0123456789012345678901234"),
        });
        var migrator = new UserTableMigrator();

        await migrator.MigrateAsync(BuildContext(source), CancellationToken.None);

        await using var verify = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        User user = await verify.Users.SingleAsync();
        user.PasswordHash.Should().BeNull("a bcrypt hash from the source system is not portable to PBKDF2-HMAC-SHA256 and must never be copied over");
        user.MustChangePassword.Should().BeTrue("every user with a non-portable password must be forced to reset on first login");
    }

    [Fact]
    public async Task MigrateAsync_SsoOnlyUser_WritesNullPasswordHashButDoesNotForceReset()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new FakeSource(new[] { Row(1, "sso@icbank.com", passwordHash: null) });
        var migrator = new UserTableMigrator();

        await migrator.MigrateAsync(BuildContext(source), CancellationToken.None);

        await using var verify = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        User user = await verify.Users.SingleAsync();
        user.PasswordHash.Should().BeNull();
        user.MustChangePassword.Should().BeFalse("an Azure AD SSO-only user never had a password to migrate, so forcing a reset would be spurious");
    }

    [Fact]
    public async Task MigrateAsync_DuplicateEmailAlreadyInDestination_SkipsAndRecordsMapping_NotADuplicateInsert()
    {
        if (!HasSqlServer)
        {
            return;
        }

        await using (var seed = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options))
        {
            seed.Users.Add(new User { Email = "existing@icbank.com", Name = "Existing", CreatedAt = DateTime.UtcNow, CreatedBy = "seed" });
            await seed.SaveChangesAsync();
        }

        var source = new FakeSource(new[] { Row(1, "existing@icbank.com", passwordHash: null) });
        var idMap = new InMemoryIdMappingStore();

        TableMigrationResult result = await new UserTableMigrator().MigrateAsync(BuildContext(source, idMap), CancellationToken.None);

        result.RowsInserted.Should().Be(0);
        result.RowsSkippedAlreadyMigrated.Should().Be(1, "a user with this email already exists in the destination, so no new row must be inserted");

        await using var verify = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        (await verify.Users.CountAsync()).Should().Be(1, "the migrator must recognize the pre-existing row by email and not create a duplicate");
    }

    [Fact]
    public async Task MigrateAsync_RunTwice_IsIdempotent()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new FakeSource(new[] { Row(1, "idempotent@icbank.com", "$2b$10$abcdefghijklmnopqrstuvwxyz0123456789012345678901234") });
        var idMap = new InMemoryIdMappingStore();
        var migrator = new UserTableMigrator();

        await migrator.MigrateAsync(BuildContext(source, idMap), CancellationToken.None);
        TableMigrationResult second = await migrator.MigrateAsync(BuildContext(source, idMap), CancellationToken.None);

        second.RowsInserted.Should().Be(0);
        second.RowsSkippedAlreadyMigrated.Should().Be(1);

        await using var verify = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        (await verify.Users.CountAsync()).Should().Be(1, "re-running the migrator must never duplicate an already-migrated user");
    }

    private static SourceRow Row(int id, string email, string? passwordHash) => new(new Dictionary<string, object?>
    {
        ["id"] = id,
        ["email"] = email,
        ["name"] = "Test User",
        ["password_hash"] = passwordHash,
        ["is_active"] = true,
        ["is_locked"] = false,
        ["created_at"] = new DateTime(2024, 1, 1),
    });

    private MigrationRunContext BuildContext(IPostgresDataSource source, IIdMappingStore? idMap = null) =>
        new(
            source,
            idMap ?? new InMemoryIdMappingStore(),
            _connectionString!,
            new FixedDateTimeProvider(),
            NullLogger.Instance,
            new MigrationReport { Mode = "migrate", StartedAtUtc = DateTimeOffset.UtcNow });

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public DateTimeOffset RiyadhNow => UtcNow.ToOffset(TimeSpan.FromHours(3));
    }

    private sealed class FakeSource : IPostgresDataSource
    {
        private readonly IReadOnlyList<SourceRow> _rows;

        public FakeSource(IReadOnlyList<SourceRow> rows) => _rows = rows;

        public async IAsyncEnumerable<SourceRow> ReadTableAsync(string tableName, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (SourceRow row in _rows)
            {
                yield return row;
                await Task.Yield();
            }
        }

        public Task<long> CountRowsAsync(string tableName, CancellationToken cancellationToken) => Task.FromResult((long)_rows.Count);

        public Task VerifyConnectivityAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
