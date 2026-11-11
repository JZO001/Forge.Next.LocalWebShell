using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// The small static page a browser gets without the launch token or the session cookie (HTTP 401). It tells the user
/// where the right link is, in the browser's language where the application gave one, and contains no script and nothing
/// of the application.
/// </summary>
internal static class UnauthorizedPage
{
    /// <summary>
    /// How many of the browser's preferred languages are tried. A browser sends a handful; the limit only keeps a forged
    /// header from making the gate loop over thousands.
    /// </summary>
    internal const int MaxPreferences = 10;

    /// <summary>
    /// The encoder of the page's texts. The page is UTF-8, so letters of every script stay as they are (the default
    /// encoder would turn every non-ASCII letter into an entity); markup characters and quotes are encoded as always.
    /// </summary>
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    /// <summary>
    /// The texts for a request: the first of the browser's preferred languages that the settings have texts for (see
    /// <see cref="Options.UnauthorizedTexts"/>), otherwise the default texts.
    /// </summary>
    /// <param name="settings">The shell's settings.</param>
    /// <param name="acceptLanguage">The request's <c>Accept-Language</c> header; empty or malformed means the default texts.</param>
    /// <returns>The texts and their language.</returns>
    public static UnauthorizedPageText Choose(Options settings, StringValues acceptLanguage)
    {
        UnauthorizedPageText fallback = new(settings.UnauthorizedLanguage, settings.UnauthorizedTitle, settings.UnauthorizedMessage);
        if (settings.UnauthorizedTexts.Count == 0
            || !StringWithQualityHeaderValue.TryParseList(acceptLanguage, out IList<StringWithQualityHeaderValue>? preferences))
        {
            return fallback;
        }

        // The candidates in a fixed order, so that two keys of one language always resolve the same way; the default
        // texts come last, so an entry for the default language wins over them when the browser asks for that language.
        List<UnauthorizedPageText> candidates = settings.UnauthorizedTexts
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new UnauthorizedPageText(entry.Key, entry.Value.Title, entry.Value.Message))
            .Append(fallback)
            .ToList();

        // A missing quality is 1; 0 means "not this one", and * says nothing about which. OrderByDescending is stable,
        // so languages of equal quality keep the browser's order.
        IEnumerable<string> ranked = preferences
            .Where(preference => (preference.Quality ?? 1) > 0 && preference.Value.HasValue && preference.Value.Value != "*")
            .OrderByDescending(preference => preference.Quality ?? 1)
            .Take(MaxPreferences)
            .Select(preference => preference.Value.Value!);

        foreach (string tag in ranked)
        {
            if (Match(candidates, tag) is UnauthorizedPageText match)
            {
                return match;
            }
        }

        return fallback;
    }

    /// <summary>The page's HTML, with the texts and their language HTML-encoded.</summary>
    /// <param name="text">The texts to show.</param>
    /// <returns>The HTML document.</returns>
    public static string Render(UnauthorizedPageText text)
    {
        string language = Encoder.Encode(text.Language);
        string title = Encoder.Encode(text.Title);
        string message = Encoder.Encode(text.Message);

        return $"<!DOCTYPE html><html lang=\"{language}\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + $"<title>{title}</title>"
            + "<style>body{font-family:system-ui,sans-serif;max-width:40rem;margin:4rem auto;padding:0 1rem;line-height:1.5;color:#222}h1{font-size:1.4rem}</style>"
            + $"</head><body><h1>{title}</h1><p>{message}</p></body></html>";
    }

    /// <summary>
    /// Writes the page as the response, with status 401, no caching, and the language it chose for the request's
    /// <c>Accept-Language</c>.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="settings">The shell's settings.</param>
    /// <returns>A task that completes when the page is written.</returns>
    public static Task WriteAsync(HttpResponse response, Options settings)
    {
        UnauthorizedPageText text = Choose(settings, response.HttpContext.Request.Headers.AcceptLanguage);

        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.ContentType = "text/html; charset=utf-8";
        response.Headers.CacheControl = "no-store";
        response.Headers.ContentLanguage = text.Language;
        response.Headers.Vary = "Accept-Language";
        return response.WriteAsync(Render(text), response.HttpContext.RequestAborted);
    }

    /// <summary>
    /// The candidate for one preferred language: the same tag, then the tag's language alone, then a candidate of the
    /// same language.
    /// </summary>
    /// <param name="candidates">The configured texts, the default last.</param>
    /// <param name="tag">A language the browser asked for.</param>
    /// <returns>The candidate; null when none fits.</returns>
    private static UnauthorizedPageText? Match(List<UnauthorizedPageText> candidates, string tag)
    {
        string language = LanguageTags.PrimaryLanguage(tag);

        return candidates.Find(candidate => string.Equals(candidate.Language, tag, StringComparison.OrdinalIgnoreCase))
            ?? candidates.Find(candidate => string.Equals(candidate.Language, language, StringComparison.OrdinalIgnoreCase))
            ?? candidates.Find(candidate => string.Equals(LanguageTags.PrimaryLanguage(candidate.Language), language, StringComparison.OrdinalIgnoreCase));
    }
}
