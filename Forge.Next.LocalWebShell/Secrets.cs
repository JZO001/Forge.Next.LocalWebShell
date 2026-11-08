using System.Security.Cryptography;
using System.Text;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// The two secrets of one process: the launch token, which travels once in the launch link, and the value of the
/// session cookie it is exchanged for. Both are new at every start and live only in memory.
/// </summary>
/// <remarks>
/// The cookie value is a separate random value rather than the token itself, so the token never sits in the browser's
/// cookie store. <see cref="ToString"/> never shows either.
/// </remarks>
internal sealed class Secrets
{
    /// <summary>The size of each secret: 32 random bytes, 256 bits.</summary>
    private const int SecretBytes = 32;

    /// <summary>Creates both secrets from the cryptographic random number generator.</summary>
    public Secrets()
    {
        LaunchToken = NewSecret();
        CookieValue = NewSecret();
    }

    /// <summary>The launch token, URL-safe base64 (43 characters).</summary>
    public string LaunchToken { get; }

    /// <summary>The value of the session cookie, URL-safe base64 (43 characters).</summary>
    public string CookieValue { get; }

    /// <summary>Whether <paramref name="candidate"/> is the launch token, compared in constant time.</summary>
    /// <param name="candidate">The value from the request; <see langword="null"/> is never a match.</param>
    /// <returns><see langword="true"/> for the launch token.</returns>
    public bool IsLaunchToken(string? candidate) => Matches(LaunchToken, candidate);

    /// <summary>Whether <paramref name="candidate"/> is the session cookie's value, compared in constant time.</summary>
    /// <param name="candidate">The cookie value from the request; <see langword="null"/> is never a match.</param>
    /// <returns><see langword="true"/> for the session cookie's value.</returns>
    public bool IsCookieValue(string? candidate) => Matches(CookieValue, candidate);

    /// <summary>The type name only: the secrets must not reach a log through a <c>ToString</c> call.</summary>
    /// <returns>The type name.</returns>
    public override string ToString() => nameof(Secrets);

    /// <summary>A new random secret in URL-safe base64 without padding, so it needs no encoding in a query string.</summary>
    /// <returns>The secret.</returns>
    internal static string NewSecret()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(SecretBytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Compares in constant time, so the time of a refusal does not tell how many leading characters were right. The
    /// length can differ quickly; it is not secret.
    /// </summary>
    private static bool Matches(string expected, string? candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(candidate));
    }
}
