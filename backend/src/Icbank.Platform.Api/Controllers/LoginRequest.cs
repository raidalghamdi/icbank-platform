namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/auth/login</c>.</summary>
/// <param name="Email">The account email.</param>
/// <param name="Password">The plaintext password.</param>
public sealed record LoginRequest(string Email, string Password);
