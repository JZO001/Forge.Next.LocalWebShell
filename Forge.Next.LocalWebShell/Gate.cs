using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Forge.Next.LocalWebShell;

/// <summary>
/// The middleware in front of the whole application: it admits only requests with a loopback Host header that carry
/// either the launch token (exchanged here for the session cookie) or the session cookie. Everything else, static files
/// and the Blazor hub included, gets HTTP 400 or the 401 page.
/// </summary>
/// <remarks>
/// It is inserted first in the pipeline by <see cref="StartupFilter"/>, so the application cannot place
/// anything before it by mistake. It never logs the query string, because the first request carries the token.
/// </remarks>
internal sealed class Gate(
    RequestDelegate next,
    Secrets secrets,
    IOptions<Options> options,
    ILogger<Gate> logger)
{
    /// <summary>The name of the query parameter that carries the launch token.</summary>
    internal const string TokenQueryName = "token";

    /// <summary>Handles one request.</summary>
    /// <param name="context">The request's context.</param>
    /// <returns>A task that completes when the request is handled.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        if (!LoopbackHosts.IsAllowedHost(request.Host.Host))
        {
            // The host name is not secret; logging it helps to see a rebinding attempt or a misconfigured client.
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Refused a request with the Host header {Host}: only loopback names are accepted", request.Host.Value);
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.Headers.CacheControl = "no-store";
            return;
        }

        Options settings = options.Value;
        string cookieName = CookieNameFor(settings, context.Connection.LocalPort);

        if (request.Query.TryGetValue(TokenQueryName, out StringValues tokens) && tokens.Count == 1 && secrets.IsLaunchToken(tokens[0]))
        {
            SignIn(context, settings, cookieName);
            return;
        }

        if (request.Cookies.TryGetValue(cookieName, out string? cookie) && secrets.IsCookieValue(cookie))
        {
            await next(context);
            return;
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Refused a request to {Path} without the session cookie", request.Path.Value);
        }

        await UnauthorizedPage.WriteAsync(context.Response, settings);
    }

    /// <summary>
    /// The session cookie's name for one port. Browsers keep cookies per host, not per port, so the port is part of the
    /// name: two instances on one machine must not overwrite each other's cookie.
    /// </summary>
    /// <param name="settings">The shell's settings.</param>
    /// <param name="port">The local port of the connection.</param>
    /// <returns>The cookie name, e.g. <c>Forge.LocalWebShell-54321</c>.</returns>
    internal static string CookieNameFor(Options settings, int port) =>
        string.Create(CultureInfo.InvariantCulture, $"{settings.CookieName}-{port}");

    /// <summary>
    /// The address to redirect to after the token was accepted: the same path and query without the token, so the token
    /// leaves the address bar. A path that a browser would read as another host (<c>//host</c>, <c>/\host</c>) becomes
    /// <c>/</c>, so the redirect can never leave the shell.
    /// </summary>
    /// <param name="request">The request that carried the token.</param>
    /// <returns>A relative address.</returns>
    internal static string TargetWithoutToken(HttpRequest request)
    {
        QueryBuilder rest = new();
        foreach (KeyValuePair<string, StringValues> pair in request.Query)
        {
            if (string.Equals(pair.Key, TokenQueryName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string? value in pair.Value)
            {
                rest.Add(pair.Key, value ?? string.Empty);
            }
        }

        string path = (request.PathBase + request.Path).Value ?? string.Empty;
        if (path.Length == 0 || path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith("/\\", StringComparison.Ordinal))
        {
            path = "/";
        }

        return path + rest.ToQueryString().Value;
    }

    /// <summary>Sets the session cookie and redirects to the same page without the token.</summary>
    private void SignIn(HttpContext context, Options settings, string cookieName)
    {
        context.Response.Cookies.Append(cookieName, secrets.CookieValue, new CookieOptions
        {
            HttpOnly = true,
            SameSite = settings.CookieSameSite,
            Path = "/",
            // Loopback HTTP: a Secure cookie is not needed, the traffic never leaves the machine.
            Secure = false,
            IsEssential = true,
        });

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Redirect(TargetWithoutToken(context.Request));

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("A browser was admitted with the launch token on port {Port}", context.Connection.LocalPort);
        }
    }
}
