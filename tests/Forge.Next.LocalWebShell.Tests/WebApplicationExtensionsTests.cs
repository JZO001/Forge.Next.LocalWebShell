using System.Net;
using System.Net.Sockets;
using ErrorOr;
using Forge.Next.LocalWebShell.Tests.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Forge.Next.LocalWebShell.Tests;

/// <summary>
/// The start: the loopback address, the launch link, the browser (through a substitute launcher), a taken port,
/// invalid settings, a missing registration, and the address check.
/// </summary>
public class WebApplicationExtensionsTests
{
    /// <summary>The shell listens on 127.0.0.1 on a port the operating system picked, and the launch link carries a token.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenStarted_ShouldListenOnTheLoopbackAddressOnly()
    {
        await using TestShell shell = await TestShell.StartAsync();

        shell.Session.Address.Host.ShouldBe("127.0.0.1");
        shell.Session.Address.Port.ShouldBeGreaterThan(0);
        shell.Session.Address.AbsolutePath.ShouldBe("/");
        shell.Session.LaunchUrl.GetLeftPart(UriPartial.Path).ShouldBe(shell.Session.Address.AbsoluteUri);
        shell.Session.LaunchUrl.Query.ShouldStartWith("?token=");
        shell.Token.Length.ShouldBe(43);
        shell.Session.Browser.ShouldBe(BrowserState.NotRequested);
        shell.Session.BrowserError.ShouldBeNull();
    }

    /// <summary>Every start makes a new token.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenStartedTwice_ShouldMakeADifferentTokenEachTime()
    {
        await using TestShell first = await TestShell.StartAsync();
        await using TestShell second = await TestShell.StartAsync();

        first.Token.ShouldNotBe(second.Token);
        first.Port.ShouldNotBe(second.Port);
    }

