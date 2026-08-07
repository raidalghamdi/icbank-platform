namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>Port exposing the current request's correlation/trace id and originating IP, for audit records.</summary>
public interface IRequestContext
{
    /// <summary>Gets the current request's correlation id (W3C trace id), or a generated value for non-HTTP contexts.</summary>
    string CorrelationId { get; }

    /// <summary>Gets the caller's IP address, if known.</summary>
    string? IpAddress { get; }
}
