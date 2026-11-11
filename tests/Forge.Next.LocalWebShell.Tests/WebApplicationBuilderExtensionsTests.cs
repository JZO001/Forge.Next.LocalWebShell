using Forge.Next.LocalWebShell.Tests.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.Next.LocalWebShell.Tests;

/// <summary>
/// The registration: the logging filter that keeps the token out of the log, idempotence, a replaced launcher, and the
/// binding of the settings.
/// </summary>
public class WebApplicationBuilderExtensionsTests
{
    /// <summary>The request diagnostics, which log the full URL with the token, are on from Warning only.</summary>
    [Fact]
    public async Task AddLocalWebShell_WhenAdded_ShouldLogRequestDiagnosticsFromWarningOnly()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new NullCapturingProvider());
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        builder.AddLocalWebShell();
        await using WebApplication app = builder.Build();
        ILogger diagnostics = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(WebApplicationBuilderExtensions.HostingDiagnosticsCategory);

        diagnostics.IsEnabled(LogLevel.Information).ShouldBeFalse();
        diagnostics.IsEnabled(LogLevel.Warning).ShouldBeTrue();
    }

    /// <summary>Adding the shell twice does not bind the port twice or put the gate twice in the pipeline.</summary>
    [Fact]
    public async Task AddLocalWebShell_WhenCalledTwice_ShouldRegisterEverythingOnce()
    {
        await using TestShell shell = await TestShell.StartAsync(arrange: builder => builder.AddLocalWebShell());

        shell.App.Services.GetServices<IStartupFilter>().OfType<StartupFilter>().Count().ShouldBe(1);
        using HttpClient client = shell.RawClient();
        (await client.GetAsync($"/?token={shell.Token}")).StatusCode.ShouldBe(System.Net.HttpStatusCode.Redirect);
    }

    /// <summary>A launcher registered before the shell is kept; the default is added only when none is there.</summary>
    [Fact]
    public async Task AddLocalWebShell_WhenALauncherIsAlreadyRegistered_ShouldKeepIt()
    {
        IBrowserLauncher own = NSubstitute.Substitute.For<IBrowserLauncher>();

        await using TestShell shell = await TestShell.StartAsync(arrange: builder => builder.Services.AddSingleton(own));

        shell.App.Services.GetRequiredService<IBrowserLauncher>().ShouldBeSameAs(own);
    }

    /// <summary>
    /// The 401 page's further languages bind from configuration, and their keys stay case-insensitive after the binding.
    /// </summary>
    [Fact]
    public async Task AddLocalWebShell_WhenTheConfigurationHasFurtherLanguages_ShouldBindThem()
    {
        await using TestShell shell = await TestShell.StartAsync(arrange: builder => builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["LocalWebShell:UnauthorizedTexts:hu:Title"] = "Cím",
                ["LocalWebShell:UnauthorizedTexts:hu:Message"] = "Üzenet",
            }));

        Options options = shell.App.Services.GetRequiredService<IOptions<Options>>().Value;

        options.UnauthorizedTexts["HU"].Title.ShouldBe("Cím");
        options.UnauthorizedTexts["hu"].Message.ShouldBe("Üzenet");
    }

    /// <summary>A null builder is a programming error.</summary>
    [Fact]
    public void AddLocalWebShell_WhenTheBuilderIsNull_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => WebApplicationBuilderExtensions.AddLocalWebShell(null!));
    }

    /// <summary>A provider that enables every level, so the filter rules alone decide.</summary>
    private sealed class NullCapturingProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new EnabledLogger();

        public void Dispose()
        {
        }

        private sealed class EnabledLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
            }
        }
    }
}
