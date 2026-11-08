using System.Net;
using ErrorOr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forge.Next.LocalWebShell.Tests.TestHelpers;

/// <summary>
/// A real local web shell on a real Kestrel, on a free loopback port, with two endpoints: <c>/</c> answers
/// <c>home</c>, <c>/echo</c> answers the query string it got. The browser is off unless a test turns it on.
/// </summary>
internal sealed class TestShell : IAsyncDisposable
{
    private TestShell(WebApplication app, Session session)
    {
        App = app;
        Session = session;
    }

    /// <summary>The running application.</summary>
    public WebApplication App { get; }

    /// <summary>The started session.</summary>
    public Session Session { get; }

    /// <summary>The launch token, read from the launch link.</summary>
    public string Token => Session.LaunchUrl.Query.Substring("?token=".Length);

    /// <summary>The port the shell listens on.</summary>
    public int Port => Session.Address.Port;

    /// <summary>The session cookie's name for this shell.</summary>
    public string CookieName => $"Forge.LocalWebShell-{Port}";

    /// <summary>Builds and starts a shell; fails the test if the start returns an error.</summary>
    /// <param name="configure">Changes to the settings (after the browser is turned off).</param>
    /// <param name="arrange">Changes to the builder before the shell is added (services, configuration).</param>
    /// <returns>The started shell.</returns>
    public static async Task<TestShell> StartAsync(Action<Options>? configure = null, Action<WebApplicationBuilder>? arrange = null)
    {
        WebApplication app = Build(configure, arrange);
        ErrorOr<Session> started = await app.StartLocalWebShellAsync();
        if (started.IsError)
        {
            await app.DisposeAsync();
            throw new InvalidOperationException($"The test shell did not start: {started.FirstError.Code} {started.FirstError.Description}");
        }

        return new TestShell(app, started.Value);
    }

    /// <summary>Builds the application without starting it.</summary>
    /// <param name="configure">Changes to the settings (after the browser is turned off).</param>
    /// <param name="arrange">Changes to the builder before the shell is added.</param>
    /// <returns>The built application.</returns>
    public static WebApplication Build(Action<Options>? configure = null, Action<WebApplicationBuilder>? arrange = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Logging.ClearProviders();
        arrange?.Invoke(builder);
        builder.AddLocalWebShell(options =>
        {
            options.OpenBrowser = false;
            configure?.Invoke(options);
        });

        WebApplication app = builder.Build();
        app.MapGet("/", () => "home");
        app.MapGet("/echo", (HttpRequest request) => request.QueryString.Value ?? string.Empty);
        return app;
    }

    /// <summary>A client that neither follows redirects nor keeps cookies, so every header can be checked by hand.</summary>
    /// <returns>The client; dispose it.</returns>
    public HttpClient RawClient() =>
        new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = Session.Address };

    /// <summary>A browser-like client that follows redirects and keeps cookies in <paramref name="cookies"/>.</summary>
    /// <param name="cookies">The cookie store, shared between clients to imitate one browser.</param>
    /// <returns>The client; dispose it.</returns>
    public HttpClient BrowserClient(CookieContainer cookies) =>
        new(new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true, CookieContainer = cookies }) { BaseAddress = Session.Address };

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }
}
