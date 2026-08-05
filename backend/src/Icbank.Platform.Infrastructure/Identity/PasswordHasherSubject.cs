namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Password hashing marker type. <see cref="Microsoft.AspNetCore.Identity.PasswordHasher{TUser}"/>
/// is generic over a user type purely for its optional per-user hashing-options hook, which this
/// platform doesn't use — an empty marker class avoids pulling the full Identity user model into
/// Infrastructure for no reason.
/// </summary>
public sealed class PasswordHasherSubject
{
}
