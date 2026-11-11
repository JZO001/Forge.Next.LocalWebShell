using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// Checks <see cref="Options"/> when the application is built and when it starts, so a wrong setting stops
/// it with every problem named, instead of failing later on the first request.
/// </summary>
internal sealed class OptionsValidation : IValidateOptions<Options>
{
    /// <summary>The characters a cookie name must not contain (RFC 6265 token separators that matter in practice).</summary>
    private static readonly char[] ForbiddenCookieNameCharacters = [' ', '\t', ';', ',', '=', '"', '\\', '(', ')', '<', '>', '@', ':', '/', '[', ']', '?', '{', '}'];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.Port is < 0 or > 65535)
        {
            failures.Add($"LocalWebShell:Port must be between 0 (the operating system picks a free port) and 65535; it is {options.Port}.");
        }

        if (string.IsNullOrWhiteSpace(options.CookieName))
        {
            failures.Add("LocalWebShell:CookieName must not be empty.");
        }
        else if (options.CookieName.IndexOfAny(ForbiddenCookieNameCharacters) >= 0)
        {
            failures.Add($"LocalWebShell:CookieName '{options.CookieName}' is not a valid cookie name: no spaces, ';', ',', '=' or other separators.");
        }

        // None would send the cookie on every cross-site request, which is the attack the cookie exists to stop.
        if (options.CookieSameSite is not (SameSiteMode.Strict or SameSiteMode.Lax))
        {
            failures.Add($"LocalWebShell:CookieSameSite must be Strict or Lax; it is {options.CookieSameSite}.");
        }

        if (string.IsNullOrWhiteSpace(options.UnauthorizedTitle))
        {
            failures.Add("LocalWebShell:UnauthorizedTitle must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.UnauthorizedMessage))
        {
            failures.Add("LocalWebShell:UnauthorizedMessage must not be empty.");
        }

        if (!LanguageTags.IsWellFormed(options.UnauthorizedLanguage))
        {
            failures.Add($"LocalWebShell:UnauthorizedLanguage '{options.UnauthorizedLanguage}' is not a language tag such as 'en' or 'pt-BR'.");
        }

        ValidateUnauthorizedTexts(options, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Checks the 401 page's further languages: every key a language tag, which the page writes into <c>lang</c> and
    /// <c>Content-Language</c>, and every entry with both texts.
    /// </summary>
    /// <param name="options">The settings.</param>
    /// <param name="failures">The problems found so far; this adds its own.</param>
    private static void ValidateUnauthorizedTexts(Options options, List<string> failures)
    {
        foreach (KeyValuePair<string, UnauthorizedText> entry in options.UnauthorizedTexts)
        {
            string prefix = $"LocalWebShell:UnauthorizedTexts:{entry.Key}";

            if (!LanguageTags.IsWellFormed(entry.Key))
            {
                failures.Add($"{prefix}: '{entry.Key}' is not a language tag such as 'hu' or 'pt-BR'.");
            }

            if (entry.Value is null)
            {
                failures.Add($"{prefix} has no texts.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Value.Title))
            {
                failures.Add($"{prefix}:Title must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(entry.Value.Message))
            {
                failures.Add($"{prefix}:Message must not be empty.");
            }
        }
    }
}
