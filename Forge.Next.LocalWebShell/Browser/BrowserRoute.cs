namespace Forge.Next.LocalWebShell.Browser;

/// <summary>How the browser launcher opens the link, or that it does not.</summary>
internal enum BrowserRoute
{
    /// <summary>Not opened; <see cref="BrowserDecision.Reason"/> says why.</summary>
    None,

    /// <summary>Windows, not elevated: the shell opens the link (<c>UseShellExecute</c>), as a double-click would.</summary>
    ShellOpen,

    /// <summary>Windows, elevated: the desktop shell (Explorer) opens the link with the user's normal rights.</summary>
    DesktopShell,

    /// <summary>Linux or FreeBSD with a graphical session, not root: <c>xdg-open</c>.</summary>
    XdgOpen,

    /// <summary>macOS, not root: <c>open</c>.</summary>
    MacOpen,
}
