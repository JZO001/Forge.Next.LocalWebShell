using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// The small static page a browser gets without the launch token or the session cookie (HTTP 401). It tells the user
/// where the right link is, and contains no script and nothing of the application.
/// </summary>
internal static class UnauthorizedPage
{
    /// <summary>The page's HTML, with the configured title and message HTML-encoded.</summary>
    /// <param name="settings">The shell's settings.</param>
    /// <returns>The HTML document.</returns>
    public static string Render(Options settings)
    {
        HtmlEncoder encoder = HtmlEncoder.Default;
        string title = encoder.Encode(settings.UnauthorizedTitle);
        string message = encoder.Encode(settings.UnauthorizedMessage);

        return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + $"<title>{title}</title>"
            + "<style>body{font-family:system-ui,sans-serif;max-width:40rem;margin:4rem auto;padding:0 1rem;line-height:1.5;color:#222}h1{font-size:1.4rem}</style>"
            + $"</head><body><h1>{title}</h1><p>{message}</p></body></html>";
    }

    /// <summary>Writes the page as the response, with status 401 and no caching.</summary>
    /// <param name="response">The response.</param>
    /// <param name="settings">The shell's settings.</param>
    /// <returns>A task that completes when the page is written.</returns>
    public static Task WriteAsync(HttpResponse response, Options settings)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.ContentType = "text/html; charset=utf-8";
        response.Headers.CacheControl = "no-store";
        return response.WriteAsync(Render(settings), response.HttpContext.RequestAborted);
    }
}
