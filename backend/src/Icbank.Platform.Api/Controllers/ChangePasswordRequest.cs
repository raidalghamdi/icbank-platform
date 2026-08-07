namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/auth/change-password</c>.</summary>
/// <param name="CurrentPassword">The caller's current temporary password.</param>
/// <param name="NewPassword">The replacement password.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
