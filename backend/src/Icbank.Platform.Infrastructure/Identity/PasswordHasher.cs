using Icbank.Platform.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Implements <see cref="IPasswordHasher"/> using ASP.NET Core Identity's
/// <see cref="Microsoft.AspNetCore.Identity.PasswordHasher{TUser}"/> — PBKDF2-HMAC-SHA256,
/// 100,000+ iterations, the exact algorithm DOTNET-CONVENTIONS.md §5.2 mandates for any
/// locally-stored credential ("do not hand-roll hashing"). This is the in-box .NET 8 default;
/// no new NuGet dependency is introduced.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasherSubject Subject = new();

    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<PasswordHasherSubject> _innerHasher = new();

    /// <inheritdoc />
    public string HashPassword(string password) => _innerHasher.HashPassword(Subject, password);

    /// <inheritdoc />
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        PasswordVerificationResult result = _innerHasher.VerifyHashedPassword(Subject, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
