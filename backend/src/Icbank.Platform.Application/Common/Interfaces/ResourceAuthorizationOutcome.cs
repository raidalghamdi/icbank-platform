namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>The specific reason a <see cref="ResourceAuthorizationResult"/> succeeded or failed.</summary>
public enum ResourceAuthorizationOutcome
{
    /// <summary>The resource exists and the actor is entitled to act on it.</summary>
    Authorized = 0,

    /// <summary>No resource with the supplied id exists.</summary>
    NotFound = 1,

    /// <summary>The resource exists but the actor is not entitled to act on it (e.g. a plain admin targeting a super-admin peer).</summary>
    ForbiddenPeer = 2,
}
