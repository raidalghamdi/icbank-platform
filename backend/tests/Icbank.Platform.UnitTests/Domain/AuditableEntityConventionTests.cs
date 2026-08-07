using System.Reflection;
using FluentAssertions;
using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.UnitTests.Domain;

/// <summary>
/// Pure reflection-based checks over the Domain assembly (no EF/Infrastructure dependency,
/// consistent with the UnitTests project's layering constraint). Confirms the ported entity set
/// carries the audit/soft-delete/concurrency shape mandated by the conventions doc, and that the
/// Domain project stays free of forbidden framework coupling (R-BE-002).
/// </summary>
public sealed class AuditableEntityConventionTests
{
    private const int ExpectedMinimumAuditableEntityCount = 42;

    [Fact]
    public void DomainAssembly_EveryConcreteAuditableEntity_ExposesAllAuditProperties()
    {
        List<Type> entityTypes = GetConcreteAuditableEntityTypes();

        string[] requiredProperties = { "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "DeletedAt", "RowVersion" };

        foreach (Type entityType in entityTypes)
        {
            foreach (var propertyName in requiredProperties)
            {
                entityType.GetProperty(propertyName).Should().NotBeNull(
                    $"{entityType.Name} inherits AuditableEntity and must expose '{propertyName}'");
            }
        }
    }

    [Fact]
    public void DomainAssembly_EveryConcreteAuditableEntity_ImplementsISoftDeletable()
    {
        List<Type> entityTypes = GetConcreteAuditableEntityTypes();

        foreach (Type entityType in entityTypes)
        {
            typeof(ISoftDeletable).IsAssignableFrom(entityType).Should().BeTrue(
                $"{entityType.Name} must implement ISoftDeletable via the AuditableEntity base (R-BE-023)");
        }
    }

    [Fact]
    public void DomainAssembly_PortedEntityCount_MatchesDataModelTableCount()
    {
        // 43 source tables minus the 1 documented natural-key exception
        // (ShorfahSectionSlaDefault, which intentionally does not derive from AuditableEntity)
        // plus the 1 deliberate normalization join table (AiYearActivationChannel) added during
        // the port (AMBIGUOUS-2 in DATA-MODEL.md) == 42 AuditableEntity-derived classes minimum.
        List<Type> entityTypes = GetConcreteAuditableEntityTypes();

        entityTypes.Count.Should().BeGreaterThanOrEqualTo(ExpectedMinimumAuditableEntityCount);
    }

    [Fact]
    public void DomainAssembly_HasNoReferencesOutsideSystemNamespace()
    {
        Assembly domainAssembly = typeof(AuditableEntity).Assembly;

        IEnumerable<string> referencedAssemblyNames = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        foreach (var name in referencedAssemblyNames)
        {
            var isFrameworkOrSystemAssembly = name.StartsWith("System", StringComparison.Ordinal)
                || name.StartsWith("netstandard", StringComparison.Ordinal)
                || name.StartsWith("mscorlib", StringComparison.Ordinal);

            isFrameworkOrSystemAssembly.Should().BeTrue(
                $"Domain must not reference '{name}' -- R-BE-002 forbids any non-System.* dependency");
        }
    }

    private static List<Type> GetConcreteAuditableEntityTypes() =>
        typeof(AuditableEntity).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AuditableEntity).IsAssignableFrom(t))
            .ToList();
}
