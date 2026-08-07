using System.Security.Cryptography;
using Icbank.Platform.Application.Common.Interfaces;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Default <see cref="ITemporaryPasswordGenerator"/> implementation. Uses a cryptographically
/// secure RNG (never <see cref="Random"/>) and an alphabet excluding visually-ambiguous
/// characters (<c>0/O</c>, <c>1/l/I</c>) since generated passwords are sometimes read aloud or
/// copy-typed by an end user during first login.
/// </summary>
public sealed class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    private const int PasswordLength = 24;
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%^&*()-_=+";
    private const string Alphabet = Upper + Lower + Digits + Special;

    /// <inheritdoc />
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(PasswordLength);
        var chars = new char[PasswordLength];
        for (var index = 0; index < PasswordLength; index++)
        {
            chars[index] = Alphabet[bytes[index] % Alphabet.Length];
        }

        return new string(chars);
    }
}
