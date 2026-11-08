using System.Net;

namespace Forge.Next.LocalWebShell;

/// <summary>The loopback rules: which Host headers the gate accepts, and whether a server address is a loopback one.</summary>
internal static class LoopbackHosts
{
    /// <summary>
    /// The host names a browser uses for the shell. Anything else in the Host header is refused: a page under a foreign
    /// name that resolves to 127.0.0.1 (DNS rebinding) must not reach the application.
    /// </summary>
    private static readonly string[] AllowedHostNames = ["localhost", "127.0.0.1", "[::1]", "::1"];

    /// <summary>Whether <paramref name="host"/> (the Host header without the port) is a loopback name.</summary>
    /// <param name="host">The host part of the Host header.</param>
    /// <returns><see langword="true"/> for <c>localhost</c>, <c>127.0.0.1</c> and <c>[::1]</c>, in any letter case.</returns>
    public static bool IsAllowedHost(string? host) =>
        !string.IsNullOrEmpty(host) && AllowedHostNames.Contains(host, StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether a server address (as Kestrel reports it, e.g. <c>http://127.0.0.1:5000</c>) is a loopback address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>
    /// <see langword="true"/> for <c>localhost</c> and loopback IP addresses; <see langword="false"/> for any other host,
    /// a wildcard (<c>0.0.0.0</c>, <c>[::]</c>, <c>+</c>, <c>*</c>) or an address that cannot be read.
    /// </returns>
    public static bool IsLoopbackAddress(string? address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(uri.Host.Trim('[', ']'), out IPAddress? ip) && IPAddress.IsLoopback(ip);
    }
}
