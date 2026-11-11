using System.Net;
using Forge.Next.LocalWebShell.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;

namespace Forge.Next.LocalWebShell.Tests;

/// <summary>
/// The gate on a real Kestrel: who gets in (the launch token, then the cookie), who does not (no cookie, a wrong
/// token, a wrong cookie, a foreign Host header), and what the token exchange sends back.
/// </summary>
public class GateTests
{
    /// <summary>Without the token or the cookie every path gets the 401 page, the Blazor hub and static files too.</summary>
    [Theory]
    [InlineData("/")]
    [InlineData("/echo")]
    [InlineData("/_blazor/negotiate")]
    [InlineData("/css/site.css")]
    [InlineData("/docs/index.html")]
    public async Task InvokeAsync_WhenNeitherTokenNorCookie_ShouldReturnTheUnauthorizedPage(string path)
    {
        await using TestShell shell = await TestShell.StartAsync();
        using HttpClient client = shell.RawClient();

        using HttpResponseMessage response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/html");
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        (await response.Content.ReadAsStringAsync()).ShouldContain("Open the link from the console window");
    }

    /// <summary>
    /// The 401 page follows the browser's language where the settings have it, names the language it chose, and says that
    /// the answer depends on the header; without a fitting language it is in the default one.
    /// </summary>
    [Theory]
    [InlineData("hu-HU,hu;q=0.9,en;q=0.8", "hu", "Nyisd meg a konzol linkjét")]
    [InlineData("de-DE", "en", "Open the link from the console window")]
    [InlineData(null, "en", "Open the link from the console window")]
    public async Task InvokeAsync_WhenTheBrowserSendsItsLanguages_ShouldAnswerInTheFirstThatFits(string? acceptLanguage, string language, string title)
    {
        await using TestShell shell = await TestShell.StartAsync(options =>
            options.UnauthorizedTexts["hu"] = new UnauthorizedText { Title = "Nyisd meg a konzol linkjét", Message = "A linket a konzolablak mutatja." });
        using HttpClient client = shell.RawClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/");
        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentLanguage.ShouldBe([language]);
        response.Headers.Vary.ShouldContain("Accept-Language");
        string html = await response.Content.ReadAsStringAsync();
        html.ShouldContain($"<html lang=\"{language}\">");
        html.ShouldContain($"<title>{title}</title>");
    }

    /// <summary>The token sets an HttpOnly, SameSite=Strict cookie and redirects to the same path without the token.</summary>
    [Fact]
    public async Task InvokeAsync_WhenTheTokenIsRight_ShouldSetTheCookieAndRedirectWithoutTheToken()
    {
        await using TestShell shell = await TestShell.StartAsync();
        using HttpClient client = shell.RawClient();

        using HttpResponseMessage response = await client.GetAsync($"/echo?a=1&token={shell.Token}&b=2");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe("/echo?a=1&b=2");
        response.Headers.GetValues("Referrer-Policy").ShouldContain("no-referrer");
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();

        string setCookie = response.Headers.GetValues("Set-Cookie").Single();
        setCookie.ShouldStartWith($"{shell.CookieName}=");
        setCookie.ShouldContain("httponly", Case.Insensitive);
        setCookie.ShouldContain("samesite=strict", Case.Insensitive);
        setCookie.ShouldContain("path=/", Case.Insensitive);
        setCookie.ShouldNotContain(shell.Token);
        setCookie.ShouldNotContain("expires", Case.Insensitive);
    }

    /// <summary>A browser that followed the launch link reaches the application, and keeps reaching it with the cookie.</summary>
    [Fact]
    public async Task InvokeAsync_WhenTheBrowserFollowedTheLaunchLink_ShouldPassLaterRequestsWithTheCookie()
    {
        await using TestShell shell = await TestShell.StartAsync();
        CookieContainer cookies = new();
        using HttpClient browser = shell.BrowserClient(cookies);

        string first = await browser.GetStringAsync(shell.Session.LaunchUrl);
        string later = await browser.GetStringAsync("/echo?x=1");

        first.ShouldBe("home");
        later.ShouldBe("?x=1");
    }

