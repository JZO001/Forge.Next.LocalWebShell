namespace Forge.Next.LocalWebShell.Browser;

/// <summary>
/// Decides how to open the browser, from the operating system, the process's privilege and the environment. A pure
/// function, so every combination is testable on any operating system; <see cref="LocalBrowserLauncher"/> carries it out.
/// </summary>
/// <remarks>
/// The rules, one per invariant:
/// <list type="bullet">
/// <item><b>Never a privileged browser.</b> Windows elevated: only through the desktop shell, which runs with the user's
/// normal rights. Unix or macOS as root: not at all, because a browser running as root would give every page it opens
/// root rights (and Chromium-based browsers refuse to run as root anyway).</item>
/// <item><b>Never a browser in the terminal.</b> Unix without a graphical session: not at all, because <c>xdg-open</c>
/// would fail or start a text browser in the same terminal and take over the console.</item>
/// </list>
/// In every "not at all" case the application prints the launch link instead.
/// </remarks>
internal static class BrowserRouting
{
    /// <summary>The X11 display variable; set in an X11 session (and under XWayland).</summary>
    internal const string X11DisplayVariable = "DISPLAY";

    /// <summary>The Wayland display variable; set in a Wayland session.</summary>
    internal const string WaylandDisplayVariable = "WAYLAND_DISPLAY";

    /// <summary>The reason for not opening a browser as root.</summary>
    private const string RootReason =
        "the application runs as root, and a browser must not get root rights. Open the link as your normal user.";

    /// <summary>The operating system family this process runs on.</summary>
    /// <returns>The family.</returns>
    public static BrowserPlatform CurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return BrowserPlatform.Windows;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return BrowserPlatform.Unix;
        }

        return OperatingSystem.IsMacOS() ? BrowserPlatform.MacOS : BrowserPlatform.Other;
    }

    /// <summary>Chooses the route.</summary>
    /// <param name="platform">The operating system family.</param>
    /// <param name="privileged">
    /// Whether the process is elevated (Windows) or runs as root (Unix, macOS): <see cref="Environment.IsPrivilegedProcess"/>.
    /// </param>
    /// <param name="environment">Reads an environment variable; <see cref="Environment.GetEnvironmentVariable(string)"/>.</param>
    /// <returns>The route, or <see cref="BrowserRoute.None"/> with the reason.</returns>
    public static BrowserDecision Choose(BrowserPlatform platform, bool privileged, Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        switch (platform)
        {
            case BrowserPlatform.Windows:
                return new BrowserDecision(privileged ? BrowserRoute.DesktopShell : BrowserRoute.ShellOpen, null);

            case BrowserPlatform.Unix:
                if (privileged)
                {
                    return BrowserDecision.NotOpened(RootReason);
                }

                if (string.IsNullOrEmpty(environment(X11DisplayVariable)) && string.IsNullOrEmpty(environment(WaylandDisplayVariable)))
                {
                    return BrowserDecision.NotOpened(
                        "there is no graphical session (neither DISPLAY nor WAYLAND_DISPLAY is set). Open the link in a browser on this machine's desktop, or forward the port over SSH (ssh -L) and open it on your own machine.");
                }

                return new BrowserDecision(BrowserRoute.XdgOpen, null);

            case BrowserPlatform.MacOS:
                return privileged ? BrowserDecision.NotOpened(RootReason) : new BrowserDecision(BrowserRoute.MacOpen, null);

            default:
                return BrowserDecision.NotOpened("this operating system is not supported.");
        }
    }
}
