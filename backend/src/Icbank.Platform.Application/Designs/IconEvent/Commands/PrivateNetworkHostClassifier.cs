using System.Net;
using System.Net.Sockets;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Classifies whether a host (IP literal or hostname) falls in private/link-local address space,
/// or is a well-known localhost-style DNS name -- the address-space block list required by SEC-12
/// (BUSINESS-RULES.md §7.5): <c>127.0.0.0/8</c>, <c>10/8</c>, <c>172.16/12</c>, <c>192.168/16</c>,
/// <c>169.254/16</c> (including the cloud metadata endpoint <c>169.254.169.254</c>), <c>::1</c>,
/// <c>fc00::/7</c>, and localhost-style hostnames. This classification only inspects the literal
/// text of the host -- it deliberately does not perform DNS resolution (an attacker-controlled
/// hostname resolving to a private address at request time is a TOCTOU the validator alone cannot
/// close; combined with the "reject all remote references" posture in
/// <see cref="HtmlRemoteResourceScanner"/>, no hostname -- private-looking or not -- is ever
/// allowed through in the first place, so resolution-time re-binding is moot here).
/// </summary>
public static class PrivateNetworkHostClassifier
{
    private static readonly string[] LocalhostStyleNames =
    {
        "localhost",
        "localhost.localdomain",
        "metadata.google.internal",
        "metadata",
    };

    /// <summary>Determines whether <paramref name="host"/> is a private, link-local, loopback, or localhost-style address.</summary>
    /// <param name="host">The bare hostname or IP literal (no scheme, no port, no brackets).</param>
    public static bool IsPrivateOrLinkLocal(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var trimmed = host.Trim().Trim('[', ']');

        if (LocalhostStyleNames.Contains(trimmed, StringComparer.OrdinalIgnoreCase) ||
            trimmed.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(trimmed, out IPAddress? address))
        {
            return IsPrivateOrLinkLocal(address);
        }

        return false;
    }

    private static bool IsPrivateOrLinkLocal(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 127 // 127.0.0.0/8 loopback
                || bytes[0] == 10 // 10.0.0.0/8
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) // 172.16.0.0/12
                || (bytes[0] == 192 && bytes[1] == 168) // 192.168.0.0/16
                || (bytes[0] == 169 && bytes[1] == 254); // 169.254.0.0/16, incl. 169.254.169.254 metadata endpoint
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true; // ::1
            }

            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC; // fc00::/7 (unique local addresses)
        }

        return false;
    }
}
