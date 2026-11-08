using Forge.Next.LocalWebShell.Browser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.Next.LocalWebShell;

/// <summary>Registers the local web shell on a <see cref="WebApplicationBuilder"/>.</summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// The logging category of ASP.NET Core's request diagnostics. It logs every request's full URL, query string
    /// included, at Information level, and the first request carries the launch token.
    /// </summary>
    internal const string HostingDiagnosticsCategory = "Microsoft.AspNetCore.Hosting.Diagnostics";

    /// <summary>
    /// Turns the application into a local web shell:
    /// <list type="bullet">
    /// <item>Kestrel listens on <c>127.0.0.1</c> only, on <see cref="Options.Port"/> (0: a free port);</item>
    /// <item>a gate in front of the whole pipeline admits only the browser that opened the launch link (a launch token
    /// exchanged for a session cookie) and only with a loopback Host header;</item>
    /// <item>ASP.NET Core's request diagnostics are logged from Warning up only, so the token does not reach the log;</item>
    /// <item>a browser launcher is registered (<see cref="ILocalBrowserLauncher"/>), unless one already is.</item>
    /// </list>
    /// Start the application with
    /// <see cref="WebApplicationExtensions.StartLocalWebShellAsync"/> instead of <c>Run</c>.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configure">
    /// Optional settings in code; they run after the <c>LocalWebShell</c> configuration section is bound, so they win.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <remarks>
    /// Calling it twice is harmless: the registrations are added once, and each <paramref name="configure"/> runs. The
    /// logging filter works for every provider added through <c>builder.Logging</c> (including
    /// <c>builder.Logging.AddSerilog()</c>). If the application replaces the logging factory itself
    /// (<c>builder.Host.UseSerilog()</c> or <c>builder.Services.AddSerilog()</c>), the filter does not apply: set the
    /// <c>Microsoft.AspNetCore.Hosting.Diagnostics</c> category to Warning in Serilog's own configuration.
    /// </remarks>
    public static WebApplicationBuilder AddLocalWebShell(this WebApplicationBuilder builder, Action<Options>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        OptionsBuilder<Options> options = builder.Services
            .AddOptions<Options>()
            .Bind(builder.Configuration.GetSection(Options.SectionName));

        if (configure is not null)
        {
            options.Configure(configure);
        }

        options.ValidateOnStart();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<Options>, OptionsValidation>());
        builder.Services.TryAddSingleton<Secrets>();
        builder.Services.TryAddSingleton<ILocalBrowserLauncher, LocalBrowserLauncher>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<KestrelServerOptions>, KestrelSetup>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, StartupFilter>());

        builder.Logging.AddFilter(HostingDiagnosticsCategory, LogLevel.Warning);

        return builder;
    }
}
