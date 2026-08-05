namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for password hashing/verification (DOTNET-CONVENTIONS.md §5.2: PBKDF2-HMAC-SHA256 via
/// <c>Microsoft.AspNetCore.Identity.PasswordHasher&lt;TUser&gt;</c>, 100,000+ iterations — the
/// conventions doc's own mandated algorithm for any locally-stored credential; never hand-rolled).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password for storage. Never logged, never returned to a caller.</summary>
    /// <param name="password">The plaintext password.</param>
    /// <returns>The opaque hash string, safe to persist.</returns>
    string HashPassword(string password);

    /// <summary>Verifies a plaintext password against a stored hash, using constant-time comparison.</summary>
    /// <param name="hashedPassword">The previously stored hash.</param>
    /// <param name="providedPassword">The plaintext password supplied by the caller.</param>
    /// <returns><c>true</c> if the password matches.</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
