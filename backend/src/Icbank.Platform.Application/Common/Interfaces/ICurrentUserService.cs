namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port exposing the identity of the caller for the current request. Implemented in
/// Infrastructure/Api by reading the authenticated <c>ClaimsPrincipal</c>; consumed by the audit
/// interceptor (R-BE-022) so <c>CreatedBy</c>/<c>UpdatedBy</c> are populated without Application
/// or Domain knowing anything about ASP.NET Core or JWTs.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Gets the identifier of the currently authenticated user, or a system marker for background/anonymous work.</summary>
    string UserId { get; }
}