    /// <summary>A wrong token gives nothing: no cookie, the 401 page.</summary>
    [Fact]
    public async Task InvokeAsync_WhenTheTokenIsWrong_ShouldReturnTheUnauthorizedPageWithoutACookie()
    {
        await using TestShell shell = await TestShell.StartAsync();
        using HttpClient client = shell.RawClient();

        using HttpResponseMessage response = await client.GetAsync("/?token=wrong-token");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }

    /// <summary>A cookie with the right name but a wrong value does not pass.</summary>
    [Fact]
    public async Task InvokeAsync_WhenTheCookieValueIsWrong_ShouldReturnTheUnauthorizedPage()
    {
        await using TestShell shell = await TestShell.StartAsync();
        using HttpClient client = shell.RawClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", $"{shell.CookieName}=not-the-value");

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A Host header that is not a loopback name is refused before anything else (DNS rebinding).</summary>
    [Fact]
    public async Task InvokeAsync_WhenTheHostHeaderIsForeign_ShouldReturnBadRequest()
    {
        await using TestShell shell = await TestShell.StartAsync();
        using HttpClient client = shell.RawClient();
        using HttpRequestMessage request = new(HttpMethod.Get, $"/?token={shell.Token}");
        request.Headers.Host = $"evil.example:{shell.Port}";

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }

    /// <summary>The loopback names are accepted in the Host header.</summary>
    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public async Task InvokeAsync_WhenTheHostHeaderIsALoopbackName_ShouldAcceptIt(string host)
    {
        await using TestShell shell = await TestShell.StartAsync();
        using HttpClient client = shell.RawClient();
        using HttpRequestMessage request = new(HttpMethod.Get, $"/?token={shell.Token}");
        request.Headers.Host = $"{host}:{shell.Port}";

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    /// <summary>The Lax setting is applied to the cookie.</summary>
    [Fact]
    public async Task InvokeAsync_WhenSameSiteIsLax_ShouldSetALaxCookie()
    {
        await using TestShell shell = await TestShell.StartAsync(options => options.CookieSameSite = SameSiteMode.Lax);
        using HttpClient client = shell.RawClient();

        using HttpResponseMessage response = await client.GetAsync($"/?token={shell.Token}");

        response.Headers.GetValues("Set-Cookie").Single().ShouldContain("samesite=lax", Case.Insensitive);
    }

    /// <summary>
    /// Two shells on one machine, one browser: browsers keep cookies per host, not per port, so without the port in the
    /// cookie name the second sign-in would lock the first tab out.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenTwoShellsShareOneBrowser_ShouldKeepBothSignedIn()
    {
        await using TestShell first = await TestShell.StartAsync();
        await using TestShell second = await TestShell.StartAsync();
        CookieContainer cookies = new();
        using HttpClient firstBrowser = first.BrowserClient(cookies);
        using HttpClient secondBrowser = second.BrowserClient(cookies);

        await firstBrowser.GetStringAsync(first.Session.LaunchUrl);
        await secondBrowser.GetStringAsync(second.Session.LaunchUrl);
        using HttpResponseMessage firstAgain = await firstBrowser.GetAsync("/");

        firstAgain.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>The token leaves the redirect target; the rest of the query stays, in order.</summary>
    [Theory]
    [InlineData("/a", "?token=x&b=1", "/a?b=1")]
    [InlineData("/a", "?b=1&TOKEN=x&c=2", "/a?b=1&c=2")]
    [InlineData("", "?token=x", "/")]
    [InlineData("/", "?token=x", "/")]
    public void TargetWithoutToken_WhenTheQueryHasTheToken_ShouldDropOnlyTheToken(string path, string query, string expected)
    {
        DefaultHttpContext context = new();
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);

        Gate.TargetWithoutToken(context.Request).ShouldBe(expected);
    }

    /// <summary>A path a browser would read as another host never becomes the redirect target.</summary>
    [Theory]
    [InlineData("//evil.example/x")]
    [InlineData("/\\evil.example/x")]
    public void TargetWithoutToken_WhenThePathLooksLikeAnotherHost_ShouldRedirectToTheRoot(string path)
    {
        DefaultHttpContext context = new();
        context.Request.Path = path;
        context.Request.QueryString = new QueryString("?token=x");

        Gate.TargetWithoutToken(context.Request).ShouldBe("/");
    }
}
