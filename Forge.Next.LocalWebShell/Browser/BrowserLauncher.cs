using System.ComponentModel;
using System.Diagnostics;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Forge.Next.LocalWebShell.Browser;

/// <summary>
/// The default <see cref="IBrowserLauncher"/>: the user's default browser, never with elevated or root rights and
/// never inside the terminal.
/// </summary>
/// <remarks>
/// <see cref="BrowserRouting"/> decides; this class carries the decision out:
/// <list type="bullet">
/// <item><b>Windows, not elevated:</b> the shell opens the link (<c>UseShellExecute</c>), as a double-click would.</item>
/// <item><b>Windows, elevated:</b> the desktop shell, Explorer, opens it with the user's normal rights
/// (<see cref="DesktopShell"/>). Without an Explorer shell (Server Core) nothing is opened.</item>
/// <item><b>Linux and FreeBSD:</b> <c>xdg-open</c>, only in a graphical session (<c>DISPLAY</c> or
/// <c>WAYLAND_DISPLAY</c>) and not as root.</item>
/// <item><b>macOS:</b> <c>open</c>, not as root.</item>
/// </list>
/// When nothing is opened, the error says why, and the application prints the launch link.
/// </remarks>
internal sealed class BrowserLauncher(ILogger<BrowserLauncher> logger) : IBrowserLauncher
{
    /// <summary>The <c>SW_SHOWNORMAL</c> show command for the browser window.</summary>
    private const int ShowNormal = 1;

    /// <inheritdoc />
    public ErrorOr<Success> Open(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        string target = url.AbsoluteUri;

        BrowserDecision decision = BrowserRouting.Choose(BrowserRouting.CurrentPlatform(), Environment.IsPrivilegedProcess, Environment.GetEnvironmentVariable);

        try
        {
            switch (decision.Route)
            {
                case BrowserRoute.ShellOpen when OperatingSystem.IsWindows():
                    using (Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }))
                    {
                        return Result.Success;
                    }

                case BrowserRoute.DesktopShell when OperatingSystem.IsWindows():
                    return OpenThroughDesktopShell(target);

                case BrowserRoute.XdgOpen:
                    return StartHelper("xdg-open", target);

                case BrowserRoute.MacOpen:
                    return StartHelper("open", target);

                default:
                    return Errors.BrowserNotOpened(decision.Reason ?? "this operating system is not supported.");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            // A boundary: a helper that is missing (no xdg-open) or fails to start becomes an error; the caller prints the link.
            return Errors.BrowserNotOpened($"the browser could not be started ({exception.Message}).");
        }
    }

    /// <summary>Opens the link through the desktop shell; logs why when it cannot.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private ErrorOr<Success> OpenThroughDesktopShell(string target)
    {
        ErrorOr<Success> opened = DesktopShell.ShellExecuteUnelevated(target, string.Empty, ShowNormal);
        if (opened.IsError && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("The process is elevated and the desktop shell could not open the browser unelevated, so no browser was opened: {Reason}", opened.FirstError.Description);
        }

        return opened;
    }

    /// <summary>
    /// Starts a helper (<c>xdg-open</c>, <c>open</c>) with the link as its only argument, without a shell. Its standard input
    /// is closed at once, so nothing it starts can read from the application's console.
    /// </summary>
    private static ErrorOr<Success> StartHelper(string command, string target)
    {
        ProcessStartInfo info = new(command) { UseShellExecute = false, RedirectStandardInput = true };
        info.ArgumentList.Add(target);
        using Process? process = Process.Start(info);
        process?.StandardInput.Close();
        return Result.Success;
    }
}
