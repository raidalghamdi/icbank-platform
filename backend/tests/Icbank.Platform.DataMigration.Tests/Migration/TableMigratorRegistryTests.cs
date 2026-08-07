using FluentAssertions;
using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Migration.Migrators;

namespace Icbank.Platform.DataMigration.Tests.Migration;

/// <summary>
/// Guards against the exact defect the International Days domain shipped once: five migrators
/// (<see cref="InternationalDayTableMigrator"/>, <see cref="DayYearlyThemeTableMigrator"/>,
/// <see cref="DayActivationTableMigrator"/>, <see cref="IntlDaySourceTableMigrator"/>,
/// <see cref="IntlSearchHistoryTableMigrator"/>) compiled cleanly, had their own transformer
/// tests passing, and were never added to <see cref="TableMigratorRegistry.GetOrderedMigrators"/>
/// -- so a full migration run would have reported success while silently migrating zero rows for
/// that entire domain. Nothing short of a reflection-based inventory check catches this: every
/// other test in this suite exercises a migrator that someone remembered to both write and wire
/// up, which is exactly the class of test that cannot fail for a migrator nobody wired up.
/// </summary>
public sealed class TableMigratorRegistryTests
{
    public static TheoryData<Type> RegisteredMigratorTypes()
    {
        var data = new TheoryData<Type>();
        foreach (Type type in TableMigratorRegistry.GetOrderedMigrators().Select(m => m.GetType()))
        {
            data.Add(type);
        }

        return data;
    }

    [Fact]
    public void GetOrderedMigrators_EveryConcreteITableMigratorInAssembly_IsRegistered()
    {
        Type[] allMigratorTypes = typeof(ITableMigrator).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ITableMigrator).IsAssignableFrom(t))
            .ToArray();

        // Sanity floor: if this drops to zero (e.g. a future refactor moves migrators to another
        // assembly), the test below would trivially and silently pass having found nothing to
        // check. Fail loudly instead so the test's own blind spot cannot regress unnoticed.
        const int knownMigratorCountAtGuardWriteTime = 42;
        allMigratorTypes.Should().HaveCountGreaterOrEqualTo(
            knownMigratorCountAtGuardWriteTime,
            "a lower count means either migrators moved assembly or this discovery query broke");

        Type[] registeredTypes = TableMigratorRegistry.GetOrderedMigrators()
            .Select(m => m.GetType())
            .ToArray();

        IEnumerable<Type> unregistered = allMigratorTypes.Except(registeredTypes);

        unregistered.Should().BeEmpty(
            "every ITableMigrator implementation must be wired into TableMigratorRegistry -- " +
            "a migrator that compiles and has its own unit tests but is never registered here " +
            "will silently migrate zero rows for its entire table with no error and no test " +
            "failure anywhere else (see spec/DATA-MIGRATION-NOTES.md: this exact defect shipped " +
            "once for the five International Days migrators)");
    }

    [Fact]
    public void GetOrderedMigrators_NoDuplicateRegistration_EveryMigratorTypeAppearsExactlyOnce()
    {
        var registeredTypes = TableMigratorRegistry.GetOrderedMigrators()
            .Select(m => m.GetType())
            .ToList();

        registeredTypes.Should().OnlyHaveUniqueItems(
            "registering the same migrator twice would double-process its table and silently " +
            "hide that some other migrator was never added in its place");
    }

    [Fact]
    public void GetOrderedMigrators_NoDuplicateSourceTableName_EveryTableIsMigratedByExactlyOneMigrator()
    {
        IReadOnlyList<ITableMigrator> migrators = TableMigratorRegistry.GetOrderedMigrators();

        migrators.Select(m => m.SourceTableName).Should().OnlyHaveUniqueItems(
            "two migrators claiming the same source table would double-read and double-write it");
    }

    [Fact]
    public void GetOrderedMigrators_ReturnsNonEmptyList()
    {
        TableMigratorRegistry.GetOrderedMigrators().Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(RegisteredMigratorTypes))]
    public void GetOrderedMigrators_EachMigrator_HasNonEmptySourceAndDestinationTableNames(Type migratorType)
    {
        var migrator = (ITableMigrator)Activator.CreateInstance(migratorType)!;

        migrator.SourceTableName.Should().NotBeNullOrWhiteSpace(
            $"{migratorType.Name} must declare which source table it reads from");
        migrator.DestinationTableName.Should().NotBeNullOrWhiteSpace(
            $"{migratorType.Name} must declare which destination table it writes to");
    }
}
