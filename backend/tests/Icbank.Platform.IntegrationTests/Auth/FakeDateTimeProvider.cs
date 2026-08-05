using Icbank.Platform.Application.Common.Interfaces;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Mutable <see cref="IDateTimeProvider"/> test double so integration tests can control "now"
/// deterministically (task rule: no <see cref="DateTime.Now"/>/random in tests). Mirrors the
/// NSubstitute-based fakes already used for <see cref="IDateTimeProvider"/> at the unit-test
/// layer, but as a plain settable class since <see cref="AuthWebApplicationFactory"/> registers
/// it once per factory instance and tests need to advance the clock across multiple HTTP calls
/// within the same test.
/// </summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    /// <summary>Gets or sets the fixed UTC instant this provider returns for <see cref="UtcNow"/>.</summary>
    public DateTimeOffset FixedUtcNow { get; set; } = new(2026, 8, 5, 6, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public DateTimeOffset UtcNow => FixedUtcNow;

    /// <inheritdoc />
    public DateTimeOffset RiyadhNow => FixedUtcNow.ToOffset(TimeSpan.FromHours(3));
}
