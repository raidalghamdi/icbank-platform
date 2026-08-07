namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for generating cryptographically random, policy-compliant temporary passwords, shared by
/// account creation and admin-triggered password resets so both paths produce passwords with the
/// same strength guarantee instead of two hand-rolled implementations drifting apart.
/// </summary>
public interface ITemporaryPasswordGenerator
{
    /// <summary>Generates a new random temporary password.</summary>
    /// <returns>The plaintext password. Callers must never log it (R-BE-054).</returns>
    string Generate();
}
