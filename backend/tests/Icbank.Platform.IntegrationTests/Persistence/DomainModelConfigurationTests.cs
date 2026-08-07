using System.Reflection;
using FluentAssertions;
using Icbank.Platform.Domain.Common;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Icbank.Platform.IntegrationTests.Persistence;

/// <summary>
/// Verifies structural properties of the ported domain model that the task's acceptance
/// criteria call out explicitly: every entity carries the audit columns, every soft-deletable
/// entity has its query filter registered, the model builds without EF warnings, and every
/// entity type discoverable in the assembly has a matching <c>IEntityTypeConfiguration&lt;T&gt;</c>.
/// </summary>
public sealed class DomainModelConfigurationTests
{
    private const string FakeConnectionString =
        "Server=localhost;Database=IcbankPlatformModelTest;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public void OnModelCreating_EntireModel_BuildsWithoutThrowing()
    {
        using AppDbContext context = CreateContext();

        IModel model = context.Model;

        model.Should().NotBeNull();
    }

    [Fact]
    public void Model_EveryEntityType_HasIntPrimaryKeyOrDocumentedNaturalKey()
    {
        using AppDbContext context = CreateContext();

        // ShorfahSectionSlaDefault is the single documented exception (natural key), see
        // DOMAIN-PORT-NOTES.md; every other entity must use the standard int surrogate key.
        const string naturalKeyEntity = "ShorfahSectionSlaDefault";

        IEnumerable<IEntityType> entityTypes = context.Model.GetEntityTypes()
            .Where(e => e.ClrType.Name != naturalKeyEntity);

        foreach (IEntityType entityType in entityTypes)
        {
            IKey? primaryKey = entityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull($"{entityType.ClrType.Name} must declare a primary key");
            primaryKey!.Properties.Should().ContainSingle()
                .Which.ClrType.Should().Be(typeof(int), $"{entityType.ClrType.Name} should use an int surrogate key");
        }
    }

    [Fact]
    public void Model_EveryAuditableEntity_HasAllFiveAuditColumnsMapped()
    {
        using AppDbContext context = CreateContext();

        IEnumerable<IEntityType> auditableEntityTypes = context.Model.GetEntityTypes()
            .Where(e => typeof(AuditableEntity).IsAssignableFrom(e.ClrType));

        string[] requiredAuditColumns = { "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "DeletedAt", "RowVersion" };

        foreach (IEntityType entityType in auditableEntityTypes)
        {
            foreach (var column in requiredAuditColumns)
            {
                entityType.FindProperty(column).Should().NotBeNull(
                    $"{entityType.ClrType.Name} inherits AuditableEntity and must map '{column}'");
            }
        }
    }

    [Fact]
    public void Model_EverySoftDeletableEntity_HasQueryFilterRegistered()
    {
        using AppDbContext context = CreateContext();

        IEnumerable<IEntityType> softDeletableEntityTypes = context.Model.GetEntityTypes()
            .Where(e => typeof(ISoftDeletable).IsAssignableFrom(e.ClrType));

        foreach (IEntityType entityType in softDeletableEntityTypes)
        {
            entityType.GetQueryFilter().Should().NotBeNull(
                $"{entityType.ClrType.Name} implements ISoftDeletable and must register a soft-delete query filter (R-BE-023)");
        }
    }

    [Fact]
    public void DomainAssembly_EveryConcreteAuditableEntity_HasMatchingConfigurationInInfrastructure()
    {
        Assembly domainAssembly = typeof(AuditableEntity).Assembly;
        Assembly infrastructureAssembly = typeof(AppDbContext).Assembly;

        var entityTypes = domainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AuditableEntity).IsAssignableFrom(t))
            .ToList();

        var configuredEntityTypes = infrastructureAssembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToList();

        // This is the regression guard called out by the task: adding a new AuditableEntity
        // subclass without a matching IEntityTypeConfiguration<T> must fail this test.
        const string becauseReason = "every AuditableEntity in Domain must have a matching IEntityTypeConfiguration<T> in Infrastructure";
        entityTypes.Should().BeSubsetOf(configuredEntityTypes, becauseReason);
    }

    [Fact]
    public void Model_ShorfahSectionSlaDefault_UsesNaturalSectionTypeKey()
    {
        using AppDbContext context = CreateContext();

        IEntityType? entityType = context.Model.FindEntityType(typeof(Icbank.Platform.Domain.Shorfah.ShorfahSectionSlaDefault));

        entityType.Should().NotBeNull();
        IKey? primaryKey = entityType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle().Which.Name.Should().Be("SectionType");
    }

    private static AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(FakeConnectionString)
            .Options;
        return new AppDbContext(options);
    }
}
