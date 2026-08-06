using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.Admin.Queries;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Admin;

/// <summary>Verifies <see cref="ExportActivityLogQueryValidator"/>.</summary>
public sealed class ExportActivityLogQueryValidatorTests
{
    private readonly ExportActivityLogQueryValidator _validator = new();

    [Fact]
    public void Validate_DateFromAfterDateTo_Fails()
    {
        var query = new ExportActivityLogQuery(1, null, null, new DateTime(2026, 2, 1), new DateTime(2026, 1, 1));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(ExportActivityLogQuery.DateFrom));
    }

    [Fact]
    public void Validate_DateFromBeforeDateTo_Passes()
    {
        var query = new ExportActivityLogQuery(1, null, null, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeUserId_Fails()
    {
        var query = new ExportActivityLogQuery(1, -5, null, null, null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(ExportActivityLogQuery.UserId));
    }

    [Fact]
    public void Validate_NoFilters_Passes()
    {
        var query = new ExportActivityLogQuery(1, null, null, null, null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
