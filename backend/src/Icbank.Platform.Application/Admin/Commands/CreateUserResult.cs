namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>The result of a successful <see cref="CreateUserCommand"/>.</summary>
/// <param name="User">The created user's detail projection.</param>
/// <param name="TemporaryPassword">The one-time plaintext password, present only when the caller didn't supply one.</param>
public sealed record CreateUserResult(UserDetailDto User, string? TemporaryPassword);