    /// <summary>With the browser on, the launcher gets the launch link, after the server listens.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenTheBrowserIsOn_ShouldOpenTheLaunchLink()
    {
        ILocalBrowserLauncher launcher = Substitute.For<ILocalBrowserLauncher>();
        Uri? opened = null;
        launcher.Open(Arg.Any<Uri>()).Returns(call =>
        {
            opened = call.Arg<Uri>();
            return Result.Success;
        });

        await using TestShell shell = await TestShell.StartAsync(
            options => options.OpenBrowser = true,
            builder => builder.Services.AddSingleton(launcher));

        opened.ShouldBe(shell.Session.LaunchUrl);
        shell.Session.Browser.ShouldBe(BrowserState.Opened);
        using HttpClient client = new() { BaseAddress = shell.Session.Address };
        (await client.GetAsync("/")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A browser that cannot be opened is reported, and the server keeps running.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenTheBrowserCannotBeOpened_ShouldReportItAndKeepRunning()
    {
        ILocalBrowserLauncher launcher = Substitute.For<ILocalBrowserLauncher>();
        launcher.Open(Arg.Any<Uri>()).Returns(Error.Failure(ErrorCodes.BrowserNotOpened, "no desktop"));

        await using TestShell shell = await TestShell.StartAsync(
            options => options.OpenBrowser = true,
            builder => builder.Services.AddSingleton(launcher));

        shell.Session.Browser.ShouldBe(BrowserState.NotOpened);
        shell.Session.BrowserError!.Value.Code.ShouldBe(ErrorCodes.BrowserNotOpened);
        CookieContainer cookies = new();
        using HttpClient browser = shell.BrowserClient(cookies);
        (await browser.GetStringAsync(shell.Session.LaunchUrl)).ShouldBe("home");
    }

    /// <summary>A launcher that throws does not stop the start: the exception becomes the browser's error.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenTheLauncherThrows_ShouldReportBrowserNotOpened()
    {
        ILocalBrowserLauncher launcher = Substitute.For<ILocalBrowserLauncher>();
        launcher.Open(Arg.Any<Uri>()).Returns<ErrorOr<Success>>(_ => throw new InvalidOperationException("broken launcher"));

        await using TestShell shell = await TestShell.StartAsync(
            options => options.OpenBrowser = true,
            builder => builder.Services.AddSingleton(launcher));

        shell.Session.Browser.ShouldBe(BrowserState.NotOpened);
        shell.Session.BrowserError!.Value.Description.ShouldContain("broken launcher");
    }

    /// <summary>A fixed port that is already taken gives PortInUse, not an exception.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenTheFixedPortIsTaken_ShouldReturnPortInUse()
    {
        using TcpListener occupant = new(IPAddress.Loopback, 0);
        occupant.Start();
        int port = ((IPEndPoint)occupant.LocalEndpoint).Port;
        await using WebApplication app = TestShell.Build(options => options.Port = port);

        ErrorOr<Session> started = await app.StartLocalWebShellAsync();

        started.IsError.ShouldBeTrue();
        started.FirstError.Code.ShouldBe(ErrorCodes.PortInUse);
        started.FirstError.Type.ShouldBe(ErrorType.Conflict);
        started.FirstError.Description.ShouldContain(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>A free fixed port is used as given.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenAFreeFixedPortIsSet_ShouldListenOnIt()
    {
        int port;
        using (TcpListener probe = new(IPAddress.Loopback, 0))
        {
            probe.Start();
            port = ((IPEndPoint)probe.LocalEndpoint).Port;
        }

        await using TestShell shell = await TestShell.StartAsync(options => options.Port = port);

        shell.Port.ShouldBe(port);
    }

    /// <summary>
    /// Invalid settings stop the application already when it is built (Kestrel reads the port then), with every problem
    /// named.
    /// </summary>
    [Fact]
    public void Build_WhenTheSettingsAreInvalid_ShouldThrowWithEveryProblem()
    {
        OptionsValidationException exception = Should.Throw<OptionsValidationException>(() => TestShell.Build(options =>
        {
            options.Port = -1;
            options.CookieName = "bad name";
        }));

        exception.Failures.Count().ShouldBe(2);
    }

    /// <summary>Starting without the registration is a programming error, said plainly.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenTheShellIsNotRegistered_ShouldThrowInvalidOperation()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        await using WebApplication app = builder.Build();

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => app.StartLocalWebShellAsync());

        exception.Message.ShouldContain("AddLocalWebShell");
    }

    /// <summary>A non-loopback address among the server's addresses is refused, whatever its position.</summary>
    [Theory]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://[::]:5000")]
    [InlineData("http://192.168.1.10:5000")]
    [InlineData("http://+:5000")]
    public void LoopbackAddressOf_WhenAnAddressIsNotLoopback_ShouldReturnNotLoopback(string foreign)
    {
        IServer server = ServerWith("http://127.0.0.1:5001", foreign);

        ErrorOr<Uri> address = WebApplicationExtensions.LoopbackAddressOf(server);

        address.IsError.ShouldBeTrue();
        address.FirstError.Code.ShouldBe(ErrorCodes.NotLoopback);
        address.FirstError.Description.ShouldContain(foreign);
    }

    /// <summary>No address is an unexpected error.</summary>
    [Fact]
    public void LoopbackAddressOf_WhenThereIsNoAddress_ShouldReturnNoAddress()
    {
        IServer server = ServerWith();

        ErrorOr<Uri> address = WebApplicationExtensions.LoopbackAddressOf(server);

        address.FirstError.Code.ShouldBe(ErrorCodes.NoAddress);
    }

    /// <summary>A loopback address comes back with a trailing slash.</summary>
    [Fact]
    public void LoopbackAddressOf_WhenTheAddressIsLoopback_ShouldReturnItWithASlash()
    {
        IServer server = ServerWith("http://127.0.0.1:5001");

        ErrorOr<Uri> address = WebApplicationExtensions.LoopbackAddressOf(server);

        address.Value.AbsoluteUri.ShouldBe("http://127.0.0.1:5001/");
    }

    /// <summary>The shell is registered through the builder, so the test can check the services it added.</summary>
    [Fact]
    public async Task StartLocalWebShellAsync_WhenConfiguredFromConfiguration_ShouldUseTheBoundSettings()
    {
        await using TestShell shell = await TestShell.StartAsync(arrange: builder =>
        {
            builder.Configuration["LocalWebShell:CookieSameSite"] = "Lax";
            builder.Configuration["LocalWebShell:UnauthorizedTitle"] = "Use the console link";
        });

        Options options = shell.App.Services.GetRequiredService<IOptions<Options>>().Value;
        options.CookieSameSite.ShouldBe(Microsoft.AspNetCore.Http.SameSiteMode.Lax);
        options.UnauthorizedTitle.ShouldBe("Use the console link");
        // The test helper's code turns the browser off after the binding: code wins.
        options.OpenBrowser.ShouldBeFalse();
    }

    /// <summary>A server substitute that reports the given addresses.</summary>
    private static IServer ServerWith(params string[] addresses)
    {
        IServerAddressesFeature feature = Substitute.For<IServerAddressesFeature>();
        feature.Addresses.Returns(addresses.ToList());
        FeatureCollection features = new();
        features.Set(feature);
        IServer server = Substitute.For<IServer>();
        server.Features.Returns(features);
        return server;
    }
}
