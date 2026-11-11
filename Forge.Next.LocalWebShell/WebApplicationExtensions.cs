using System.Net.Sockets;
using ErrorOr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.Next.LocalWebShell;

/// <summary>Starts a <see cref="WebApplication"/> registered with the local web shell.</summary>
public static class WebApplicationExtensions
{
    /// <summary>The logging category of the shell's start.</summary>
    private const string LogCategory = "Forge.Next.LocalWebShell";

    /// <summary>
    /// Starts the application, checks that it listens on a loopback address only, and then, not before, opens the
    /// browser at the launch link (if <see cref="Options.OpenBrowser"/> is on). Afterwards wait with
    /// <c>app.WaitForShutdownAsync()</c>.
    /// </summary>
    /// <param name="app">The application, built from a builder on which
    /// <see cref="WebApplicationBuilderExtensions.AddLocalWebShell"/> was called.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    /// <returns>
    /// The session (the address, the launch link, the browser's state), or an error:
    /// <see cref="ErrorCodes.PortInUse"/> when the fixed port is taken (the server is not running; dispose
    /// the application), <see cref="ErrorCodes.NotLoopback"/> or <see cref="ErrorCodes.NoAddress"/>
    /// (the server was stopped). A browser that could not be opened is not an error of the start: see
    /// <see cref="Session.Browser"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <see cref="WebApplicationBuilderExtensions.AddLocalWebShell"/> was not called: a programming error.
    /// </exception>
    /// <remarks>
    /// Invalid settings do not get this far: <c>builder.Build()</c> already throws an <see cref="OptionsValidationException"/>
    /// that names every problem, because Kestrel reads the port when the application is built.
    /// </remarks>
    public static async Task<ErrorOr<Session>> StartLocalWebShellAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        Secrets secrets = app.Services.GetService<Secrets>()
            ?? throw new InvalidOperationException("The local web shell is not registered: call builder.AddLocalWebShell() before builder.Build().");
        Options options = app.Services.GetRequiredService<IOptions<Options>>().Value;
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(LogCategory);

        try
        {
            await app.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception) when (IsAddressInUse(exception))
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("The local web shell could not start: port {Port} on the loopback address is already in use", options.Port);
            }

            return Errors.PortInUse(options.Port);
        }

        ErrorOr<Uri> address = LoopbackAddressOf(app.Services.GetRequiredService<IServer>());
        if (address.IsError)
        {
            // Never leave a server running that listens where it must not, or that cannot be opened.
            await app.StopAsync(CancellationToken.None).ConfigureAwait(false);
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("The local web shell stopped the server: {Reason}", address.FirstError.Description);
            }

            return address.Errors;
        }

        Uri launchUrl = new UriBuilder(address.Value) { Query = $"{Gate.TokenQueryName}={secrets.LaunchToken}" }.Uri;
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("The local web shell listens on {Address}", address.Value);
        }

        if (!options.OpenBrowser)
        {
            return new Session(address.Value, launchUrl, BrowserState.NotRequested, null);
        }

        ErrorOr<Success> opened = OpenBrowser(app.Services.GetRequiredService<IBrowserLauncher>(), launchUrl);
        if (opened.IsError)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("The local web shell did not open the browser: {Reason}", opened.FirstError.Description);
            }

            return new Session(address.Value, launchUrl, BrowserState.NotOpened, opened.FirstError);
        }

        return new Session(address.Value, launchUrl, BrowserState.Opened, null);
    }

    /// <summary>
    /// The address to open, from the addresses the server reports: an error if there is none, or if any of them is not
    /// a loopback address.
    /// </summary>
    /// <param name="server">The started server.</param>
    /// <returns>The loopback address with a trailing slash, or the error.</returns>
    internal static ErrorOr<Uri> LoopbackAddressOf(IServer server)
    {
        ICollection<string> addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];

        string? foreign = addresses.FirstOrDefault(address => !LoopbackHosts.IsLoopbackAddress(address));
        if (foreign is not null)
        {
            return Errors.NotLoopback(foreign);
        }

        string? first = addresses.FirstOrDefault();
        if (first is null || !Uri.TryCreate(first, UriKind.Absolute, out Uri? uri))
        {
            return Errors.NoAddress();
        }

        return new UriBuilder(uri) { Path = "/" }.Uri;
    }

    /// <summary>Asks the launcher to open the link; an exception from a replaced launcher becomes an error, not a crash.</summary>
    private static ErrorOr<Success> OpenBrowser(IBrowserLauncher launcher, Uri launchUrl)
    {
        try
        {
            return launcher.Open(launchUrl);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A boundary: a browser that cannot be opened must not stop a server that runs.
            return Errors.BrowserNotOpened(exception.Message);
        }
    }

    /// <summary>Whether the start failed because the port was taken (Kestrel wraps the socket error in an IOException).</summary>
    private static bool IsAddressInUse(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is AddressInUseException or SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse })
            {
                return true;
            }
        }

        return false;
    }
}
