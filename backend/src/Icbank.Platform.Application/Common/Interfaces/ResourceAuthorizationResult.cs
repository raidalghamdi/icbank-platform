namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>The outcome of a resource-level authorization check (SEC-16).</summary>
/// <param name="Outcome">The specific reason authorization succeeded or failed.</param>
public sealed record ResourceAuthorizationResult(ResourceAuthorizationOutcome Outcome)
{
    /// <summary>Gets a canonical authorized result.</summary>
    public static ResourceAuthorizationResult Authorized { get; } = new(ResourceAuthorizationOutcome.Authorized);

    /// <summary>Gets a canonical not-found result.</summary>
    public static ResourceAuthorizationResult NotFound { get; } = new(ResourceAuthorizationOutcome.NotFound);

    /// <summary>Gets a canonical forbidden-peer result (target outranks or equals a protected tier the actor cannot touch).</summary>
    public static ResourceAuthorizationResult ForbiddenPeer { get; } = new(ResourceAuthorizationOutcome.ForbiddenPeer);

    /// <summary>Gets a value indicating whether the resource is reachable by the actor.</summary>
    public bool IsAuthorized => Outcome == ResourceAuthorizationOutcome.Authorized;
}
